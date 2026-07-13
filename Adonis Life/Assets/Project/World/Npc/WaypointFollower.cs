using System.Collections.Generic;
using UnityEngine;

namespace AdonisLife.World.Npc
{
    /// <summary>
    /// Pure waypoint-following math shared by pedestrian and vehicle agents: move toward the
    /// current target waypoint at a given speed, advancing (and optionally looping) when
    /// reached. Stateless so it can be unit tested directly.
    /// </summary>
    public static class WaypointFollower
    {
        public const float ArrivalEpsilon = 0.05f;

        /// <summary>
        /// One movement step. Returns the new position and the (possibly advanced) target
        /// waypoint index. When the last waypoint is reached and <paramref name="loop"/> is
        /// false, the index stays at the last waypoint.
        /// </summary>
        public static (Vector2 position, int targetIndex) Step(
            Vector2 position, int targetIndex, IReadOnlyList<Vector2> waypoints,
            float speed, float deltaTime, bool loop)
        {
            if (waypoints == null || waypoints.Count == 0 || speed <= 0f || deltaTime <= 0f)
            {
                return (position, targetIndex);
            }

            targetIndex = Mathf.Clamp(targetIndex, 0, waypoints.Count - 1);
            float budget = speed * deltaTime;

            while (budget > 0f)
            {
                Vector2 target = waypoints[targetIndex];
                float distance = Vector2.Distance(position, target);

                if (distance <= budget || distance <= ArrivalEpsilon)
                {
                    position = target;
                    budget -= distance;

                    if (targetIndex == waypoints.Count - 1)
                    {
                        if (!loop)
                        {
                            return (position, targetIndex);
                        }

                        targetIndex = 0;
                    }
                    else
                    {
                        targetIndex++;
                    }
                }
                else
                {
                    position += (target - position).normalized * budget;
                    budget = 0f;
                }
            }

            return (position, targetIndex);
        }
    }
}
