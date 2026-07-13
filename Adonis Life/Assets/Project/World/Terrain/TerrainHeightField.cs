using UnityEngine;

namespace AdonisLife.World.Terrain
{
    /// <summary>
    /// Deterministic, continuous height field for the procedural terrain region. All features
    /// (rolling hills, west coastline, beach band, meandering river, lake, north cliff plateau)
    /// are functions of world position and the settings seed only, so any two chunks sampled at
    /// the same world coordinate always agree — chunk edges are continuous by construction.
    /// Contains no scene, editor, or asset dependencies.
    /// </summary>
    public static class TerrainHeightField
    {
        private const float SeaFloorHeight = 1f;
        private const float HillNoiseScale = 1f / 300f;
        private const int HillOctaves = 4;
        private const float RiverMeanderScale = 0.004f;
        private const float CliffTransitionWidth = 10f;
        private const float CliffLineNoiseAmplitude = 30f;
        private const float BeachFlattenStrength = 0.85f;
        private const float RockSlopeThreshold = 0.7f;
        private const float SlopeSampleDistance = 1f;

        /// <summary>Terrain surface height in meters at a world position. Always in [0, MaxHeight].</summary>
        public static float SampleHeight(float worldX, float worldZ, TerrainGenerationSettings s)
        {
            float hillAmplitude = Mathf.Max(0f, s.MaxHeight - s.CliffHeight - s.SeaLevel - 8f);
            float baseHeight = s.SeaLevel + 4f + Fbm(worldX * HillNoiseScale, worldZ * HillNoiseScale, s.Seed) * hillAmplitude;

            // North cliff plateau: a sharp rise across a noise-modulated east-west line.
            float cliffLine = GetCliffLineZ(worldX, s);
            float cliffBlend = SmoothStep01((worldZ - cliffLine) / CliffTransitionWidth);
            float height = baseHeight + s.CliffHeight * cliffBlend;

            // West coastline: heights fall to the sea floor toward the west edge.
            float coastBlend = SmoothStep01((worldX - s.OriginX) / s.CoastWidth);
            height = Mathf.Lerp(SeaFloorHeight, height, coastBlend);

            // River: carve a channel along the meandering path down toward the sea.
            float riverDistance = Mathf.Abs(worldZ - GetRiverCenterZ(worldX, s));
            float riverBlend = SmoothStep01(1f - riverDistance / (s.RiverWidth * 2f));
            height -= s.RiverDepth * riverBlend;

            // Lake: radial depression below sea level.
            Vector2 lakeCenter = GetLakeCenter(s);
            float lakeDistance = Vector2.Distance(new Vector2(worldX, worldZ), lakeCenter);
            float lakeBlend = SmoothStep01(1f - lakeDistance / s.LakeRadius);
            height = Mathf.Lerp(height, s.SeaLevel - s.LakeDepth, lakeBlend);

            // Beach: flatten heights that sit within the beach band around sea level.
            float beachWeight = Mathf.Clamp01(1f - Mathf.Abs(height - s.SeaLevel) / s.BeachBand);
            height = Mathf.Lerp(height, s.SeaLevel + 0.2f, beachWeight * BeachFlattenStrength);

            return Mathf.Clamp(height, 0f, s.MaxHeight);
        }

        /// <summary>Normalized height (0..1) as required by Unity terrain heightmaps.</summary>
        public static float SampleHeight01(float worldX, float worldZ, TerrainGenerationSettings s)
        {
            return SampleHeight(worldX, worldZ, s) / s.MaxHeight;
        }

        /// <summary>Z coordinate of the river center line at a given X.</summary>
        public static float GetRiverCenterZ(float worldX, TerrainGenerationSettings s)
        {
            float phase = (s.Seed & 1023) * 0.006135f;
            return s.OriginZ + s.TotalDepth * 0.35f +
                   Mathf.Sin((worldX - s.OriginX) * RiverMeanderScale * 2f * Mathf.PI + phase) * s.TotalDepth * 0.06f;
        }

