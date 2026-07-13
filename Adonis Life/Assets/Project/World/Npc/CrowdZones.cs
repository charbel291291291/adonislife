using System.Collections.Generic;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;

namespace AdonisLife.World.Npc
{
    /// <summary>Relative pedestrian density of a crowd zone.</summary>
    public enum CrowdDensity
    {
        VeryLow,
        Low,
        Medium,
        High
    }

    /// <summary>A world-space area with an expected pedestrian density.</summary>
    public readonly struct CrowdZone
    {
        public readonly CellCoordinate2D Cell;
        public readonly CellRect Area;
        public readonly CrowdDensity Density;

        public CrowdZone(CellCoordinate2D cell, CellRect area, CrowdDensity density)
        {
            Cell = cell;
            Area = area;
            Density = density;
        }
    }

    /// <summary>
    /// Derives crowd zones from the city's block zoning: commercial cells draw the biggest
    /// crowds, the civic center a moderate one, residential streets light foot traffic, and
    /// industrial corners almost none.
    /// </summary>
    public static class CrowdZoneMap
    {
        public static CrowdDensity GetDensity(BlockZone zone)
        {
            switch (zone)
            {
                case BlockZone.Commercial: return CrowdDensity.High;
                case BlockZone.Civic: return CrowdDensity.Medium;
                case BlockZone.Residential: return CrowdDensity.Low;
                case BlockZone.Industrial: return CrowdDensity.VeryLow;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(zone), zone, null);
            }
        }

        /// <summary>Integer spawn weight for a density level.</summary>
        public static int GetSpawnWeight(CrowdDensity density)
        {
            switch (density)
            {
                case CrowdDensity.High: return 8;
                case CrowdDensity.Medium: return 4;
                case CrowdDensity.Low: return 2;
                case CrowdDensity.VeryLow: return 1;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(density), density, null);
            }
        }

        /// <summary>One crowd zone per cell, covering the whole cell footprint.</summary>
        public static List<CrowdZone> GetCrowdZones(CityGenerationSettings settings)
        {
            var zones = new List<CrowdZone>();
            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                UnityEngine.Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
                var area = new CellRect(
                    origin.x, origin.x + settings.CellSize,
                    origin.y, origin.y + settings.CellSize);
                zones.Add(new CrowdZone(cell, area, GetDensity(BuildingBlockPlanner.GetZone(cell, settings))));
            }

            return zones;
        }
    }
}
