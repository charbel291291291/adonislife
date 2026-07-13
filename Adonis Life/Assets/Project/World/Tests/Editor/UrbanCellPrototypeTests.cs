using System;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class UrbanCellPrototypeTests
    {
        [Test]
        public void CellSize_Is250()
        {
            Assert.AreEqual(250f, UrbanCellLayout.CellSize);
        }

        [Test]
        public void CellCenter_Is125_125()
        {
            Assert.AreEqual(new Vector2(125f, 125f), UrbanCellLayout.GetCellCenter());
        }

        [Test]
        public void MainRoadWidth_Is20()
        {
            CellRect road = UrbanCellLayout.GetMainRoadRect();
            Assert.AreEqual(20f, road.Depth);
        }

        [Test]
        public void SecondaryRoadWidth_Is14()
        {
            CellRect road = UrbanCellLayout.GetSecondaryRoadRect();
            Assert.AreEqual(14f, road.Width);
        }

        [Test]
        public void SidewalkWidth_Is4()
        {
            Assert.AreEqual(4f, UrbanCellLayout.SidewalkWidth);
        }

        [Test]
        public void DevelopmentBlocks_RemainInsideCell()
        {
            foreach (UrbanCellBlock block in (UrbanCellBlock[])Enum.GetValues(typeof(UrbanCellBlock)))
            {
                CellRect rect = UrbanCellLayout.GetBlockRect(block);

                Assert.GreaterOrEqual(rect.XMin, 0f, $"{block} XMin out of bounds.");
                Assert.LessOrEqual(rect.XMax, UrbanCellLayout.CellSize, $"{block} XMax out of bounds.");
                Assert.GreaterOrEqual(rect.ZMin, 0f, $"{block} ZMin out of bounds.");
                Assert.LessOrEqual(rect.ZMax, UrbanCellLayout.CellSize, $"{block} ZMax out of bounds.");
            }
        }

        [Test]
        public void DevelopmentBlocks_DoNotOverlapRoads()
        {
            CellRect mainRoad = UrbanCellLayout.GetMainRoadRect();
            CellRect secondaryRoad = UrbanCellLayout.GetSecondaryRoadRect();

            foreach (UrbanCellBlock block in (UrbanCellBlock[])Enum.GetValues(typeof(UrbanCellBlock)))
            {
                CellRect rect = UrbanCellLayout.GetBlockRect(block);

                Assert.IsFalse(rect.Overlaps(mainRoad), $"{block} overlaps the main road.");
                Assert.IsFalse(rect.Overlaps(secondaryRoad), $"{block} overlaps the secondary road.");
            }
        }

        [Test]
        public void CalculatedDimensions_ArePositive()
        {
            Assert.Greater(UrbanCellLayout.GetMainRoadRect().Width, 0f);
            Assert.Greater(UrbanCellLayout.GetMainRoadRect().Depth, 0f);
            Assert.Greater(UrbanCellLayout.GetSecondaryRoadRect().Width, 0f);
            Assert.Greater(UrbanCellLayout.GetSecondaryRoadRect().Depth, 0f);

            foreach (UrbanCellBlock block in (UrbanCellBlock[])Enum.GetValues(typeof(UrbanCellBlock)))
            {
                CellRect rect = UrbanCellLayout.GetBlockRect(block);
                Assert.Greater(rect.Width, 0f, $"{block} width must be positive.");
                Assert.Greater(rect.Depth, 0f, $"{block} depth must be positive.");
            }
        }
    }
}