        /// <summary>Z coordinate of the cliff base line at a given X.</summary>
        public static float GetCliffLineZ(float worldX, TerrainGenerationSettings s)
        {
            float noise = ValueNoise(worldX * 0.01f, 0.5f, s.Seed + 7919) * 2f - 1f;
            return s.OriginZ + s.TotalDepth * 0.75f + noise * CliffLineNoiseAmplitude;
        }

        /// <summary>World-space center of the lake depression.</summary>
        public static Vector2 GetLakeCenter(TerrainGenerationSettings s)
        {
            return new Vector2(s.OriginX + s.TotalWidth * 0.62f, s.OriginZ + s.TotalDepth * 0.58f);
        }

        /// <summary>
        /// Per-chunk normalized heightmap, indexed [z, x] as Unity's SetHeights expects. Sample
        /// positions of adjacent chunks share their boundary coordinate, so edges match exactly.
        /// </summary>
        public static float[,] GenerateChunkHeights01(int chunkX, int chunkZ, TerrainGenerationSettings s)
        {
            int res = s.HeightmapResolution;
            float step = s.ChunkSize / (res - 1);
            float originX = s.OriginX + chunkX * s.ChunkSize;
            float originZ = s.OriginZ + chunkZ * s.ChunkSize;

            float[,] heights = new float[res, res];
            for (int iz = 0; iz < res; iz++)
            {
                for (int ix = 0; ix < res; ix++)
                {
                    heights[iz, ix] = SampleHeight01(originX + ix * step, originZ + iz * step, s);
                }
            }

            return heights;
        }

        /// <summary>
        /// Vegetation mask / splat weights at a world position: (sand, grass, rock), normalized
        /// to sum to 1. Sand near sea level, rock on steep slopes, grass elsewhere.
        /// </summary>
        public static Vector3 GetSplatWeights(float worldX, float worldZ, TerrainGenerationSettings s)
        {
            float height = SampleHeight(worldX, worldZ, s);

            float dhdx = (SampleHeight(worldX + SlopeSampleDistance, worldZ, s) -
                          SampleHeight(worldX - SlopeSampleDistance, worldZ, s)) / (2f * SlopeSampleDistance);
            float dhdz = (SampleHeight(worldX, worldZ + SlopeSampleDistance, s) -
                          SampleHeight(worldX, worldZ - SlopeSampleDistance, s)) / (2f * SlopeSampleDistance);
            float slope = Mathf.Sqrt(dhdx * dhdx + dhdz * dhdz);

            float sand = Mathf.Clamp01(1f - (height - (s.SeaLevel + s.BeachBand * 0.5f)) / s.BeachBand);
            float rock = Mathf.Clamp01((slope - RockSlopeThreshold) / RockSlopeThreshold);
            float grass = Mathf.Clamp01(1f - sand - rock);

            float sum = sand + grass + rock;
            return new Vector3(sand / sum, grass / sum, rock / sum);
        }

        /// <summary>Fractal Brownian motion over hash-based value noise, output in [0, 1].</summary>
        public static float Fbm(float x, float z, int seed)
        {
            float amplitude = 0.5f;
            float frequency = 1f;
            float total = 0f;
            float normalization = 0f;

            for (int octave = 0; octave < HillOctaves; octave++)
            {
                total += ValueNoise(x * frequency, z * frequency, seed + octave * 101) * amplitude;
                normalization += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return total / normalization;
        }

        /// <summary>Bilinear-interpolated lattice value noise, output in [0, 1].</summary>
        public static float ValueNoise(float x, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int z0 = Mathf.FloorToInt(z);
            float fx = SmoothStep01(x - x0);
            float fz = SmoothStep01(z - z0);

            float v00 = Hash01(x0, z0, seed);
            float v10 = Hash01(x0 + 1, z0, seed);
            float v01 = Hash01(x0, z0 + 1, seed);
            float v11 = Hash01(x0 + 1, z0 + 1, seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fz);
        }

        /// <summary>Deterministic integer-lattice hash, output in [0, 1).</summary>
        public static float Hash01(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 0x9E3779B1u;
                h = (h ^ (h >> 15)) * 0x85EBCA6Bu;
                h ^= (uint)z * 0xC2B2AE35u;
                h = (h ^ (h >> 13)) * 0x27D4EB2Fu;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
