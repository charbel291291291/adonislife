using System.Collections.Generic;
using AdonisLife.World.ProceduralCity;
using UnityEngine;

namespace AdonisLife.World.Traffic
{
    /// <summary>Which physical road an edge or lane belongs to.</summary>
    public enum RoadKind
    {
        MainAvenue,
        SecondaryRoad
    }

    /// <summary>A road graph node: one intersection at the center of an urban cell.</summary>
    public readonly struct RoadNode
    {
        public readonly int Id;
        public readonly CellCoordinate2D Cell;
        public readonly Vector2 Position;

        public RoadNode(int id, CellCoordinate2D cell, Vector2 position)
        {
            Id = id;
            Cell = cell;
            Position = position;
        }
    }

    /// <summary>An undirected road segment between two adjacent intersections.</summary>
    public readonly struct RoadEdge
    {
        public readonly int NodeA;
        public readonly int NodeB;
        public readonly RoadKind Kind;
        public readonly float Length;

        public RoadEdge(int nodeA, int nodeB, RoadKind kind, float length)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Kind = kind;
            Length = length;
        }
    }

    /// <summary>
    /// The city's intersection-level road network: one node per cell intersection, main-avenue
    /// edges between horizontal neighbors and secondary-road edges between vertical neighbors.
    /// </summary>
    public class RoadGraph
    {
        private readonly List<RoadNode> _nodes;
        private readonly List<RoadEdge> _edges;
        private readonly Dictionary<int, List<int>> _adjacency = new Dictionary<int, List<int>>();

        public IReadOnlyList<RoadNode> Nodes => _nodes;
        public IReadOnlyList<RoadEdge> Edges => _edges;

        public RoadGraph(List<RoadNode> nodes, List<RoadEdge> edges)
        {
            _nodes = nodes;
            _edges = edges;

            for (int i = 0; i < edges.Count; i++)
            {
                AddAdjacency(edges[i].NodeA, i);
                AddAdjacency(edges[i].NodeB, i);
            }
        }

        /// <summary>Indices into <see cref="Edges"/> of all edges touching a node.</summary>
        public IReadOnlyList<int> GetIncidentEdges(int nodeId)
        {
            return _adjacency.TryGetValue(nodeId, out List<int> list) ? list : (IReadOnlyList<int>)System.Array.Empty<int>();
        }

        private void AddAdjacency(int nodeId, int edgeIndex)
        {
            if (!_adjacency.TryGetValue(nodeId, out List<int> list))
            {
                list = new List<int>();
                _adjacency[nodeId] = list;
            }

            list.Add(edgeIndex);
        }
    }

    /// <summary>Builds the road graph from the procedural city settings.</summary>
    public static class RoadGraphBuilder
    {
        public static int GetNodeId(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            return cell.Z * settings.CellsX + cell.X;
        }

        public static RoadGraph Build(CityGenerationSettings settings)
        {
            var nodes = new List<RoadNode>();
            var edges = new List<RoadEdge>();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
                float center = settings.CellSize / 2f;
                nodes.Add(new RoadNode(
                    GetNodeId(cell, settings), cell,
                    new Vector2(origin.x + center, origin.y + center)));
            }

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                int id = GetNodeId(cell, settings);
                if (cell.X + 1 < settings.CellsX)
                {
                    int east = GetNodeId(new CellCoordinate2D(cell.X + 1, cell.Z), settings);
                    edges.Add(new RoadEdge(id, east, RoadKind.MainAvenue, settings.CellSize));
                }

                if (cell.Z + 1 < settings.CellsZ)
                {
                    int north = GetNodeId(new CellCoordinate2D(cell.X, cell.Z + 1), settings);
                    edges.Add(new RoadEdge(id, north, RoadKind.SecondaryRoad, settings.CellSize));
                }
            }

            return new RoadGraph(nodes, edges);
        }
    }
}
