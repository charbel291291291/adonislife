using System.Collections.Generic;
using AdonisLife.World.Common;

namespace AdonisLife.World.Traffic
{
    /// <summary>A planned vehicle placement: a lane index and a normalized position along it.</summary>
    public readonly struct VehicleSpawn
    {
        public readonly int LaneIndex;
        public readonly float T;

        public VehicleSpawn(int laneIndex, float t)
        {
            LaneIndex = laneIndex;
            T = t;
        }
    }

    /// <summary>
    /// Deterministic vehicle spawn planning: distributes vehicles across lanes with a minimum
    /// along-lane spacing, resolved purely from the seed so the same plan is produced every run.
    /// </summary>
    public static class VehicleSpawnModel
    {
        public const float EndClearanceT = 0.08f;
        private const int AttemptsPerVehicle = 16;

        public static List<VehicleSpawn> PlanSpawns(
            IReadOnlyList<Lane> lanes, int vehicleCount, float minSpacingMeters, int seed)
        {
            var spawns = new List<VehicleSpawn>();
            if (lanes.Count == 0 || vehicleCount <= 0)
            {
                return spawns;
            }

            var occupied = new Dictionary<int, List<float>>();

            int attempt = 0;
            while (spawns.Count < vehicleCount && attempt < vehicleCount * AttemptsPerVehicle)
            {
                int laneIndex = (int)(DeterministicHash.Value01(attempt, 0, 17, seed) * lanes.Count);
                laneIndex = System.Math.Min(laneIndex, lanes.Count - 1);
                float t = EndClearanceT +
                          DeterministicHash.Value01(attempt, 1, 17, seed) * (1f - 2f * EndClearanceT);
                attempt++;

                float laneLength = lanes[laneIndex].Length;
                float minSpacingT = laneLength > 0f ? minSpacingMeters / laneLength : 1f;

                if (!occupied.TryGetValue(laneIndex, out List<float> taken))
                {
                    taken = new List<float>();
                    occupied[laneIndex] = taken;
                }

                bool tooClose = false;
                foreach (float existing in taken)
                {
                    if (System.Math.Abs(existing - t) < minSpacingT)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                taken.Add(t);
                spawns.Add(new VehicleSpawn(laneIndex, t));
            }

            return spawns;
        }
    }
}
