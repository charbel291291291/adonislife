using System.Collections.Generic;
using AdonisLife.World.Buildings;
using AdonisLife.World.Common;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.ProceduralCity
{
    /// <summary>Nature prop category placed on the terrain.</summary>
    public enum NatureType
    {
        Tree,
        Rock
    }

    /// <summary>One deterministic nature prop placement on the terrain.</summary>
    public readonly struct NatureInstance
    {
        public readonly NatureType Type;
        public readonly Vector2 Position;
        public readonly float GroundHeight;
        public readonly float Scale;

        public NatureInstance(NatureType type, Vector2 position, float groundHeight, float scale)
        {
            Type = type;
            Position = position;
            GroundHeight = groundHeight;
            Scale = scale;
        }
    }

    /// <summary>
    /// Pure geometry for the environment layer: street trees and grass strips in the
    /// block-inset gap along the roads, park rings with playground pads around the civic
    /// buildings, and splat-driven tree/rock placements on the terrain. No scene, editor, or
    /// asset dependencies.
    /// </summary>
    public static class EnvironmentLayout
    {
        public const float TreeBandCenterOffset = 1.25f;
        public const float TreeSpacing = 50f;
        public const float TreeEdgeOffset = 12.5f;
        public const float IntersectionTreeClearance = 3f;
        public const float GrassBandInnerOffset = 0.5f;
        public const float PlaygroundSize = 3f;
        public const float PlaygroundCornerInset = 0.5f;
        public const float NatureSampleStep = 60f;
        public const float NatureJitter = 10f;
        public const float TreeGrassThreshold = 0.55f;
        public const float RockWeightThreshold = 0.35f;
        public const float TreePlacementChance = 0.5f;

        /// <summary>
        /// Street tree positions in the block-inset gap on both sides of both roads, clear of
        /// the intersection zone and the corner utility equipment.
        /// </summary>
        public static List<Vector2> GetStreetTreePositions(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float sw = settings.SidewalkWidth;

            float mainNorthZ = main.ZMax + sw + TreeBandCenterOffset;
            float mainSouthZ = main.ZMin - sw - TreeBandCenterOffset;
            float secondaryWestX = secondary.XMin - sw - TreeBandCenterOffset;
            float secondaryEastX = secondary.XMax + sw + TreeBandCenterOffset;

            float xExclusionMin = secondary.XMin - sw - IntersectionTreeClearance;
            float xExclusionMax = secondary.XMax + sw + IntersectionTreeClearance;
            float zExclusionMin = main.ZMin - sw - IntersectionTreeClearance;
            float zExclusionMax = main.ZMax + sw + IntersectionTreeClearance;

            var positions = new List<Vector2>();
            for (float x = origin.x + TreeEdgeOffset; x < origin.x + settings.CellSize; x += TreeSpacing)
            {
                if (x > xExclusionMin && x < xExclusionMax)
                {
                    continue;
                }

                positions.Add(new Vector2(x, mainNorthZ));
                positions.Add(new Vector2(x, mainSouthZ));
            }

            for (float z = origin.y + TreeEdgeOffset; z < origin.y + settings.CellSize; z += TreeSpacing)
            {
                if (z > zExclusionMin && z < zExclusionMax)
                {
                    continue;
                }

                positions.Add(new Vector2(secondaryWestX, z));
                positions.Add(new Vector2(secondaryEastX, z));
            }

            return positions;
        }

        /// <summary>
        /// Grass strips filling the block-inset gap between the utility strips and the blocks on
        /// every road side, split at the intersection like the utility strips.
        /// </summary>
        public static List<CellRect> GetGrassStripRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float cellMaxX = origin.x + settings.CellSize;
            float cellMaxZ = origin.y + settings.CellSize;
            float sw = settings.SidewalkWidth;
            float inset = settings.BlockInset;

            float northInner = main.ZMax + sw + GrassBandInnerOffset;
            float northOuter = main.ZMax + sw + inset;
            float southInner = main.ZMin - sw - GrassBandInnerOffset;
            float southOuter = main.ZMin - sw - inset;
            float westColumn = secondary.XMin - sw;
            float eastColumn = secondary.XMax + sw;

            return new List<CellRect>
            {
                // Main avenue sides, split at the secondary sidewalk columns.
                new CellRect(origin.x, westColumn - GrassBandInnerOffset, northInner, northOuter),
                new CellRect(eastColumn + GrassBandInnerOffset, cellMaxX, northInner, northOuter),
                new CellRect(origin.x, westColumn - GrassBandInnerOffset, southOuter, southInner),
                new CellRect(eastColumn + GrassBandInnerOffset, cellMaxX, southOuter, southInner),
                // Secondary road sides, split at the main sidewalk band.
                new CellRect(westColumn - inset, westColumn - GrassBandInnerOffset, origin.y, southOuter),
                new CellRect(westColumn - inset, westColumn - GrassBandInnerOffset, northOuter, cellMaxZ),
                new CellRect(eastColumn + GrassBandInnerOffset, eastColumn + inset, origin.y, southOuter),
                new CellRect(eastColumn + GrassBandInnerOffset, eastColumn + inset, northOuter, cellMaxZ)
            };
        }

        /// <summary>
        /// Park ring rects around each civic building: the setback area between the civic
        /// footprint and its block edge.
        /// </summary>
        public static List<CellRect> GetParkRects(CityGenerationSettings settings)
        {
            var parks = new List<CellRect>();
            var center = new CellCoordinate2D(settings.CellsX / 2, settings.CellsZ / 2);

            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                CellRect block = ProceduralCityLayout.GetBlockRect(center, settings, quadrant);
                CellRect footprint = BuildingBlockPlanner.PlanBlock(center, quadrant, settings)[0].Footprint;

                parks.Add(new CellRect(block.XMin, block.XMax, block.ZMin, footprint.ZMin));
                parks.Add(new CellRect(block.XMin, block.XMax, footprint.ZMax, block.ZMax));
                parks.Add(new CellRect(block.XMin, footprint.XMin, footprint.ZMin, footprint.ZMax));
                parks.Add(new CellRect(footprint.XMax, block.XMax, footprint.ZMin, footprint.ZMax));
            }

            return parks;
        }

        /// <summary>
        /// Playground pads at the two non-road corners of every civic block, sitting on the park
        /// ring.
        /// </summary>
        public static List<CellRect> GetPlaygroundPads(CityGenerationSettings settings)
        {
            var pads = new List<CellRect>();
            var center = new CellCoordinate2D(settings.CellsX / 2, settings.CellsZ / 2);

            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                CellRect block = ProceduralCityLayout.GetBlockRect(center, settings, quadrant);
                bool mainIsSouth = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.NE;
                bool secondaryIsEast = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.SW;

                // Rear corner: away from both roads.
                float rearZMin = mainIsSouth ? block.ZMax - PlaygroundCornerInset - PlaygroundSize : block.ZMin + PlaygroundCornerInset;
                float rearXMin = secondaryIsEast ? block.XMin + PlaygroundCornerInset : block.XMax - PlaygroundCornerInset - PlaygroundSize;
                pads.Add(new CellRect(rearXMin, rearXMin + PlaygroundSize, rearZMin, rearZMin + PlaygroundSize));

                // Second pad: rear-Z, road-side-X corner.
                float sideXMin = secondaryIsEast ? block.XMax - PlaygroundCornerInset - PlaygroundSize : block.XMin + PlaygroundCornerInset;
                pads.Add(new CellRect(sideXMin, sideXMin + PlaygroundSize, rearZMin, rearZMin + PlaygroundSize));
            }

            return pads;
        }

        /// <summary>
        /// Deterministic tree and rock placements on the terrain, driven by the vegetation mask:
        /// trees on grass-dominant land above sea level, rocks where the rock weight is high.
        /// </summary>
        public static List<NatureInstance> GetNatureInstances(TerrainGenerationSettings terrain)
        {
            var instances = new List<NatureInstance>();

            for (float x = terrain.OriginX + NatureSampleStep / 2f;
                 x < terrain.OriginX + terrain.TotalWidth;
                 x += NatureSampleStep)
            {
                for (float z = terrain.OriginZ + NatureSampleStep / 2f;
                     z < terrain.OriginZ + terrain.TotalDepth;
                     z += NatureSampleStep)
                {
                    int ix = (int)(x - terrain.OriginX);
                    int iz = (int)(z - terrain.OriginZ);
                    float jx = (DeterministicHash.Value01(ix, iz, 1, terrain.Seed) - 0.5f) * 2f * NatureJitter;
                    float jz = (DeterministicHash.Value01(ix, iz, 2, terrain.Seed) - 0.5f) * 2f * NatureJitter;
                    float px = x + jx;
                    float pz = z + jz;

                    Vector3 weights = TerrainHeightField.GetSplatWeights(px, pz, terrain);
                    float height = TerrainHeightField.SampleHeight(px, pz, terrain);
                    float roll = DeterministicHash.Value01(ix, iz, 3, terrain.Seed);

                    if (weights.z > RockWeightThreshold)
                    {
                        float scale = 1f + roll * 1.5f;
                        instances.Add(new NatureInstance(NatureType.Rock, new Vector2(px, pz), height, scale));
                    }
                    else if (weights.y > TreeGrassThreshold && height > terrain.SeaLevel + 1f &&
                             roll < TreePlacementChance)
                    {
                        float scale = 0.8f + roll * 0.8f;
                        instances.Add(new NatureInstance(NatureType.Tree, new Vector2(px, pz), height, scale));
                    }
                }
            }

            return instances;
        }
    }
}
