using System;
using System.Collections.Generic;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class InfrastructureLayoutTests
    {
        private static readonly UtilityNetwork[] AllNetworks =
            (UtilityNetwork[])Enum.GetValues(typeof(UtilityNetwork));

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

        private static bool RectInside(CellRect inner, CellRect outer)
        {
            return inner.XMin >= outer.XMin && inner.XMax <= outer.XMax &&
                   inner.ZMin >= outer.ZMin && inner.ZMax <= outer.ZMax;
        }

        [Test]
        public void Conduits_StayInsideUtilityStrips_AndNetworksDoNotOverlap()
        {
            CityGenerationSettings settings = CitySettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> strips = RoadDetailLayout.GetUtilityStripRects(cell, settings);
                var allConduits = new List<CellRect>();

                foreach (UtilityNetwork network in AllNetworks)
                {
                    List<CellRect> conduits = InfrastructureLayout.GetConduitRects(cell, settings, network);
                    Assert.AreEqual(4, conduits.Count);

                    foreach (CellRect conduit in conduits)
                    {
                        bool insideAnyStrip = false;
                        foreach (CellRect strip in strips)
                        {
                            if (RectInside(conduit, strip))
                            {
                                insideAnyStrip = true;
                                break;
                            }
                        }

                        Assert.IsTrue(insideAnyStrip, $"{network} conduit in {cell} leaves the utility strips.");
                        allConduits.Add(conduit);
                    }
                }

                for (int i = 0; i < allConduits.Count; i++)
                {
                    for (int j = i + 1; j < allConduits.Count; j++)
                    {
                        Assert.IsFalse(allConduits[i].Overlaps(allConduits[j]),
                            $"Conduits {i} and {j} overlap in {cell}.");
                    }
                }
            }
        }

        [Test]
        public void Manholes_SitOnTheSecondaryRoad_OutsideTheIntersection()
        {
            CityGenerationSettings settings = CitySettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<Vector2> manholes = InfrastructureLayout.GetManholePositions(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
                CellRect intersection = RoadDetailLayout.GetIntersectionRect(cell, settings);

                Assert.Greater(manholes.Count, 0);

                foreach (Vector2 manhole in manholes)
                {
                    Assert.AreEqual(secondary.CenterX, manhole.x, 0.001f);
                    Assert.GreaterOrEqual(manhole.y, secondary.ZMin);
                    Assert.LessOrEqual(manhole.y, secondary.ZMax);
                    Assert.IsFalse(manhole.y >= intersection.ZMin && manhole.y <= intersection.ZMax,
                        $"Manhole at {manhole} sits inside the intersection in {cell}.");
                }
            }
        }

        [Test]
        public void CornerEquipment_HasDistinctPositionsPerNetwork()
        {
            CityGenerationSettings settings = CitySettings();
            var cell = new CellCoordinate2D(1, 1);

            var positions = new HashSet<Vector2>();
            foreach (UtilityNetwork network in AllNetworks)
            {
                positions.Add(InfrastructureLayout.GetCornerEquipmentPosition(cell, settings, network));
            }

            Assert.AreEqual(AllNetworks.Length, positions.Count, "Corner equipment positions collide.");
        }

        [Test]
        public void LightingCircuit_AvoidsTheIntersectionZone()
        {
            CityGenerationSettings settings = CitySettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
                float exclusionMin = secondary.XMin - settings.SidewalkWidth;
                float exclusionMax = secondary.XMax + settings.SidewalkWidth;

                List<CellRect> wires = InfrastructureLayout.GetLightingCircuitRects(cell, settings);
                Assert.AreEqual(4, wires.Count);

                foreach (CellRect wire in wires)
                {
                    Assert.Greater(wire.Width, 0f, $"Zero-length lighting wire in {cell}.");
                    Assert.IsFalse(wire.XMin < exclusionMax && wire.XMax > exclusionMin &&
                                   !(wire.XMax <= exclusionMin) && !(wire.XMin >= exclusionMax),
                        $"Lighting wire crosses the intersection zone in {cell}.");
                }
            }
        }

        [Test]
        public void ServicePaths_AvoidRoadsSidewalksAndBlocks()
        {
            CityGenerationSettings settings = CitySettings();
            var quadrants = (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant));

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                foreach (DevelopmentBlockQuadrant quadrant in quadrants)
                {
                    CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                    List<CellRect> paths = InfrastructureLayout.GetServicePathRects(cell, settings, quadrant);
                    Assert.AreEqual(2, paths.Count);

                    foreach (CellRect path in paths)
                    {
                        Assert.IsFalse(path.Overlaps(main), $"Service path overlaps main road in {cell} {quadrant}.");
                        Assert.IsFalse(path.Overlaps(secondary), $"Service path overlaps secondary road in {cell} {quadrant}.");
                        Assert.IsFalse(path.Overlaps(block), $"Service path overlaps its block in {cell} {quadrant}.");
                    }
                }
            }
        }

        [Test]
        public void BridgeDeck_SpansTheRiverAboveWaterAndBanks()
        {
            TerrainGenerationSettings terrain = TerrainSettings();
            (CellRect footprint, float deckTopY) = InfrastructureLayout.GetBridgeDeck(terrain);

            float x = InfrastructureLayout.GetServiceRoadX(terrain);
            float riverZ = TerrainHeightField.GetRiverCenterZ(x, terrain);

            Assert.GreaterOrEqual(riverZ, footprint.ZMin, "Bridge does not reach the river center (south).");
            Assert.LessOrEqual(riverZ, footprint.ZMax, "Bridge does not reach the river center (north).");
            Assert.Greater(deckTopY, terrain.SeaLevel, "Bridge deck is not above sea level.");

            float channelHeight = TerrainHeightField.SampleHeight(x, riverZ, terrain);
            Assert.Greater(deckTopY, channelHeight, "Bridge deck is not above the carved channel.");
        }

        [Test]
        public void Tunnel_RunsBelowTheCliffTerrain()
        {
            TerrainGenerationSettings terrain = TerrainSettings();
            (CellRect footprint, float floorY, float height) = InfrastructureLayout.GetTunnelSegment(terrain);

            float x = InfrastructureLayout.GetServiceRoadX(terrain);
            float cliffZ = TerrainHeightField.GetCliffLineZ(x, terrain);

            Assert.GreaterOrEqual(cliffZ, footprint.ZMin);
            Assert.LessOrEqual(cliffZ, footprint.ZMax);
            Assert.Greater(height, 0f);

            // At the north end of the bore the plateau terrain must be above the tunnel ceiling.
            float plateauHeight = TerrainHeightField.SampleHeight(x, footprint.ZMax, terrain);
            Assert.Greater(plateauHeight, floorY + height, "Tunnel ceiling pokes out of the cliff plateau.");
        }

        [Test]
        public void InfrastructureLayout_IsDeterministic()
        {
            CityGenerationSettings settings = CitySettings();
            var cell = new CellCoordinate2D(2, 0);

            Assert.AreEqual(
                InfrastructureLayout.GetConduitRects(cell, settings, UtilityNetwork.Water),
                InfrastructureLayout.GetConduitRects(cell, settings, UtilityNetwork.Water));
            Assert.AreEqual(
                InfrastructureLayout.GetManholePositions(cell, settings),
                InfrastructureLayout.GetManholePositions(cell, settings));
            Assert.AreEqual(
                InfrastructureLayout.GetBridgeDeck(TerrainSettings()),
                InfrastructureLayout.GetBridgeDeck(TerrainSettings()));
        }
    }
}
