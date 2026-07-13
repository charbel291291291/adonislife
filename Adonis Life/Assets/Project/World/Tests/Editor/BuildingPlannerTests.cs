using System;
using System.Collections.Generic;
using System.Linq;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;

namespace AdonisLife.World.Tests.Editor
{
    public class BuildingPlannerTests
    {
        private static readonly DevelopmentBlockQuadrant[] AllQuadrants =
            (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant));

        private static CityGenerationSettings DefaultSettings(int seed = 0)
        {
            return new CityGenerationSettings(
                cellsX: 3,
                cellsZ: 3,
                cellSize: 250f,
                mainRoadWidth: 20f,
                secondaryRoadWidth: 14f,
                sidewalkWidth: 4f,
                blockInset: 2f,
                seed: seed);
        }

        private static bool RectInside(CellRect inner, CellRect outer, float tolerance = 0.001f)
        {
            return inner.XMin >= outer.XMin - tolerance && inner.XMax <= outer.XMax + tolerance &&
                   inner.ZMin >= outer.ZMin - tolerance && inner.ZMax <= outer.ZMax + tolerance;
        }

        [Test]
        public void PlanCity_IsDeterministic()
        {
            List<BuildingSpec> a = BuildingBlockPlanner.PlanCity(DefaultSettings());
            List<BuildingSpec> b = BuildingBlockPlanner.PlanCity(DefaultSettings());

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i], b[i], $"Spec {i} differs between identical plans.");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentPlans()
        {
            List<BuildingSpec> a = BuildingBlockPlanner.PlanCity(DefaultSettings(seed: 0));
            List<BuildingSpec> b = BuildingBlockPlanner.PlanCity(DefaultSettings(seed: 77));

            bool anyDifferent = false;
            for (int i = 0; i < a.Count && !anyDifferent; i++)
            {
                anyDifferent = !a[i].Equals(b[i]);
            }

            Assert.IsTrue(anyDifferent, "Different seeds produced identical building plans.");
        }

        [Test]
        public void CityCenterCell_HostsTheFourCivicBuildings()
        {
            CityGenerationSettings settings = DefaultSettings();
            var center = new CellCoordinate2D(1, 1);

            Assert.AreEqual(BlockZone.Civic, BuildingBlockPlanner.GetZone(center, settings));

            var civicTypes = new List<BuildingType>();
            foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
            {
                List<BuildingSpec> specs = BuildingBlockPlanner.PlanBlock(center, quadrant, settings);
                Assert.AreEqual(1, specs.Count, $"Civic block {quadrant} should hold exactly one building.");
                civicTypes.Add(specs[0].Type);
            }

            CollectionAssert.AreEquivalent(
                new[] { BuildingType.Hospital, BuildingType.Government, BuildingType.Police, BuildingType.FireStation },
                civicTypes);
        }

        [Test]
        public void AllTwelveBuildingTypes_AppearInTheDefaultCity()
        {
            List<BuildingSpec> specs = BuildingBlockPlanner.PlanCity(DefaultSettings());
            HashSet<BuildingType> present = new HashSet<BuildingType>(specs.Select(s => s.Type));

            foreach (BuildingType type in (BuildingType[])Enum.GetValues(typeof(BuildingType)))
            {
                Assert.Contains(type, present.ToList(), $"Building type {type} is missing from the city.");
            }
        }

        [Test]
        public void Buildings_StayInsideTheirBlocks()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                    foreach (BuildingSpec spec in BuildingBlockPlanner.PlanBlock(cell, quadrant, settings))
                    {
                        Assert.IsTrue(RectInside(spec.Footprint, block),
                            $"{spec.Type} in {cell} {quadrant} leaves its block.");
                    }
                }
            }
        }

        [Test]
        public void Buildings_DoNotOverlapWithinABlock()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    List<BuildingSpec> specs = BuildingBlockPlanner.PlanBlock(cell, quadrant, settings);
                    for (int i = 0; i < specs.Count; i++)
                    {
                        for (int j = i + 1; j < specs.Count; j++)
                        {
                            Assert.IsFalse(specs[i].Footprint.Overlaps(specs[j].Footprint),
                                $"Buildings {i} and {j} overlap in {cell} {quadrant}.");
                        }
                    }
                }
            }
        }

        [Test]
        public void Setbacks_KeepFootprintsInsideInsetLots()
        {
            CityGenerationSettings settings = DefaultSettings();
            var cell = new CellCoordinate2D(0, 1); // Residential cell.

            foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
            {
                List<BuildingSpec> specs = BuildingBlockPlanner.PlanBlock(cell, quadrant, settings);
                for (int lot = 0; lot < specs.Count; lot++)
                {
                    float setback = BuildingCatalog.GetDefinition(specs[lot].Type).Setback;
                    CellRect lotRect = BuildingBlockPlanner.GetLotRect(cell, quadrant, lot, settings);

                    Assert.AreEqual(lotRect.XMin + setback, specs[lot].Footprint.XMin, 0.001f);
                    Assert.AreEqual(lotRect.XMax - setback, specs[lot].Footprint.XMax, 0.001f);
                    Assert.AreEqual(lotRect.ZMin + setback, specs[lot].Footprint.ZMin, 0.001f);
                    Assert.AreEqual(lotRect.ZMax - setback, specs[lot].Footprint.ZMax, 0.001f);
                }
            }
        }

        [Test]
        public void Floors_StayWithinCatalogRange_AndHeightsMatch()
        {
            foreach (BuildingSpec spec in BuildingBlockPlanner.PlanCity(DefaultSettings()))
            {
                BuildingDefinition definition = BuildingCatalog.GetDefinition(spec.Type);
                Assert.GreaterOrEqual(spec.Floors, definition.FloorsMin, $"{spec.Type} below floor minimum.");
                Assert.LessOrEqual(spec.Floors, definition.FloorsMax, $"{spec.Type} above floor maximum.");
                Assert.AreEqual(BuildingCatalog.GetHeight(spec.Type, spec.Floors), spec.Height);
                Assert.Greater(spec.Height, 0f);
            }
        }

        [Test]
        public void Entrances_TouchTheFootprintOnARoadFacingSide()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    bool mainIsSouth = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.NE;
                    bool secondaryIsEast = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.SW;
                    EntranceSide mainSide = mainIsSouth ? EntranceSide.South : EntranceSide.North;
                    EntranceSide secondarySide = secondaryIsEast ? EntranceSide.East : EntranceSide.West;

                    foreach (BuildingSpec spec in BuildingBlockPlanner.PlanBlock(cell, quadrant, settings))
                    {
                        Assert.IsTrue(spec.Entrance == mainSide || spec.Entrance == secondarySide,
                            $"{spec.Type} in {cell} {quadrant} faces {spec.Entrance}, not a road side.");

                        switch (spec.Entrance)
                        {
                            case EntranceSide.South:
                                Assert.AreEqual(spec.Footprint.ZMin, spec.EntranceMarker.ZMax, 0.001f);
                                break;
                            case EntranceSide.North:
                                Assert.AreEqual(spec.Footprint.ZMax, spec.EntranceMarker.ZMin, 0.001f);
                                break;
                            case EntranceSide.West:
                                Assert.AreEqual(spec.Footprint.XMin, spec.EntranceMarker.XMax, 0.001f);
                                break;
                            case EntranceSide.East:
                                Assert.AreEqual(spec.Footprint.XMax, spec.EntranceMarker.XMin, 0.001f);
                                break;
                        }
                    }
                }
            }
        }

        [Test]
        public void CityPlan_HasExpectedBuildingCount()
        {
            // 8 non-civic cells x 4 blocks x 9 lots + 4 civic buildings.
            List<BuildingSpec> specs = BuildingBlockPlanner.PlanCity(DefaultSettings());
            Assert.AreEqual(8 * 4 * 9 + 4, specs.Count);
        }
    }
}
