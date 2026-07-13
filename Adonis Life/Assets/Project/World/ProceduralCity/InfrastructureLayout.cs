using System.Collections.Generic;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.ProceduralCity
{
    /// <summary>The four buried utility networks routed through the city's utility strips.</summary>
    public enum UtilityNetwork
    {
        Electricity,
        Water,
        Sewage,
        Internet
    }

    /// <summary>
    /// Pure geometry for the infrastructure layer: buried utility conduits inside the main-avenue
    /// utility strips, sewage manholes on the secondary road, corner equipment (transformer,
    /// water valve, internet cabinet), the elevated street-lighting circuit, service paths on the
    /// non-road sides of every development block, and the terrain-side service road with its
    /// river bridge and cliff tunnel. No scene, editor, or asset dependencies.
    /// </summary>
    public static class InfrastructureLayout
    {
        public const float ConduitWidth = 0.1f;
        public const float ConduitSpacing = 0.1f;
        public const float ManholeSize = 1f;
        public const float ManholeSpacing = 60f;
        public const float ManholeEdgeOffset = 40f;
        public const float EquipmentOffset = 1.2f;
        public const float LightingWireWidth = 0.05f;
        public const float LightingWireHeight = 8f;
        public const float ServicePathWidth = 1.2f;
        public const float ServiceRoadHalfWidth = 3f;
        public const float BridgeApproachMargin = 5f;
        public const float BridgeClearance = 1f;
        public const float TunnelSouthMargin = 15f;
        public const float TunnelNorthMargin = 25f;
        public const float TunnelHeight = 6f;

        /// <summary>
        /// Buried conduit segments for one network: a lane inside each of the four main-avenue
        /// utility strip segments, offset per network so the four networks never overlap.
        /// </summary>
        public static List<CellRect> GetConduitRects(
            CellCoordinate2D cell, CityGenerationSettings settings, UtilityNetwork network)
        {
            List<CellRect> strips = RoadDetailLayout.GetUtilityStripRects(cell, settings);
            float laneOffset = (int)network * ConduitSpacing;

            // The first four utility strips are the main-avenue segments (west/east, north/south).
            var conduits = new List<CellRect>(4);
            for (int i = 0; i < 4; i++)
            {
                CellRect strip = strips[i];
                conduits.Add(new CellRect(
                    strip.XMin, strip.XMax,
                    strip.ZMin + laneOffset, strip.ZMin + laneOffset + ConduitWidth));
            }

            return conduits;
        }

        /// <summary>Sewage manhole centers along the secondary road, skipping the intersection.</summary>
        public static List<Vector2> GetManholePositions(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float exclusionMin = main.ZMin - settings.SidewalkWidth;
            float exclusionMax = main.ZMax + settings.SidewalkWidth;

            var positions = new List<Vector2>();
            for (float z = origin.y + ManholeEdgeOffset; z < origin.y + settings.CellSize; z += ManholeSpacing)
            {
                if (z > exclusionMin && z < exclusionMax)
                {
                    continue;
                }

                positions.Add(new Vector2(secondary.CenterX, z));
            }

            return positions;
        }

        /// <summary>
        /// Corner equipment position in the block-inset gap: transformer (NE), water valve (NW),
        /// internet cabinet (SW) around each intersection.
        /// </summary>
        public static Vector2 GetCornerEquipmentPosition(
            CellCoordinate2D cell, CityGenerationSettings settings, UtilityNetwork network)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            float sw = settings.SidewalkWidth;

            switch (network)
            {
                case UtilityNetwork.Electricity:
                    return new Vector2(secondary.XMax + sw + EquipmentOffset, main.ZMax + sw + EquipmentOffset);
                case UtilityNetwork.Water:
                    return new Vector2(secondary.XMin - sw - EquipmentOffset, main.ZMax + sw + EquipmentOffset);
                case UtilityNetwork.Internet:
                    return new Vector2(secondary.XMin - sw - EquipmentOffset, main.ZMin - sw - EquipmentOffset);
                case UtilityNetwork.Sewage:
                    return new Vector2(secondary.XMax + sw + EquipmentOffset, main.ZMin - sw - EquipmentOffset);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(network), network, null);
            }
        }

        /// <summary>
        /// Street-lighting circuit wires: elevated lines along both main-avenue sidewalk centers,
        /// split at the intersection zone, spanning between the first and last light positions.
        /// </summary>
        public static List<CellRect> GetLightingCircuitRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float sw = settings.SidewalkWidth;
            float half = LightingWireWidth / 2f;

            float firstLightX = origin.x + RoadDetailLayout.StreetLightEdgeOffset;
            float lastLightX = firstLightX;
            for (float x = firstLightX; x < origin.x + settings.CellSize; x += RoadDetailLayout.StreetLightSpacing)
            {
                lastLightX = x;
            }

            float exclusionMin = secondary.XMin - sw;
            float exclusionMax = secondary.XMax + sw;
            float[] lineZ = { main.ZMax + sw / 2f, main.ZMin - sw / 2f };

            var wires = new List<CellRect>();
            foreach (float z in lineZ)
            {
                wires.Add(new CellRect(firstLightX, exclusionMin, z - half, z + half));
                wires.Add(new CellRect(exclusionMax, lastLightX, z - half, z + half));
            }

            return wires;
        }

        /// <summary>
        /// Ground-level service paths on the two non-road sides of a development block, inside
        /// the block-inset gap.
        /// </summary>
        public static List<CellRect> GetServicePathRects(
            CellCoordinate2D cell, CityGenerationSettings settings, DevelopmentBlockQuadrant quadrant)
        {
            CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
            bool mainIsSouth = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.NE;
            bool secondaryIsEast = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.SW;

            // Non-road sides: opposite the main avenue and opposite the secondary road.
            CellRect zSide = mainIsSouth
                ? new CellRect(block.XMin, block.XMax, block.ZMax, block.ZMax + ServicePathWidth)
                : new CellRect(block.XMin, block.XMax, block.ZMin - ServicePathWidth, block.ZMin);
            CellRect xSide = secondaryIsEast
                ? new CellRect(block.XMin - ServicePathWidth, block.XMin, block.ZMin, block.ZMax)
                : new CellRect(block.XMax, block.XMax + ServicePathWidth, block.ZMin, block.ZMax);

            return new List<CellRect> { zSide, xSide };
        }

        /// <summary>X coordinate of the north-south terrain service road.</summary>
        public static float GetServiceRoadX(TerrainGenerationSettings terrain)
        {
            return terrain.OriginX + terrain.TotalWidth * 0.5f;
        }

        /// <summary>
        /// Bridge deck over the river on the terrain service road: footprint and deck top height,
        /// spanning the carved channel with approach margins and clearance above both banks.
        /// </summary>
        public static (CellRect footprint, float deckTopY) GetBridgeDeck(TerrainGenerationSettings terrain)
        {
            float x = GetServiceRoadX(terrain);
            float riverZ = TerrainHeightField.GetRiverCenterZ(x, terrain);
            float span = terrain.RiverWidth * 2f + BridgeApproachMargin;

            var footprint = new CellRect(
                x - ServiceRoadHalfWidth, x + ServiceRoadHalfWidth,
                riverZ - span, riverZ + span);

            float southBank = TerrainHeightField.SampleHeight(x, riverZ - span, terrain);
            float northBank = TerrainHeightField.SampleHeight(x, riverZ + span, terrain);
            float deckTopY = Mathf.Max(southBank, northBank, terrain.SeaLevel) + BridgeClearance;

            return (footprint, deckTopY);
        }

        /// <summary>
        /// Tunnel through the north cliff on the terrain service road: footprint, floor height
        /// (the approach terrain height south of the cliff), and fixed tunnel bore height.
        /// </summary>
        public static (CellRect footprint, float floorY, float height) GetTunnelSegment(TerrainGenerationSettings terrain)
        {
            float x = GetServiceRoadX(terrain);
            float cliffZ = TerrainHeightField.GetCliffLineZ(x, terrain);

            var footprint = new CellRect(
                x - ServiceRoadHalfWidth, x + ServiceRoadHalfWidth,
                cliffZ - TunnelSouthMargin, cliffZ + TunnelNorthMargin);

            float floorY = TerrainHeightField.SampleHeight(x, cliffZ - TunnelSouthMargin - 5f, terrain);
            return (footprint, floorY, TunnelHeight);
        }
    }
}
