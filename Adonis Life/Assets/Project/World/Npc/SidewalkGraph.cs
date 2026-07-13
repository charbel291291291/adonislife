using System.Collections.Generic;
using System.Linq;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Traffic;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.Npc
{
    /// <summary>
    /// Pedestrian navigation graph over the sidewalk network. Nodes are the four corner curb
    /// pads of every intersection; edges are the pedestrian crossings around each intersection
    /// and the sidewalk runs connecting adjacent intersections. Corner index order matches
    /// <see cref="RoadDetailLayout.GetCornerCurbRects"/>: 0=SW, 1=SE, 2=NW, 3=NE.
    /// </summary>
    public class SidewalkGraph
    {
        public const int CornersPerIntersection = 4;

        private readonly Dictionary<int, Vector2> _nodePositions;
        private readonly Dictionary<int, List<(int neighbor, float cost)>> _adjacency;

        public IReadOnlyDictionary<int, Vector2> NodePositions => _nodePositions;
        public int EdgeCount { get; }

        private SidewalkGraph(
            Dictionary<int, Vector2> nodePositions,
            Dictionary<int, List<(int, float)>> adjacency,
            int edgeCount)
        {
            _nodePositions = nodePositions;
            _adjacency = adjacency;
            EdgeCount = edgeCount;
        }

        public static int GetNodeId(CellCoordinate2D cell, int corner, CityGenerationSettings settings)
        {
            return (cell.Z * settings.CellsX + cell.X) * CornersPerIntersection + corner;
        }

        public IEnumerable<(int neighbor, float cost)> GetNeighbors(int nodeId)
        {
            return _adjacency.TryGetValue(nodeId, out List<(int, float)> list)
                ? list
                : Enumerable.Empty<(int, float)>();
        }

        /// <summary>Shortest pedestrian path between two corner nodes (inclusive), or null.</summary>
        public List<int> FindPath(int startNodeId, int goalNodeId)
        {
            return GraphPathfinder.FindPath(_nodePositions.Keys, GetNeighbors, startNodeId, goalNodeId);
        }

        public static SidewalkGraph Build(CityGenerationSettings settings)
        {
            var positions = new Dictionary<int, Vector2>();
            var adjacency = new Dictionary<int, List<(int, float)>>();
            int edgeCount = 0;

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> pads = RoadDetailLayout.GetCornerCurbRects(cell, settings);
                for (int corner = 0; corner < CornersPerIntersection; corner++)
                {
                    positions[GetNodeId(cell, corner, settings)] =
                        new Vector2(pads[corner].CenterX, pads[corner].CenterZ);
                }
            }

            void Connect(int a, int b)
            {
                float cost = Vector2.Distance(positions[a], positions[b]);
                AddNeighbor(adjacency, a, b, cost);
                AddNeighbor(adjacency, b, a, cost);
                edgeCount++;
            }

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                // Crossings around this intersection: south (0-1), north (2-3), west (0-2), east (1-3).
                Connect(GetNodeId(cell, 0, settings), GetNodeId(cell, 1, settings));
                Connect(GetNodeId(cell, 2, settings), GetNodeId(cell, 3, settings));
                Connect(GetNodeId(cell, 0, settings), GetNodeId(cell, 2, settings));
                Connect(GetNodeId(cell, 1, settings), GetNodeId(cell, 3, settings));

                // Sidewalk runs to the east neighbor along the main avenue.
                if (cell.X + 1 < settings.CellsX)
                {
                    var east = new CellCoordinate2D(cell.X + 1, cell.Z);
                    Connect(GetNodeId(cell, 1, settings), GetNodeId(east, 0, settings));
                    Connect(GetNodeId(cell, 3, settings), GetNodeId(east, 2, settings));
                }

                // Sidewalk runs to the north neighbor along the secondary road.
                if (cell.Z + 1 < settings.CellsZ)
                {
                    var north = new CellCoordinate2D(cell.X, cell.Z + 1);
                    Connect(GetNodeId(cell, 2, settings), GetNodeId(north, 0, settings));
                    Connect(GetNodeId(cell, 3, settings), GetNodeId(north, 1, settings));
                }
            }

            return new SidewalkGraph(positions, adjacency, edgeCount);
        }

        private static void AddNeighbor(
            Dictionary<int, List<(int, float)>> adjacency, int from, int to, float cost)
        {
            if (!adjacency.TryGetValue(from, out List<(int, float)> list))
            {
                list = new List<(int, float)>();
                adjacency[from] = list;
            }

            list.Add((to, cost));
        }
    }
}
