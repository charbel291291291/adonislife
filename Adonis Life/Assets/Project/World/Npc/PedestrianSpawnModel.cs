using System.Collections.Generic;
using AdonisLife.World.Common;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.Npc
{
    /// <summary>A planned pedestrian placement on a sidewalk.</summary>
    public readonly struct PedestrianSpawn
    {
        public readonly CellCoordinate2D Cell;
        public readonly Vector2 Position;

        public PedestrianSpawn(CellCoordinate2D cell, Vector2 position)
        {
            Cell = cell;
            Position = position;
        }
    }

    /// <summary>
    /// Deterministic pedestrian spawn planning: the total population is distributed across
    /// cells by crowd-zone weight, and each pedestrian is placed on one of its cell's sidewalk
    /// rects with an edge margin.
    /// </summary>
    public static class PedestrianSpawnModel
    {
        public const float SidewalkMargin = 0.5f;

        public static List<PedestrianSpawn> PlanSpawns(CityGenerationSettings settings, int totalCount, int seed)
        {
            var spawns = new List<PedestrianSpawn>();
            if (totalCount <= 0)
            {
                return spawns;
            }

            List<CrowdZone> zones = CrowdZoneMap.GetCrowdZones(settings);
            int totalWeight = 0;
            foreach (CrowdZone zone in zones)
            {
                totalWeight += CrowdZoneMap.GetSpawnWeight(zone.Density);
            }

            foreach (CrowdZone zone in zones)
            {
                int cellCount = Mathf.RoundToInt(
                    totalCount * CrowdZoneMap.GetSpawnWeight(zone.Density) / (float)totalWeight);
                List<CellRect> sidewalks = GetSidewalkRects(zone.Cell, settings);

                for (int i = 0; i < cellCount; i++)
                {
                    int cellSalt = zone.Cell.Z * settings.CellsX + zone.Cell.X;
                    int rectIndex = (int)(DeterministicHash.Value01(i, cellSalt, 41, seed) * sidewalks.Count);
                    rectIndex = System.Math.Min(rectIndex, sidewalks.Count - 1);
                    CellRect rect = sidewalks[rectIndex];

                    float u = DeterministicHash.Value01(i, cellSalt, 43, seed);
                    float v = DeterministicHash.Value01(i, cellSalt, 47, seed);
                    float x = Mathf.Lerp(rect.XMin + SidewalkMargin, rect.XMax - SidewalkMargin, u);
                    float z = Mathf.Lerp(rect.ZMin + SidewalkMargin, rect.ZMax - SidewalkMargin, v);

                    spawns.Add(new PedestrianSpawn(zone.Cell, new Vector2(x, z)));
                }
            }

            return spawns;
        }

        /// <summary>The six sidewalk rects of a cell (two main-avenue, four secondary segments).</summary>
        public static List<CellRect> GetSidewalkRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            return new List<CellRect>
            {
                ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: true),
                ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: false),
                ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: false, north: false),
                ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: false, north: true),
                ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: true, north: false),
                ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: true, north: true)
            };
        }
    }
}
