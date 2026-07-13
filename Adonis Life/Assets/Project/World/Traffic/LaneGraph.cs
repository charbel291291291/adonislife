using System.Collections.Generic;
using AdonisLife.World.ProceduralCity;
using UnityEngine;

namespace AdonisLife.World.Traffic
{
    /// <summary>
    /// One directed travel lane along a road edge, offset to the right of the travel direction
    /// (right-hand traffic).
    /// </summary>
    public readonly struct Lane
    {
        public readonly int EdgeIndex;
        public readonly RoadKind Kind;
        public readonly Vector2 Start;
        public readonly Vector2 End;

        public float Length => Vector2.Distance(Start, End);

        public Lane(int edgeIndex, RoadKind kind, Vector2 start, Vector2 end)
        {
            EdgeIndex = edgeIndex;
            Kind = kind;
            Start = start;
            End = end;
        }

        /// <summary>Point along the lane at normalized parameter t in [0, 1].</summary>
        public Vector2 GetPoint(float t)
        {
            return Vector2.Lerp(Start, End, Mathf.Clamp01(t));
        }
    }

    /// <summary>
    /// Expands the road graph into directed lanes: the main avenue carries two lanes per
    /// direction on each side of its median, the secondary road one lane per direction.
    /// </summary>
    public static class LaneGraphBuilder
    {
        public const int MainLanesPerDirection = 2;
        public const int SecondaryLanesPerDirection = 1;

        public static List<Lane> Build(RoadGraph graph, CityGenerationSettings settings)
        {
            var lanes = new List<Lane>();

            for (int edgeIndex = 0; edgeIndex < graph.Edges.Count; edgeIndex++)
            {
                RoadEdge edge = graph.Edges[edgeIndex];
                Vector2 a = graph.Nodes[edge.NodeA].Position;
                Vector2 b = graph.Nodes[edge.NodeB].Position;

                AddDirectionalLanes(lanes, edgeIndex, edge.Kind, a, b, settings);
                AddDirectionalLanes(lanes, edgeIndex, edge.Kind, b, a, settings);
            }

            return lanes;
        }

        private static void AddDirectionalLanes(
            List<Lane> lanes, int edgeIndex, RoadKind kind, Vector2 from, Vector2 to, CityGenerationSettings settings)
        {
            Vector2 direction = (to - from).normalized;
            var right = new Vector2(direction.y, -direction.x);

            int laneCount;
            float firstOffset;
            float laneWidth;
            if (kind == RoadKind.MainAvenue)
            {
                laneCount = MainLanesPerDirection;
                float medianHalf = RoadDetailLayout.MedianWidth / 2f;
                laneWidth = (settings.MainRoadWidth / 2f - medianHalf) / MainLanesPerDirection;
                firstOffset = medianHalf + laneWidth / 2f;
            }
            else
            {
                laneCount = SecondaryLanesPerDirection;
                laneWidth = settings.SecondaryRoadWidth / 2f;
                firstOffset = laneWidth / 2f;
            }

            for (int i = 0; i < laneCount; i++)
            {
                Vector2 offset = right * (firstOffset + i * laneWidth);
                lanes.Add(new Lane(edgeIndex, kind, from + offset, to + offset));
            }
        }
    }
}
