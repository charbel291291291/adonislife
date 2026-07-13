using System.Collections.Generic;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class RoadDetailLayoutTests
    {
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

        private static bool RectInside(CellRect inner, CellRect outer)
        {
            return inner.XMin >= outer.XMin && inner.XMax <= outer.XMax &&
                   inner.ZMin >= outer.ZMin && inner.ZMax <= outer.ZMax;
        }

        [Test]
        public void Crossings_AreFourPerCell_AndSpanTheirRoads()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> crossings = RoadDetailLayout.GetCrossingRects(cell, settings);
                Assert.AreEqual(4, crossings.Count);

                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                Assert.AreEqual(main.ZMin, crossings[0].ZMin);
                Assert.AreEqual(main.ZMax, crossings[0].ZMax);
                Assert.AreEqual(main.ZMin, crossings[1].ZMin);
                Assert.AreEqual(main.ZMax, crossings[1].ZMax);
                Assert.AreEqual(secondary.XMin, crossings[2].XMin);
                Assert.AreEqual(secondary.XMax, crossings[2].XMax);
                Assert.AreEqual(secondary.XMin, crossings[3].XMin);
                Assert.AreEqual(secondary.XMax, crossings[3].XMax);
            }
        }

        [Test]
        public void Medians_DoNotOverlapCrossingsOrIntersection()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> medians = RoadDetailLayout.GetMedianRects(cell, settings);
                List<CellRect> crossings = RoadDetailLayout.GetCrossingRects(cell, settings);
                CellRect intersection = RoadDetailLayout.GetIntersectionRect(cell, settings);

                Assert.AreEqual(2, medians.Count);

                foreach (CellRect median in medians)
                {
                    Assert.Greater(median.Width, 0f);
                    Assert.IsFalse(median.Overlaps(intersection), $"{cell} median overlaps intersection.");
                    foreach (CellRect crossing in crossings)
                    {
                        Assert.IsFalse(median.Overlaps(crossing), $"{cell} median overlaps a crossing.");
                    }
                }
            }
        }

        [Test]
        public void LaneDashes_StayInsideRoads_AndAvoidCrossingZone()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> dashes = RoadDetailLayout.GetLaneDashRects(cell, settings);
                List<CellRect> crossings = RoadDetailLayout.GetCrossingRects(cell, settings);
                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                Assert.Greater(dashes.Count, 0);

                foreach (CellRect dash in dashes)
                {
                    Assert.IsTrue(RectInside(dash, main) || RectInside(dash, secondary),
                        $"{cell} dash {dash.XMin},{dash.ZMin} is outside both roads.");

                    foreach (CellRect crossing in crossings)
                    {
                        Assert.IsFalse(dash.Overlaps(crossing), $"{cell} dash overlaps a crossing.");
                    }
                }
            }
        }

        [Test]
        public void DrainageStrips_StayInsideRoads_AndAvoidIntersectionAndCrossings()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> strips = RoadDetailLayout.GetDrainageRects(cell, settings);
                List<CellRect> crossings = RoadDetailLayout.GetCrossingRects(cell, settings);
                CellRect intersection = RoadDetailLayout.GetIntersectionRect(cell, settings);
                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                Assert.AreEqual(16, strips.Count);

                foreach (CellRect strip in strips)
                {
                    Assert.IsTrue(RectInside(strip, main) || RectInside(strip, secondary),
                        $"{cell} drainage strip is outside both roads.");
                    Assert.IsFalse(strip.Overlaps(intersection), $"{cell} drainage strip overlaps the intersection.");
                    foreach (CellRect crossing in crossings)
                    {
                        Assert.IsFalse(strip.Overlaps(crossing), $"{cell} drainage strip overlaps a crossing.");
                    }
                }
            }
        }

        [Test]
        public void Curbs_AreSixPerCell_WithPositiveDimensions()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> curbs = RoadDetailLayout.GetCurbRects(cell, settings);
                Assert.AreEqual(6, curbs.Count);

                foreach (CellRect curb in curbs)
                {
                    Assert.Greater(curb.Width, 0f);
                    Assert.Greater(curb.Depth, 0f);
                }
            }
        }

        [Test]
        public void CornerCurbs_SurroundTheIntersection()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> corners = RoadDetailLayout.GetCornerCurbRects(cell, settings);
                CellRect intersection = RoadDetailLayout.GetIntersectionRect(cell, settings);

                Assert.AreEqual(4, corners.Count);

                foreach (CellRect corner in corners)
                {
                    Assert.IsFalse(corner.Overlaps(intersection), $"{cell} corner curb overlaps the intersection.");
                    Assert.AreEqual(settings.SidewalkWidth, corner.Width, 0.001f);
                    Assert.AreEqual(settings.SidewalkWidth, corner.Depth, 0.001f);
                }
            }
        }

        [Test]
        public void UtilityStrips_DoNotOverlapRoadsOrBlocks()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<CellRect> strips = RoadDetailLayout.GetUtilityStripRects(cell, settings);
                CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                Assert.AreEqual(8, strips.Count);

                foreach (CellRect strip in strips)
                {
                    Assert.IsFalse(strip.Overlaps(main), $"{cell} utility strip overlaps the main road.");
                    Assert.IsFalse(strip.Overlaps(secondary), $"{cell} utility strip overlaps the secondary road.");

                    foreach (DevelopmentBlockQuadrant quadrant in
                        (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
                    {
                        CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                        Assert.IsFalse(strip.Overlaps(block), $"{cell} utility strip overlaps block {quadrant}.");
                    }
                }
            }
        }

        [Test]
        public void StreetLights_SitOnMainSidewalks_OutsideIntersectionZone()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                List<Vector2> lights = RoadDetailLayout.GetStreetLightPositions(cell, settings);
                CellRect north = ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: true);
                CellRect south = ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: false);
                CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

                Assert.Greater(lights.Count, 0);

                foreach (Vector2 light in lights)
                {
                    bool onNorth = light.x >= north.XMin && light.x <= north.XMax && light.y >= north.ZMin && light.y <= north.ZMax;
                    bool onSouth = light.x >= south.XMin && light.x <= south.XMax && light.y >= south.ZMin && light.y <= south.ZMax;
                    Assert.IsTrue(onNorth || onSouth, $"{cell} street light at {light} is off the main sidewalks.");

                    Assert.IsFalse(
                        light.x > secondary.XMin - settings.SidewalkWidth && light.x < secondary.XMax + settings.SidewalkWidth,
                        $"{cell} street light at {light} is inside the intersection zone.");
                }
            }
        }

        [Test]
        public void TrafficSigns_AreFourPerCell()
        {
            CityGenerationSettings settings = DefaultSettings();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                Assert.AreEqual(4, RoadDetailLayout.GetTrafficSignPositions(cell, settings).Count);
            }
        }

        [Test]
        public void DetailLayout_IsDeterministic()
        {
            CityGenerationSettings settings = DefaultSettings();
            CellCoordinate2D cell = new CellCoordinate2D(1, 1);

            Assert.AreEqual(
                RoadDetailLayout.GetLaneDashRects(cell, settings),
                RoadDetailLayout.GetLaneDashRects(cell, settings));
            Assert.AreEqual(
                RoadDetailLayout.GetDrainageRects(cell, settings),
                RoadDetailLayout.GetDrainageRects(cell, settings));
            Assert.AreEqual(
                RoadDetailLayout.GetStreetLightPositions(cell, settings),
                RoadDetailLayout.GetStreetLightPositions(cell, settings));
        }
    }
}
