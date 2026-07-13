using System.Collections.Generic;

namespace AdonisLife.World.Traffic
{
    /// <summary>
    /// Navigation over the road graph: Dijkstra shortest path between intersection nodes.
    /// Deterministic — equal-cost ties resolve to the lowest node id.
    /// </summary>
    public static class RoadPathfinder
    {
        /// <summary>
        /// Shortest path from one node to another as an ordered node id list (inclusive), or
        /// null if no path exists.
        /// </summary>
        public static List<int> FindPath(RoadGraph graph, int startNodeId, int goalNodeId)
        {
            if (startNodeId == goalNodeId)
            {
                return new List<int> { startNodeId };
            }

            var distance = new Dictionary<int, float>();
            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            foreach (RoadNode node in graph.Nodes)
            {
                distance[node.Id] = float.MaxValue;
            }

            distance[startNodeId] = 0f;

            while (true)
            {
                int current = -1;
                float best = float.MaxValue;
                foreach (KeyValuePair<int, float> entry in distance)
                {
                    if (!visited.Contains(entry.Key) &&
                        (entry.Value < best || (entry.Value == best && (current == -1 || entry.Key < current))))
                    {
                        best = entry.Value;
                        current = entry.Key;
                    }
                }

                if (current == -1 || best == float.MaxValue)
                {
                    return null;
                }

                if (current == goalNodeId)
                {
                    break;
                }

                visited.Add(current);

                foreach (int edgeIndex in graph.GetIncidentEdges(current))
                {
                    RoadEdge edge = graph.Edges[edgeIndex];
                    int neighbor = edge.NodeA == current ? edge.NodeB : edge.NodeA;
                    if (visited.Contains(neighbor))
                    {
                        continue;
                    }

                    float candidate = distance[current] + edge.Length;
                    if (candidate < distance[neighbor])
                    {
                        distance[neighbor] = candidate;
                        previous[neighbor] = current;
                    }
                }
            }

            var path = new List<int> { goalNodeId };
            int walk = goalNodeId;
            while (previous.TryGetValue(walk, out int parent))
            {
                path.Add(parent);
                walk = parent;
            }

            path.Reverse();
            return path[0] == startNodeId ? path : null;
        }

        /// <summary>Total length of a node path over the graph's edges.</summary>
        public static float GetPathLength(RoadGraph graph, List<int> path)
        {
            float total = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                foreach (int edgeIndex in graph.GetIncidentEdges(path[i]))
                {
                    RoadEdge edge = graph.Edges[edgeIndex];
                    if ((edge.NodeA == path[i] && edge.NodeB == path[i + 1]) ||
                        (edge.NodeB == path[i] && edge.NodeA == path[i + 1]))
                    {
                        total += edge.Length;
                        break;
                    }
                }
            }

            return total;
        }
    }
}
