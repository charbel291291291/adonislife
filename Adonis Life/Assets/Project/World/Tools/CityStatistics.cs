using System.Collections.Generic;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;

namespace AdonisLife.World.Tools
{
    /// <summary>Aggregate statistics describing one generated city configuration.</summary>
    public readonly struct CityStatisticsReport
    {
        public readonly int CellCount;
        public readonly int IntersectionCount;
        public readonly int RoadSegmentCount;
        public readonly int SidewalkSegmentCount;
        public readonly int BuildingCount;
        public readonly int StreetTreeCount;
        public readonly float TotalRoadLengthMeters;
        public readonly float CityAreaSquareMeters;
        public readonly IReadOnlyDictionary<BuildingType, int> BuildingsByType;

        public CityStatisticsReport(
            int cellCount, int intersectionCount, int roadSegmentCount, int sidewalkSegmentCount,
            int buildingCount, int streetTreeCount, float totalRoadLengthMeters, float cityAreaSquareMeters,
            IReadOnlyDictionary<BuildingType, int> buildingsByType)
        {
            CellCount = cellCount;
            IntersectionCount = intersectionCount;
            RoadSegmentCount = roadSegmentCount;
            SidewalkSegmentCount = sidewalkSegmentCount;
            BuildingCount = buildingCount;
            StreetTreeCount = streetTreeCount;
            TotalRoadLengthMeters = totalRoadLengthMeters;
            CityAreaSquareMeters = cityAreaSquareMeters;
            BuildingsByType = buildingsByType;
        }
    }

    /// <summary>
    /// Pure computation of city statistics from the generation settings — the same numbers the
    /// generators produce, without touching any scene.
    /// </summary>
    public static class CityStatistics
    {
        public static CityStatisticsReport Compute(CityGenerationSettings settings)
        {
            int cellCount = settings.CellsX * settings.CellsZ;

            var byType = new Dictionary<BuildingType, int>();
            List<BuildingSpec> buildings = BuildingBlockPlanner.PlanCity(settings);
            foreach (BuildingSpec spec in buildings)
            {
                byType[spec.Type] = byType.TryGetValue(spec.Type, out int count) ? count + 1 : 1;
            }

            int treeCount = 0;
            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                treeCount += EnvironmentLayout.GetStreetTreePositions(cell, settings).Count;
            }

            UnityEngine.Vector2 dimensions = ProceduralCityLayout.GetCityDimensions(settings);

            return new CityStatisticsReport(
                cellCount,
                intersectionCount: cellCount,
                roadSegmentCount: cellCount * 2,
                sidewalkSegmentCount: cellCount * 6,
                buildingCount: buildings.Count,
                streetTreeCount: treeCount,
                totalRoadLengthMeters: cellCount * 2 * settings.CellSize,
                cityAreaSquareMeters: dimensions.x * dimensions.y,
                buildingsByType: byType);
        }

        /// <summary>Human-readable multi-line report.</summary>
        public static string Format(CityStatisticsReport report)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("City statistics:");
            builder.AppendLine($"  Cells: {report.CellCount} ({report.CityAreaSquareMeters / 1000000f:F2} km2)");
            builder.AppendLine($"  Intersections: {report.IntersectionCount}");
            builder.AppendLine($"  Road segments: {report.RoadSegmentCount} ({report.TotalRoadLengthMeters / 1000f:F1} km)");
            builder.AppendLine($"  Sidewalk segments: {report.SidewalkSegmentCount}");
            builder.AppendLine($"  Street trees: {report.StreetTreeCount}");
            builder.AppendLine($"  Buildings: {report.BuildingCount}");
            foreach (KeyValuePair<BuildingType, int> entry in report.BuildingsByType)
            {
                builder.AppendLine($"    {entry.Key}: {entry.Value}");
            }

            return builder.ToString();
        }
    }
}
