using AdonisLife.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class TerrainHeightFieldTests
    {
        private static TerrainGenerationSettings DefaultSettings(int seed = 1234)
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
                seed: seed);
        }

        [Test]
        public void SampleHeight_IsDeterministicForSameSeed()
        {
            TerrainGenerationSettings a = DefaultSettings();
            TerrainGenerationSettings b = DefaultSettings();

            for (float x = -1000f; x < 0f; x += 97f)
            {
                for (float z = 0f; z < 1000f; z += 97f)
                {
                    Assert.AreEqual(
                        TerrainHeightField.SampleHeight(x, z, a),
                        TerrainHeightField.SampleHeight(x, z, b));
                }
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentTerrain()
        {
            TerrainGenerationSettings a = DefaultSettings(seed: 1234);
            TerrainGenerationSettings b = DefaultSettings(seed: 9876);

            bool anyDifferent = false;
            for (float x = -900f; x < -100f && !anyDifferent; x += 83f)
            {
                for (float z = 100f; z < 900f && !anyDifferent; z += 83f)
                {
                    if (!Mathf.Approximately(
                            TerrainHeightField.SampleHeight(x, z, a),
                            TerrainHeightField.SampleHeight(x, z, b)))
                    {
                        anyDifferent = true;
                    }
                }
            }

            Assert.IsTrue(anyDifferent, "Different seeds produced identical terrain.");
        }

        [Test]
        public void Heights_StayWithinValidRange()
        {
            TerrainGenerationSettings s = DefaultSettings();

            for (float x = -1000f; x <= 0f; x += 53f)
            {
                for (float z = 0f; z <= 1000f; z += 53f)
                {
                    float h = TerrainHeightField.SampleHeight(x, z, s);
                    Assert.GreaterOrEqual(h, 0f, $"Height below 0 at ({x},{z}).");
                    Assert.LessOrEqual(h, s.MaxHeight, $"Height above max at ({x},{z}).");
                }
            }
        }

        [Test]
        public void WestEdge_IsBelowSeaLevel()
        {
            TerrainGenerationSettings s = DefaultSettings();

            for (float z = 50f; z < 1000f; z += 100f)
            {
                float h = TerrainHeightField.SampleHeight(s.OriginX + 2f, z, s);
                Assert.Less(h, s.SeaLevel, $"Coast at z={z} is not underwater.");
            }
        }

        [Test]
        public void EastInterior_IsMostlyAboveSeaLevel()
        {
            TerrainGenerationSettings s = DefaultSettings();

            int above = 0;
            int total = 0;
            for (float z = 20f; z < 1000f; z += 40f)
            {
                float h = TerrainHeightField.SampleHeight(s.OriginX + s.TotalWidth - 50f, z, s);
                if (h > s.SeaLevel)
                {
                    above++;
                }

                total++;
            }

            Assert.Greater(above / (float)total, 0.6f, "Too little land above sea level in the east interior.");
        }

        [Test]
        public void River_CarvesBelowItsBanks()
        {
            TerrainGenerationSettings s = DefaultSettings();

            float x = s.OriginX + s.TotalWidth * 0.5f;
            float riverZ = TerrainHeightField.GetRiverCenterZ(x, s);
            float center = TerrainHeightField.SampleHeight(x, riverZ, s);
            float bankSouth = TerrainHeightField.SampleHeight(x, riverZ - s.RiverWidth * 4f, s);
            float bankNorth = TerrainHeightField.SampleHeight(x, riverZ + s.RiverWidth * 4f, s);

            Assert.Less(center, bankSouth, "River center is not below its south bank.");
            Assert.Less(center, bankNorth, "River center is not below its north bank.");
        }

        [Test]
        public void LakeCenter_IsBelowSeaLevel()
        {
            TerrainGenerationSettings s = DefaultSettings();
            Vector2 lake = TerrainHeightField.GetLakeCenter(s);

            float h = TerrainHeightField.SampleHeight(lake.x, lake.y, s);
            Assert.Less(h, s.SeaLevel, "Lake center is not below sea level.");
        }

        [Test]
        public void Cliff_RisesAcrossTheCliffLine()
        {
            TerrainGenerationSettings s = DefaultSettings();

            float x = s.OriginX + s.TotalWidth * 0.85f;
            float cliffLine = TerrainHeightField.GetCliffLineZ(x, s);
            float below = TerrainHeightField.SampleHeight(x, cliffLine - 25f, s);
            float above = TerrainHeightField.SampleHeight(x, cliffLine + 25f, s);

            Assert.Greater(above - below, s.CliffHeight * 0.5f,
                $"Cliff rise ({above - below}) is less than half the configured cliff height.");
        }

        [Test]
        public void AdjacentChunks_ShareIdenticalEdgeHeights()
        {
            TerrainGenerationSettings s = DefaultSettings();
            int res = s.HeightmapResolution;

            float[,] west = TerrainHeightField.GenerateChunkHeights01(0, 0, s);
            float[,] east = TerrainHeightField.GenerateChunkHeights01(1, 0, s);
            float[,] north = TerrainHeightField.GenerateChunkHeights01(0, 1, s);

            for (int i = 0; i < res; i++)
            {
                Assert.AreEqual(west[i, res - 1], east[i, 0], $"East edge mismatch at row {i}.");
                Assert.AreEqual(west[res - 1, i], north[0, i], $"North edge mismatch at column {i}.");
            }
        }

        [Test]
        public void ChunkHeights_HaveCorrectDimensionsAndRange()
        {
            TerrainGenerationSettings s = DefaultSettings();
            float[,] heights = TerrainHeightField.GenerateChunkHeights01(2, 2, s);

            Assert.AreEqual(s.HeightmapResolution, heights.GetLength(0));
            Assert.AreEqual(s.HeightmapResolution, heights.GetLength(1));

            foreach (float h in heights)
            {
                Assert.GreaterOrEqual(h, 0f);
                Assert.LessOrEqual(h, 1f);
            }
        }

        [Test]
        public void SplatWeights_AreNormalized()
        {
            TerrainGenerationSettings s = DefaultSettings();

            for (float x = -950f; x < 0f; x += 130f)
            {
                for (float z = 50f; z < 1000f; z += 130f)
                {
                    Vector3 w = TerrainHeightField.GetSplatWeights(x, z, s);
                    Assert.AreEqual(1f, w.x + w.y + w.z, 0.001f, $"Weights at ({x},{z}) do not sum to 1.");
                    Assert.GreaterOrEqual(w.x, 0f);
                    Assert.GreaterOrEqual(w.y, 0f);
                    Assert.GreaterOrEqual(w.z, 0f);
                }
            }
        }

        [Test]
        public void SplatWeights_ProduceSandNearCoast_AndGrassInland()
        {
            TerrainGenerationSettings s = DefaultSettings();

            // A point low on the coast slope should be sand-dominant.
            Vector3 coast = TerrainHeightField.GetSplatWeights(s.OriginX + 30f, 500f, s);
            Assert.Greater(coast.x, 0.5f, "Coast point is not sand-dominant.");

            // Find a grass-dominant point somewhere inland.
            bool grassFound = false;
            for (float x = -700f; x < -100f && !grassFound; x += 60f)
            {
                for (float z = 450f; z < 700f && !grassFound; z += 60f)
                {
                    if (TerrainHeightField.GetSplatWeights(x, z, s).y > 0.5f)
                    {
                        grassFound = true;
                    }
                }
            }

            Assert.IsTrue(grassFound, "No grass-dominant point found inland.");
        }
    }
}
