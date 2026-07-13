using System;
using System.Collections.Generic;
using System.Linq;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class ProceduralCityPrototypeTests
    {
        private static readonly DevelopmentBlockQuadrant[] AllQuadrants =
            (DevelopmentBlockQuadrant[])Enum.GetValues(typeof(DevelopmentBlockQuadrant));

        private static CityGenerationSettings DefaultSettings()
        {
            return new CityGenerationSettings(
                cellsX: 3,
                cellsZ: 3,
                cellSize: 250f,
                mainRoadWidth: 20f,
                secondaryRoadWidth: 14f,
                sidewalkWidth: 4f,
                blockInset: 2f,
                seed: 0);
        }

        [Test]
        public void CityDimensions_Are750x750()
        {
            CityGenerationSettings settings = DefaultSettings();
            Vector2 dimensions = ProceduralCityLayout.GetCityDimensions(settings);

            Assert.AreEqual(750f, dimensions.x);
            Assert.AreEqual(750f, dimensions.y);
        }

        [Test]
        public void CellCount_Is9()
        {
            CityGenerationSettings settings = DefaultSettings();
            List<CellCoordinate2D> cells = ProceduralCityLayout.GetAllCells(settings).ToList();

            Assert.AreEqual(9, cells.Count);
            Assert.AreEqual(9, cells.Distinct().Count());
        }

        [Test]
        public void RoadSegments_ContainNoDuplicates()
        {
            CityGenerationSettings settings = DefaultSettings();
            List<CellRect> roads = new List<CellRect>();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                roads.Add(ProceduralCityLayout.GetMainRoadRect(cell, settings));
                roads.Add(ProceduralCityLayout.GetSecondaryRoadRect(cell, settings));
            }

            Assert.AreEqual(18, roads.Count);

            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = i + 1; j < roads.Count; j++)
                {
                    Assert.AreNotEqual(roads[i], roads[j], $"Duplicate road segment found at indices {i} and {j}.");
                }
            }
        }

        [Test]
        public void DevelopmentBlocks_DoNotOverlap()
        {
            CityGenerationSettings settings = DefaultSettings();
            List<CellRect> blocks = new List<CellRect>();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    blocks.Add(ProceduralCityLayout.GetBlockRect(cell, settings, quadrant));
                }
            }

            Assert.AreEqual(settings.CellsX * settings.CellsZ * 4, blocks.Count);

            for (int i = 0; i < blocks.Count; i++)
            {
                for (int j = i + 1; j < blocks.Count; j++)
                {
                    Assert.IsFalse(blocks[i].Overlaps(blocks[j]), $"Blocks at indices {i} and {j} overlap.");
                }
            }
        }

        [Test]
        public void DevelopmentBlocks_HaveValidDimensions()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    CellRect rect = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                    Assert.Greater(rect.Width, 0f, $"{cell} {quadrant} width must be positive.");
                    Assert.Greater(rect.Depth, 0f, $"{cell} {quadrant} depth must be positive.");
                }
            }
        }

        [Test]
        public void SameSeed_ProducesDeterministicOutput()
        {
            CityGenerationSettings settingsA = DefaultSettings();
            CityGenerationSettings settingsB = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settingsA))
            {
                Assert.AreEqual(
                    ProceduralCityLayout.GetMainRoadRect(cell, settingsA),
                    ProceduralCityLayout.GetMainRoadRect(cell, settingsB));

                Assert.AreEqual(
                    ProceduralCityLayout.GetSecondaryRoadRect(cell, settingsA),
                    ProceduralCityLayout.GetSecondaryRoadRect(cell, settingsB));

                foreach (DevelopmentBlockQuadrant quadrant in AllQuadrants)
                {
                    Assert.AreEqual(
                        ProceduralCityLayout.GetBlockRect(cell, settingsA, quadrant),
                        ProceduralCityLayout.GetBlockRect(cell, settingsB, quadrant));
                }
            }
        }

        [Test]
        public void CityCenter_IsCorrectlyPositioned()
        {
            CityGenerationSettings settings = DefaultSettings();
            Vector2 center = ProceduralCityLayout.GetCityCenter(settings);

            Assert.AreEqual(new Vector2(375f, 375f), center);
        }

        [Test]
        public void RoadNetwork_IsContinuousAcrossCellBoundaries()
        {
            CityGenerationSettings settings = DefaultSettings();

            for (int z = 0; z < settings.CellsZ; z++)
            {
                for (int x = 0; x < settings.CellsX - 1; x++)
                {
                    CellRect current = ProceduralCityLayout.GetMainRoadRect(new CellCoordinate2D(x, z), settings);
                    CellRect next = ProceduralCityLayout.GetMainRoadRect(new CellCoordinate2D(x + 1, z), settings);

                    Assert.AreEqual(current.XMax, next.XMin, $"Main avenue gap/overlap between cells ({x},{z}) and ({x + 1},{z}).");
                    Assert.AreEqual(current.ZMin, next.ZMin);
                    Assert.AreEqual(current.ZMax, next.ZMax);
                }
            }

            for (int x = 0; x < settings.CellsX; x++)
            {
                for (int z = 0; z < settings.CellsZ - 1; z++)
                {
                    CellRect current = ProceduralCityLayout.GetSecondaryRoadRect(new CellCoordinate2D(x, z), settings);
                    CellRect next = ProceduralCityLayout.GetSecondaryRoadRect(new CellCoordinate2D(x, z + 1), settings);

                    Assert.AreEqual(current.ZMax, next.ZMin, $"Secondary road gap/overlap between cells ({x},{z}) and ({x},{z + 1}).");
                    Assert.AreEqual(current.XMin, next.XMin);
                    Assert.AreEqual(current.XMax, next.XMax);
                }
            }
        }
    }
}
