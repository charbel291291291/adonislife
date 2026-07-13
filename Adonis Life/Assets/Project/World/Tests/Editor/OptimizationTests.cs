using AdonisLife.World.Terrain;
using NUnit.Framework;

namespace AdonisLife.World.Tests.Editor
{
    public class OptimizationTests
    {
        private static TerrainGenerationSettings DefaultSettings()
        {
            return new TerrainGenerationSettings(
                chunksX: 4,
                chunksZ: 4,
                chunkSize: 250f,
                heightmapResolution: 129,
                originX: -1000f,
                originZ: 0f,
                maxHeight: 60f,
                seaLevel: 6f,
                coastWidth: 180f,
                beachBand: 2.5f,
                riverWidth: 14f,
                riverDepth: 7f,
                lakeRadius: 60f,
                lakeDepth: 5f,
                cliffHeight: 20f,
                seed: 1234);
        }

        [Test]
        public void ParallelHeights_MatchSequentialHeights()
        {
            TerrainGenerationSettings settings = DefaultSettings();

            float[,] sequential = TerrainHeightField.GenerateChunkHeights01(1, 2, settings);
            float[,] parallel = TerrainHeightFieldParallel.GenerateChunkHeights01(1, 2, settings);

            int resolution = settings.HeightmapResolution;
            Assert.AreEqual(resolution, parallel.GetLength(0));
            Assert.AreEqual(resolution, parallel.GetLength(1));

            for (int iz = 0; iz < resolution; iz++)
            {
                for (int ix = 0; ix < resolution; ix++)
                {
                    Assert.AreEqual(sequential[iz, ix], parallel[iz, ix], 1e-5f,
                        $"Height mismatch at ({ix},{iz}).");
                }
            }
        }

        [Test]
        public void ParallelHeights_AreDeterministicAcrossRuns()
        {
            TerrainGenerationSettings settings = DefaultSettings();

            float[,] first = TerrainHeightFieldParallel.GenerateChunkHeights01(0, 0, settings);
            float[,] second = TerrainHeightFieldParallel.GenerateChunkHeights01(0, 0, settings);

            int resolution = settings.HeightmapResolution;
            for (int iz = 0; iz < resolution; iz++)
            {
                for (int ix = 0; ix < resolution; ix++)
                {
                    Assert.AreEqual(first[iz, ix], second[iz, ix]);
                }
            }
        }
    }
}
