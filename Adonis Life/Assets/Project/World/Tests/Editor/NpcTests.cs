using System.Collections.Generic;
using AdonisLife.World.Buildings;
using AdonisLife.World.Npc;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class NpcTests
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
        public void SidewalkGraph_HasFourCornersPerIntersection_AndExpectedEdges()
        {
            CityGenerationSettings settings = CitySettings();
            SidewalkGraph graph = SidewalkGraph.Build(settings);

            Assert.AreEqual(9 * 4, graph.NodePositions.Count);
            // 4 crossings per intersection + 2 sidewalk runs per adjacent pair (12 pairs).
            Assert.AreEqual(9 * 4 + 12 * 2, graph.EdgeCount);
        }

        [Test]
        public void SidewalkGraph_ConnectsOppositeCityCorners()
        {
            CityGenerationSettings settings = CitySettings();
            SidewalkGraph graph = SidewalkGraph.Build(settings);

            int start = SidewalkGraph.GetNodeId(new CellCoordinate2D(0, 0), 0, settings);
            int goal = SidewalkGraph.GetNodeId(new CellCoordinate2D(2, 2), 3, settings);

            List<int> path = graph.FindPath(start, goal);
            Assert.IsNotNull(path, "No pedestrian path across the city.");
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            Assert.Greater(path.Count, 4);
        }

        [Test]
        public void SidewalkGraph_NodesSitOnCornerPads()
        {
            CityGenerationSettings settings = CitySettings();
            SidewalkGraph graph = SidewalkGraph.Build(settings);

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> pads = RoadDetailLayout.GetCornerCurbRects(cell, settings);
                for (int corner = 0; corner < SidewalkGraph.CornersPerIntersection; corner++)
                {
                    Vector2 position = graph.NodePositions[SidewalkGraph.GetNodeId(cell, corner, settings)];
                    Assert.AreEqual(pads[corner].CenterX, position.x, 0.001f);
                    Assert.AreEqual(pads[corner].CenterZ, position.y, 0.001f);
                }
            }
        }

        [Test]
        public void CrowdZones_CoverEveryCell_WithZoneDrivenDensities()
        {
            CityGenerationSettings settings = CitySettings();
            List<CrowdZone> zones = CrowdZoneMap.GetCrowdZones(settings);

            Assert.AreEqual(9, zones.Count);

            foreach (CrowdZone zone in zones)
            {
                BlockZone blockZone = BuildingBlockPlanner.GetZone(zone.Cell, settings);
                Assert.AreEqual(CrowdZoneMap.GetDensity(blockZone), zone.Density);
                Assert.AreEqual(settings.CellSize, zone.Area.Width, 0.001f);
                Assert.AreEqual(settings.CellSize, zone.Area.Depth, 0.001f);
            }
        }

        [Test]
        public void PedestrianSpawns_LandOnSidewalks()
        {
            CityGenerationSettings settings = CitySettings();

            foreach (PedestrianSpawn spawn in PedestrianSpawnModel.PlanSpawns(settings, 90, 7))
            {
                List<CellRect> sidewalks = PedestrianSpawnModel.GetSidewalkRects(spawn.Cell, settings);
                bool onSidewalk = false;
                foreach (CellRect rect in sidewalks)
                {
                    if (spawn.Position.x >= rect.XMin && spawn.Position.x <= rect.XMax &&
                        spawn.Position.y >= rect.ZMin && spawn.Position.y <= rect.ZMax)
                    {
                        onSidewalk = true;
                        break;
                    }
                }

                Assert.IsTrue(onSidewalk, $"Pedestrian at {spawn.Position} is off the sidewalks of {spawn.Cell}.");
            }
        }

        [Test]
        public void PedestrianSpawns_AreDeterministic_AndWeightedByCrowdZone()
        {
            CityGenerationSettings settings = CitySettings();

            List<PedestrianSpawn> a = PedestrianSpawnModel.PlanSpawns(settings, 90, 7);
            List<PedestrianSpawn> b = PedestrianSpawnModel.PlanSpawns(settings, 90, 7);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }

            int commercial = 0;
            int industrial = 0;
            foreach (PedestrianSpawn spawn in a)
            {
                BlockZone zone = BuildingBlockPlanner.GetZone(spawn.Cell, settings);
                if (zone == BlockZone.Commercial)
                {
                    commercial++;
                }
                else if (zone == BlockZone.Industrial)
                {
                    industrial++;
                }
            }

            Assert.Greater(commercial, industrial, "Commercial cells should draw more pedestrians than industrial ones.");
        }

        [Test]
        public void WaypointFollower_ReachesAndAdvancesWaypoints()
        {
            var waypoints = new List<Vector2> { new Vector2(10f, 0f), new Vector2(10f, 10f) };

            (Vector2 position, int index) = WaypointFollower.Step(
                Vector2.zero, 0, waypoints, speed: 5f, deltaTime: 1f, loop: false);
            Assert.AreEqual(new Vector2(5f, 0f), position);
            Assert.AreEqual(0, index);

            // A long step passes through the first waypoint and continues to the second.
            (position, index) = WaypointFollower.Step(
                Vector2.zero, 0, waypoints, speed: 15f, deltaTime: 1f, loop: false);
            Assert.AreEqual(1, index);
            Assert.AreEqual(new Vector2(10f, 5f), position);
        }

        [Test]
        public void WaypointFollower_LoopsWhenRequested_AndStopsWhenNot()
        {
            var waypoints = new List<Vector2> { new Vector2(2f, 0f), new Vector2(4f, 0f) };

            (Vector2 position, int index) = WaypointFollower.Step(
                Vector2.zero, 0, waypoints, speed: 10f, deltaTime: 1f, loop: false);
            Assert.AreEqual(new Vector2(4f, 0f), position, "Non-looping follower should stop at the last waypoint.");
            Assert.AreEqual(1, index);

            (position, index) = WaypointFollower.Step(
                Vector2.zero, 0, waypoints, speed: 5f, deltaTime: 1f, loop: true);
            Assert.AreEqual(0, index, "Looping follower should wrap to the first waypoint.");
        }
    }
}
