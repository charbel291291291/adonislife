using System;
using System.Collections.Generic;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Pure distance-based LOD level calculation for streamed chunks. LOD 0 is the highest
    /// detail (nearest ring); each configured distance threshold starts the next, coarser level.
    /// </summary>
    public static class ChunkLodCalculator
    {
        /// <summary>
        /// Validates that LOD distance thresholds are positive and strictly ascending.
        /// </summary>
        public static bool AreValidLodDistances(IReadOnlyList<float> lodDistances)
        {
            if (lodDistances == null || lodDistances.Count == 0)
            {
                return false;
            }

            float previous = 0f;
            foreach (float distance in lodDistances)
            {
                if (distance <= previous)
                {
                    return false;
                }

                previous = distance;
            }

            return true;
        }

        /// <summary>
        /// Returns the LOD level for a chunk at the given distance from the nearest observer.
        /// Distances below the first threshold map to level 0; distances beyond the last
        /// threshold map to the coarsest level (<c>lodDistances.Count</c>).
        /// </summary>
        public static int GetLodLevel(float distanceToNearestObserver, IReadOnlyList<float> lodDistances)
        {
            if (!AreValidLodDistances(lodDistances))
            {
                throw new ArgumentException("LOD distances must be positive and strictly ascending.", nameof(lodDistances));
            }

            if (distanceToNearestObserver < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToNearestObserver), "Distance must not be negative.");
            }

            for (int level = 0; level < lodDistances.Count; level++)
            {
                if (distanceToNearestObserver <= lodDistances[level])
                {
                    return level;
                }
            }

            return lodDistances.Count;
        }
    }
}
