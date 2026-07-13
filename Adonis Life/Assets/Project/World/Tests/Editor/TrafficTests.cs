using System.Collections.Generic;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Traffic;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class TrafficTests
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
        public void RoadGraph_HasOneNodePerCell_AndGridEdges()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);

            Assert.AreEqual(9, graph.Nodes.Count);
            // 3 rows x 2 horizontal edges + 3 columns x 2 vertical edges.
            Assert.AreEqual(12, graph.Edges.Count);

            // The center node connects in all four directions.
            int center = RoadGraphBuilder.GetNodeId(new CellCoordinate2D(1, 1), settings);
            Assert.AreEqual(4, graph.GetIncidentEdges(center).Count);

            // A corner node connects in exactly two directions.
            int corner = RoadGraphBuilder.GetNodeId(new CellCoordinate2D(0, 0), settings);
            Assert.AreEqual(2, graph.GetIncidentEdges(corner).Count);
        }

        [Test]
        public void RoadGraph_NodesSitAtIntersectionCenters()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);

            foreach (RoadNode node in graph.Nodes)
            {
                CellRect intersection = RoadDetailLayout.GetIntersectionRect(node.Cell, settings);
                Assert.AreEqual(intersection.CenterX, node.Position.x, 0.001f);
                Assert.AreEqual(intersection.CenterZ, node.Position.y, 0.001f);
            }
        }

        [Test]
        public void LaneGraph_HasExpectedLaneCounts_AndLanesStayOnTheirRoads()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);
            List<Lane> lanes = LaneGraphBuilder.Build(graph, settings);

            // 6 main edges x 4 lanes + 6 secondary edges x 2 lanes.
            Assert.AreEqual(6 * 4 + 6 * 2, lanes.Count);

            foreach (Lane lane in lanes)
            {
                Assert.Greater(lane.Length, 0f);
                RoadEdge edge = graph.Edges[lane.EdgeIndex];
                Assert.AreEqual(edge.Kind, lane.Kind);

                // Lane offset from the road centerline must stay within the road half-width.
                Vector2 a = graph.Nodes[edge.NodeA].Position;
                Vector2 b = graph.Nodes[edge.NodeB].Position;
                Vector2 direction = (b - a).normalized;
                Vector2 mid = lane.GetPoint(0.5f);
                Vector2 centerMid = (a + b) / 2f;
                float offset = Mathf.Abs(Vector2.Dot(mid - centerMid, new Vector2(direction.y, -direction.x)));

                float halfWidth = lane.Kind == RoadKind.MainAvenue
                    ? settings.MainRoadWidth / 2f
                    : settings.SecondaryRoadWidth / 2f;
                Assert.Less(offset, halfWidth, $"Lane offset {offset} exceeds road half-width.");
                Assert.Greater(offset, 0f, "Lane sits exactly on the centerline.");
            }
        }

        [Test]
        public void Pathfinder_FindsShortestManhattanRoute()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);

            int start = RoadGraphBuilder.GetNodeId(new CellCoordinate2D(0, 0), settings);
            int goal = RoadGraphBuilder.GetNodeId(new CellCoordinate2D(2, 2), settings);

            List<int> path = RoadPathfinder.FindPath(graph, start, goal);

            Assert.IsNotNull(path);
            Assert.AreEqual(5, path.Count, "Corner-to-corner path should traverse 4 edges.");
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            Assert.AreEqual(4 * settings.CellSize, RoadPathfinder.GetPathLength(graph, path), 0.001f);
        }

        [Test]
        public void Pathfinder_ReturnsTrivialPathToSelf()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);

            List<int> path = RoadPathfinder.FindPath(graph, 4, 4);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(4, path[0]);
        }

        [Test]
        public void ParkingGraph_LinksEveryParkingLotToItsCellNode()
        {
            CityGenerationSettings settings = CitySettings();
            List<ParkingNode> parking = ParkingGraphBuilder.Build(settings);

            // 8 non-civic cells x 4 blocks, one parking lot each.
            Assert.AreEqual(32, parking.Count);

            foreach (ParkingNode node in parking)
            {
                Assert.AreEqual(RoadGraphBuilder.GetNodeId(node.Cell, settings), node.RoadNodeId);

                Vector2 origin = ProceduralCityLayout.GetCellOrigin(node.Cell, settings);
                Assert.GreaterOrEqual(node.Position.x, origin.x);
                Assert.LessOrEqual(node.Position.x, origin.x + settings.CellSize);
                Assert.GreaterOrEqual(node.Position.y, origin.y);
                Assert.LessOrEqual(node.Position.y, origin.y + settings.CellSize);
            }
        }

        [Test]
        public void TrafficLights_NeverShowBothAxesPermissive()
        {
            for (float t = 0f; t < TrafficLightModel.CycleDuration * 2f; t += 0.25f)
            {
                IntersectionPhase phase = TrafficLightModel.GetPhase(t);
                bool nsPermissive = phase.NorthSouth != SignalColor.Red;
                bool ewPermissive = phase.EastWest != SignalColor.Red;
                Assert.IsFalse(nsPermissive && ewPermissive, $"Both axes permissive at t={t}.");
            }
        }

        [Test]
        public void TrafficLights_CycleThroughAllPhases()
        {
            Assert.AreEqual(SignalColor.Green, TrafficLightModel.GetPhase(0f).NorthSouth);
            Assert.AreEqual(SignalColor.Yellow, TrafficLightModel.GetPhase(TrafficLightModel.GreenDuration + 1f).NorthSouth);
            Assert.AreEqual(SignalColor.Green, TrafficLightModel.GetPhase(
                TrafficLightModel.GreenDuration + TrafficLightModel.YellowDuration + 1f).EastWest);
            Assert.AreEqual(SignalColor.Yellow, TrafficLightModel.GetPhase(
                TrafficLightModel.CycleDuration - 1f).EastWest);
            // The cycle wraps.
            Assert.AreEqual(SignalColor.Green, TrafficLightModel.GetPhase(TrafficLightModel.CycleDuration).NorthSouth);
        }

        [Test]
        public void VehicleSpawns_AreDeterministic_AndRespectSpacing()
        {
            CityGenerationSettings settings = CitySettings();
            RoadGraph graph = RoadGraphBuilder.Build(settings);
            List<Lane> lanes = LaneGraphBuilder.Build(graph, settings);

            List<VehicleSpawn> a = VehicleSpawnModel.PlanSpawns(lanes, 40, 15f, 99);
            List<VehicleSpawn> b = VehicleSpawnModel.PlanSpawns(lanes, 40, 15f, 99);

            Assert.AreEqual(a.Count, b.Count);
            Assert.Greater(a.Count, 0);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }

            var byLane = new Dictionary<int, List<float>>();
            foreach (VehicleSpawn spawn in a)
            {
                Assert.GreaterOrEqual(spawn.T, VehicleSpawnModel.EndClearanceT - 0.001f);
                Assert.LessOrEqual(spawn.T, 1f - VehicleSpawnModel.EndClearanceT + 0.001f);

                if (!byLane.TryGetValue(spawn.LaneIndex, out List<float> list))
                {
                    list = new List<float>();
                    byLane[spawn.LaneIndex] = list;
                }

                list.Add(spawn.T);
            }

            foreach (KeyValuePair<int, List<float>> entry in byLane)
            {
                float laneLength = lanes[entry.Key].Length;
                entry.Value.Sort();
                for (int i = 1; i < entry.Value.Count; i++)
                {
                    float gapMeters = (entry.Value[i] - entry.Value[i - 1]) * laneLength;
                    Assert.GreaterOrEqual(gapMeters, 15f - 0.001f,
                        $"Vehicles on lane {entry.Key} are {gapMeters}m apart.");
                }
            }
        }
    }
}
