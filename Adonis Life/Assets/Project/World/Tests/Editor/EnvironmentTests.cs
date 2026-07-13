using System;
using System.Collections.Generic;
using AdonisLife.World.Environment;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class EnvironmentLayoutTests
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

        private static TerrainGenerationSettings TerrainSettings()
        {
            return new TerrainGenerationSettings(
                chunksX: 4,
                chunksZ: 4,
                chunkSize: 250f,
                heightmapResolution: 129,
                originX: -1000f,
                originZ: 0f,
                maxHeight: 60f,
                seaLevel: 6f,
                coastWidth: 180f,
                beachBand: 2.5f,
                riverWidth: 14f,
                riverDepth: 7f,
                lakeRadius: 60f,
                lakeDepth: 5f,
                cliffHeight: 20f,
                seed: 1234);
        }

        [Test]
        public void StreetTrees_StayOffRoadsSidewalksAndBlocks()
        {
            CityGenerationSettings settings = CitySettings();
            var quadrants = (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant));

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<Vector2> trees = EnvironmentLayout.GetStreetTreePositions(cell, settings);
                Assert.Greater(trees.Count, 0);

                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                foreach (Vector2 tree in trees)
                {
                    Assert.IsFalse(Contains(main, tree), $"Tree {tree} stands on the main road in {cell}.");
                    Assert.IsFalse(Contains(secondary, tree), $"Tree {tree} stands on the secondary road in {cell}.");

                    foreach (DevelopmentBlockQuadrant quadrant in quadrants)
                    {
                        CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                        Assert.IsFalse(Contains(block, tree), $"Tree {tree} stands inside block {quadrant} in {cell}.");
                    }
                }
            }
        }

        [Test]
        public void StreetTrees_KeepClearOfCornerEquipment()
        {
            CityGenerationSettings settings = CitySettings();
            var networks = (UtilityNetwork[])Enum.GetValues(typeof(UtilityNetwork));

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (Vector2 tree in EnvironmentLayout.GetStreetTreePositions(cell, settings))
                {
                    foreach (UtilityNetwork network in networks)
                    {
                        Vector2 equipment = InfrastructureLayout.GetCornerEquipmentPosition(cell, settings, network);
                        Assert.Greater(Vector2.Distance(tree, equipment), 1.2f,
                            $"Tree {tree} collides with {network} equipment in {cell}.");
                    }
                }
            }
        }

        [Test]
        public void GrassStrips_AvoidRoadsUtilityStripsAndBlocks()
        {
            CityGenerationSettings settings = CitySettings();
            var quadrants = (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant));

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> grass = EnvironmentLayout.GetGrassStripRects(cell, settings);
                Assert.AreEqual(8, grass.Count);

                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
                List<CellRect> utility = RoadDetailLayout.GetUtilityStripRects(cell, settings);

                foreach (CellRect strip in grass)
                {
                    Assert.Greater(strip.Width, 0f);
                    Assert.Greater(strip.Depth, 0f);
                    Assert.IsFalse(strip.Overlaps(main), $"Grass overlaps the main road in {cell}.");
                    Assert.IsFalse(strip.Overlaps(secondary), $"Grass overlaps the secondary road in {cell}.");

                    foreach (CellRect utilityStrip in utility)
                    {
                        Assert.IsFalse(strip.Overlaps(utilityStrip), $"Grass overlaps a utility strip in {cell}.");
                    }

                    foreach (DevelopmentBlockQuadrant quadrant in quadrants)
                    {
                        Assert.IsFalse(strip.Overlaps(ProceduralCityLayout.GetBlockRect(cell, settings, quadrant)),
                            $"Grass overlaps block {quadrant} in {cell}.");
                    }
                }
            }
        }

        [Test]
        public void Parks_FillCivicSetbacks_WithoutTouchingTheBuildings()
        {
            CityGenerationSettings settings = CitySettings();
            List<CellRect> parks = EnvironmentLayout.GetParkRects(settings);

            Assert.AreEqual(16, parks.Count);

            var center = new CellCoordinate2D(1, 1);
            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                CellRect footprint = Buildings.BuildingBlockPlanner.PlanBlock(center, quadrant, settings)[0].Footprint;
                foreach (CellRect park in parks)
                {
                    Assert.IsFalse(park.Overlaps(footprint), $"Park overlaps the {quadrant} civic building.");
                }
            }
        }

        [Test]
        public void Playgrounds_SitInsideTheParkRings()
        {
            CityGenerationSettings settings = CitySettings();
            List<CellRect> parks = EnvironmentLayout.GetParkRects(settings);
            List<CellRect> pads = EnvironmentLayout.GetPlaygroundPads(settings);

            Assert.AreEqual(8, pads.Count);

            foreach (CellRect pad in pads)
            {
                bool insideAnyPark = false;
                foreach (CellRect park in parks)
                {
                    if (pad.XMin >= park.XMin && pad.XMax <= park.XMax &&
                        pad.ZMin >= park.ZMin && pad.ZMax <= park.ZMax)
                    {
                        insideAnyPark = true;
                        break;
                    }
                }

                Assert.IsTrue(insideAnyPark, $"Playground pad at ({pad.CenterX},{pad.CenterZ}) is outside every park.");
            }
        }

        [Test]
        public void NatureInstances_AreDeterministic_AndIncludeTreesAndRocks()
        {
            TerrainGenerationSettings terrain = TerrainSettings();

            List<NatureInstance> a = EnvironmentLayout.GetNatureInstances(terrain);
            List<NatureInstance> b = EnvironmentLayout.GetNatureInstances(terrain);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }

            bool hasTree = false;
            bool hasRock = false;
            foreach (NatureInstance instance in a)
            {
                hasTree |= instance.Type == NatureType.Tree;
                hasRock |= instance.Type == NatureType.Rock;
            }

            Assert.IsTrue(hasTree, "No trees placed on the terrain.");
            Assert.IsTrue(hasRock, "No rocks placed on the terrain.");
        }

        [Test]
        public void TerrainTrees_StandOnLandAboveSeaLevel()
        {
            TerrainGenerationSettings terrain = TerrainSettings();

            foreach (NatureInstance instance in EnvironmentLayout.GetNatureInstances(terrain))
            {
                Assert.GreaterOrEqual(instance.Position.x, terrain.OriginX - EnvironmentLayout.NatureJitter);
                Assert.LessOrEqual(instance.Position.x, terrain.OriginX + terrain.TotalWidth + EnvironmentLayout.NatureJitter);

                if (instance.Type == NatureType.Tree)
                {
                    Assert.Greater(instance.GroundHeight, terrain.SeaLevel, "A tree stands below sea level.");
                }

                Assert.Greater(instance.Scale, 0f);
            }
        }

        private static bool Contains(CellRect rect, Vector2 point)
        {
            return point.x > rect.XMin && point.x < rect.XMax && point.y > rect.ZMin && point.y < rect.ZMax;
        }
    }

    public class DayNightWeatherTests
    {
        [Test]
        public void SunPitch_MatchesKeyHours()
        {
            Assert.AreEqual(-90f, DayNightModel.GetSunPitch(0f), 0.001f);
            Assert.AreEqual(0f, DayNightModel.GetSunPitch(6f), 0.001f);
            Assert.AreEqual(90f, DayNightModel.GetSunPitch(12f), 0.001f);
            Assert.AreEqual(180f, DayNightModel.GetSunPitch(18f), 0.001f);
        }

        [Test]
        public void Periods_MapHourRangesCorrectly()
        {
            Assert.AreEqual(DayPeriod.Night, DayNightModel.GetPeriod(2f));
            Assert.AreEqual(DayPeriod.Dawn, DayNightModel.GetPeriod(5f));
            Assert.AreEqual(DayPeriod.Day, DayNightModel.GetPeriod(12f));
            Assert.AreEqual(DayPeriod.Dusk, DayNightModel.GetPeriod(18.5f));
            Assert.AreEqual(DayPeriod.Night, DayNightModel.GetPeriod(23f));
        }

        [Test]
        public void Hours_WrapAroundTheDay()
        {
            Assert.AreEqual(1f, DayNightModel.NormalizeHour(25f), 0.001f);
            Assert.AreEqual(23f, DayNightModel.NormalizeHour(-1f), 0.001f);
        }

        [Test]
        public void Weather_IsDeterministicPerDay()
        {
            for (int day = 0; day < 30; day++)
            {
                Assert.AreEqual(
                    WeatherModel.GetWeatherForDay(day, 42),
                    WeatherModel.GetWeatherForDay(day, 42));
            }
        }

        [Test]
        public void Weather_ReachesEveryStateOverAYear()
        {
            var seen = new HashSet<WeatherState>();
            for (int day = 0; day < 365; day++)
            {
                seen.Add(WeatherModel.GetWeatherForDay(day, 42));
            }

            foreach (WeatherState state in (WeatherState[])Enum.GetValues(typeof(WeatherState)))
            {
                Assert.Contains(state, new List<WeatherState>(seen), $"Weather state {state} never occurred.");
            }
        }
    }
}
