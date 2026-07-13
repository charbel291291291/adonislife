namespace AdonisLife.World.Common
{
    /// <summary>
    /// Deterministic integer hashing shared by procedural generators. Same inputs always produce
    /// the same output on every platform, which keeps all generation reproducible from a seed.
    /// </summary>
    public static class DeterministicHash
    {
        /// <summary>Hashes three lattice coordinates and a seed to a float in [0, 1).</summary>
        public static float Value01(int a, int b, int c, int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)a * 0x9E3779B1u;
                h = (h ^ (h >> 15)) * 0x85EBCA6Bu;
                h ^= (uint)b * 0xC2B2AE35u;
                h = (h ^ (h >> 13)) * 0x27D4EB2Fu;
                h ^= (uint)c * 0x165667B1u;
                h = (h ^ (h >> 16)) * 0x85EBCA6Bu;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
