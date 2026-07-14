using System.Threading;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Tools;
using NUnit.Framework;

namespace AdonisLife.World.Tests.Editor
{
    public class DevToolsTests
    {
        private static CityGenerationSettings CitySettings()
        {
            return new CityGenerationSettings(
                cellsX: 3,
                cellsZ: 3,
                cellSize: 250f,
                mainRoadWidth: 20f,
                secondaryRoadWidth: 14f,
                sidewalkWidth: 4f,
                blockInset: 2f,
                seed: 0);
        }

        [Test]
        public void CityStatistics_MatchTheGenerators()
        {
            CityStatisticsReport report = CityStatistics.Compute(CitySettings());

            Assert.AreEqual(9, report.CellCount);
            Assert.AreEqual(9, report.IntersectionCount);
            Assert.AreEqual(18, report.RoadSegmentCount);
            Assert.AreEqual(54, report.SidewalkSegmentCount);
            Assert.AreEqual(8 * 4 * 9 + 4, report.BuildingCount);
            Assert.AreEqual(9 * 2 * 250f, report.TotalRoadLengthMeters, 0.001f);
            Assert.AreEqual(750f * 750f, report.CityAreaSquareMeters, 0.001f);
            Assert.Greater(report.StreetTreeCount, 0);

            int totalByType = 0;
            foreach (BuildingType type in (BuildingType[])System.Enum.GetValues(typeof(BuildingType)))
            {
                Assert.IsTrue(report.BuildingsByType.ContainsKey(type), $"Type {type} missing from statistics.");
                totalByType += report.BuildingsByType[type];
            }

            Assert.AreEqual(report.BuildingCount, totalByType, "Per-type counts must sum to the total.");
        }

        [Test]
        public void CityStatistics_FormatContainsKeyFigures()
        {
            CityStatisticsReport report = CityStatistics.Compute(CitySettings());
            string text = CityStatistics.Format(report);

            StringAssert.Contains("Cells: 9", text);
            StringAssert.Contains("Buildings: 292", text);
            StringAssert.Contains("Hospital", text);
        }

        [Test]
        public void GenerationProfiler_RecordsAndAccumulatesSections()
        {
            var profiler = new GenerationProfiler();

            using (profiler.Section("work"))
            {
                Thread.Sleep(5);
            }

            using (profiler.Section("work"))
            {
                Thread.Sleep(5);
            }

            using (profiler.Section("other"))
            {
            }

            Assert.GreaterOrEqual(profiler.GetMilliseconds("work"), 8d, "Accumulated section time is too low.");
            Assert.AreEqual(2, profiler.SectionNames.Count);
            Assert.AreEqual(0d, profiler.GetMilliseconds("missing"));

            string report = profiler.FormatReport();
            StringAssert.Contains("work", report);
            StringAssert.Contains("other", report);
        }
    }
}
