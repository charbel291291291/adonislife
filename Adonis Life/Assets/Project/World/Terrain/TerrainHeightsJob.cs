using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AdonisLife.World.Terrain
{
    /// <summary>
    /// Burst-compiled parallel job computing one terrain chunk's normalized heightmap. Each
    /// index samples the same pure <see cref="TerrainHeightField"/> the sequential path uses,
    /// so both paths produce identical terrain.
    /// </summary>
    [BurstCompile]
    public struct TerrainChunkHeightsJob : IJobParallelFor
    {
        public TerrainGenerationSettings Settings;
        public int ChunkX;
        public int ChunkZ;

        [WriteOnly] public NativeArray<float> Heights;

        public void Execute(int index)
        {
            int resolution = Settings.HeightmapResolution;
            int iz = index / resolution;
            int ix = index % resolution;
            float step = Settings.ChunkSize / (resolution - 1);
            float worldX = Settings.OriginX + ChunkX * Settings.ChunkSize + ix * step;
            float worldZ = Settings.OriginZ + ChunkZ * Settings.ChunkSize + iz * step;

            Heights[index] = TerrainHeightField.SampleHeight01(worldX, worldZ, Settings);
        }
    }

    /// <summary>
    /// Parallel (Jobs + Burst) drop-in for <see cref="TerrainHeightField.GenerateChunkHeights01"/>.
    /// </summary>
    public static class TerrainHeightFieldParallel
    {
        public const int BatchSize = 64;

        public static float[,] GenerateChunkHeights01(int chunkX, int chunkZ, TerrainGenerationSettings settings)
        {
            int resolution = settings.HeightmapResolution;
            int sampleCount = resolution * resolution;

            using (var buffer = new NativeArray<float>(sampleCount, Allocator.TempJob))
            {
                var job = new TerrainChunkHeightsJob
                {
                    Settings = settings,
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    Heights = buffer
                };

                job.Schedule(sampleCount, BatchSize).Complete();

                var heights = new float[resolution, resolution];
                for (int iz = 0; iz < resolution; iz++)
                {
                    for (int ix = 0; ix < resolution; ix++)
                    {
                        heights[iz, ix] = buffer[iz * resolution + ix];
                    }
                }

                return heights;
            }
        }
    }
}
