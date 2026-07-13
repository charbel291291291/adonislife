using System;
using System.Collections.Generic;

namespace AdonisLife.World.Traffic
{
    /// <summary>
    /// Generic deterministic Dijkstra over any node/edge structure expressed as a neighbor
    /// function. Used by graphs that are not road graphs (e.g. the pedestrian sidewalk graph).
    /// Equal-cost ties resolve to the lowest node id.
    /// </summary>
    public static class GraphPathfinder
    {
        /// <summary>
        /// Shortest path between two nodes, inclusive, or null if unreachable.
        /// <paramref name="getNeighbors"/> returns (neighborId, edgeCost) pairs for a node.
        /// </summary>
        public static List<int> FindPath(
            IEnumerable<int> allNodeIds,
            Func<int, IEnumerable<(int neighbor, float cost)>> getNeighbors,
            int startNodeId,
            int goalNodeId)
        {
            if (startNodeId == goalNodeId)
            {
                return new List<int> { startNodeId };
            }

            var distance = new Dictionary<int, float>();
            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            foreach (int id in allNodeIds)
            {
                distance[id] = float.MaxValue;
            }

            if (!distance.ContainsKey(startNodeId) || !distance.ContainsKey(goalNodeId))
            {
                return null;
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

                foreach ((int neighbor, float cost) in getNeighbors(current))
                {
                    if (visited.Contains(neighbor) || !distance.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    float candidate = distance[current] + cost;
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
    }
}
