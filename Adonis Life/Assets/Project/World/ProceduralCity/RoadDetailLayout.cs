using System.Collections.Generic;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.ProceduralCity
{
    /// <summary>
    /// Pure geometry calculations for the road network detail layer: lane markings, medians,
    /// intersection patches, pedestrian crossings, traffic islands, curbs, drainage strips,
    /// utility strips, street light placements, and traffic sign placements. All footprints are
    /// derived from <see cref="ProceduralCityLayout"/> so details always align with the roads
    /// they decorate. Contains no scene, editor, or asset dependencies.
    /// </summary>
    public static class RoadDetailLayout
    {
        public const float LaneMarkingWidth = 0.15f;
        public const float LaneDashLength = 4f;
        public const float LaneDashPeriod = 12f;
        public const float MedianWidth = 1.5f;
        public const float MedianEndSetback = 2f;
        public const float CrossingWidth = 3f;
        public const float RefugeIslandDepth = 2f;
        public const float CurbWidth = 0.3f;
        public const float CornerCurbSize = 4f;
        public const float DrainageStripWidth = 0.4f;
        public const float UtilityStripWidth = 0.5f;
        public const float StreetLightSpacing = 50f;
        public const float StreetLightEdgeOffset = 25f;

        /// <summary>Surface patch where the main avenue and secondary road cross.</summary>
        public static CellRect GetIntersectionRect(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            return new CellRect(secondary.XMin, secondary.XMax, main.ZMin, main.ZMax);
        }

        /// <summary>
        /// Four zebra crossings per cell, one on each intersection approach, placed just outside
        /// the sidewalk bands so they connect sidewalk to sidewalk.
        /// </summary>
        public static List<CellRect> GetCrossingRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            float sw = settings.SidewalkWidth;

            return new List<CellRect>
            {
                // Crossing the main avenue, west and east of the intersection.
                new CellRect(secondary.XMin - sw - CrossingWidth, secondary.XMin - sw, main.ZMin, main.ZMax),
                new CellRect(secondary.XMax + sw, secondary.XMax + sw + CrossingWidth, main.ZMin, main.ZMax),
                // Crossing the secondary road, south and north of the intersection.
                new CellRect(secondary.XMin, secondary.XMax, main.ZMin - sw - CrossingWidth, main.ZMin - sw),
                new CellRect(secondary.XMin, secondary.XMax, main.ZMax + sw, main.ZMax + sw + CrossingWidth)
            };
        }

        /// <summary>
        /// Raised central median on the main avenue, split into a west and an east segment that
        /// stop short of the pedestrian crossings.
        /// </summary>
        public static List<CellRect> GetMedianRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            List<CellRect> crossings = GetCrossingRects(cell, settings);

            float zMin = main.CenterZ - MedianWidth / 2f;
            float zMax = main.CenterZ + MedianWidth / 2f;
            float westEnd = crossings[0].XMin - MedianEndSetback;
            float eastStart = crossings[1].XMax + MedianEndSetback;

            return new List<CellRect>
            {
                new CellRect(origin.x, westEnd, zMin, zMax),
                new CellRect(eastStart, origin.x + settings.CellSize, zMin, zMax)
            };
        }

        /// <summary>
        /// Small raised pedestrian refuge islands at the median line inside the two long
        /// crossings over the main avenue.
        /// </summary>
        public static List<CellRect> GetRefugeIslandRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            List<CellRect> crossings = GetCrossingRects(cell, settings);
            float zMin = main.CenterZ - RefugeIslandDepth / 2f;
            float zMax = main.CenterZ + RefugeIslandDepth / 2f;

            return new List<CellRect>
            {
                new CellRect(crossings[0].XMin, crossings[0].XMax, zMin, zMax),
                new CellRect(crossings[1].XMin, crossings[1].XMax, zMin, zMax)
            };
        }

        /// <summary>
        /// Dashed lane divider markings. The main avenue gets two divider lines (one per travel
        /// direction, offset a quarter road width from center); the secondary road gets a single
        /// center line. Dashes overlapping the intersection or crossing zone are omitted.
        /// </summary>
        public static List<CellRect> GetLaneDashRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            List<CellRect> crossings = GetCrossingRects(cell, settings);
            List<CellRect> dashes = new List<CellRect>();

            float half = LaneMarkingWidth / 2f;

            // Main avenue: dashes run along X; exclusion spans from west crossing to east crossing.
            float mainExclusionMin = crossings[0].XMin;
            float mainExclusionMax = crossings[1].XMax;
            float[] mainLines = { main.CenterZ - settings.MainRoadWidth / 4f, main.CenterZ + settings.MainRoadWidth / 4f };
            foreach (float lineZ in mainLines)
            {
                for (float dashStart = origin.x; dashStart + LaneDashLength <= origin.x + settings.CellSize; dashStart += LaneDashPeriod)
                {
                    if (dashStart < mainExclusionMax && dashStart + LaneDashLength > mainExclusionMin)
                    {
                        continue;
                    }

                    dashes.Add(new CellRect(dashStart, dashStart + LaneDashLength, lineZ - half, lineZ + half));
                }
            }

            // Secondary road: dashes run along Z; exclusion spans from south crossing to north crossing.
            float secondaryExclusionMin = crossings[2].ZMin;
            float secondaryExclusionMax = crossings[3].ZMax;
            float lineX = secondary.CenterX;
            for (float dashStart = origin.y; dashStart + LaneDashLength <= origin.y + settings.CellSize; dashStart += LaneDashPeriod)
            {
                if (dashStart < secondaryExclusionMax && dashStart + LaneDashLength > secondaryExclusionMin)
                {
                    continue;
                }

                dashes.Add(new CellRect(lineX - half, lineX + half, dashStart, dashStart + LaneDashLength));
            }

            return dashes;
        }

        /// <summary>
        /// Thin drainage strips along both edges of both roads, split so they never cross the
        /// intersection or a pedestrian crossing.
        /// </summary>
        public static List<CellRect> GetDrainageRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            List<CellRect> crossings = GetCrossingRects(cell, settings);
            List<CellRect> strips = new List<CellRect>();

            // Main avenue edges: segments between cell edge, crossings, and the secondary road band.
            float[] mainSegmentsX =
            {
                origin.x, crossings[0].XMin,
                crossings[0].XMax, secondary.XMin,
                secondary.XMax, crossings[1].XMin,
                crossings[1].XMax, origin.x + settings.CellSize
            };
            float[] mainEdgeZ = { main.ZMin, main.ZMax - DrainageStripWidth };
            foreach (float edgeZ in mainEdgeZ)
            {
                for (int i = 0; i < mainSegmentsX.Length; i += 2)
                {
                    strips.Add(new CellRect(mainSegmentsX[i], mainSegmentsX[i + 1], edgeZ, edgeZ + DrainageStripWidth));
                }
            }

            // Secondary road edges: segments between cell edge, crossings, and the main road band.
            float[] secondarySegmentsZ =
            {
                origin.y, crossings[2].ZMin,
                crossings[2].ZMax, main.ZMin,
                main.ZMax, crossings[3].ZMin,
                crossings[3].ZMax, origin.y + settings.CellSize
            };
            float[] secondaryEdgeX = { secondary.XMin, secondary.XMax - DrainageStripWidth };
            foreach (float edgeX in secondaryEdgeX)
            {
                for (int i = 0; i < secondarySegmentsZ.Length; i += 2)
                {
                    strips.Add(new CellRect(edgeX, edgeX + DrainageStripWidth, secondarySegmentsZ[i], secondarySegmentsZ[i + 1]));
                }
            }

            return strips;
        }

        /// <summary>
        /// Curb strips running along the road-facing edge of every sidewalk, sitting on top of
        /// the sidewalk surface.
        /// </summary>
        public static List<CellRect> GetCurbRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float cellMaxX = origin.x + settings.CellSize;
            float cellMaxZ = origin.y + settings.CellSize;
            float mainBandMin = main.ZMin - settings.SidewalkWidth;
            float mainBandMax = main.ZMax + settings.SidewalkWidth;

            return new List<CellRect>
            {
                // Main avenue sidewalk curbs (full cell width).
                new CellRect(origin.x, cellMaxX, main.ZMax, main.ZMax + CurbWidth),
                new CellRect(origin.x, cellMaxX, main.ZMin - CurbWidth, main.ZMin),
                // Secondary road sidewalk curbs (four split segments).
                new CellRect(secondary.XMin - CurbWidth, secondary.XMin, origin.y, mainBandMin),
                new CellRect(secondary.XMin - CurbWidth, secondary.XMin, mainBandMax, cellMaxZ),
                new CellRect(secondary.XMax, secondary.XMax + CurbWidth, origin.y, mainBandMin),
                new CellRect(secondary.XMax, secondary.XMax + CurbWidth, mainBandMax, cellMaxZ)
            };
        }

        /// <summary>
        /// Corner curb pads on the four sidewalk corners around the intersection.
        /// </summary>
        public static List<CellRect> GetCornerCurbRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            float sw = settings.SidewalkWidth;

            return new List<CellRect>
            {
                new CellRect(secondary.XMin - sw, secondary.XMin, main.ZMin - sw, main.ZMin),
                new CellRect(secondary.XMax, secondary.XMax + sw, main.ZMin - sw, main.ZMin),
                new CellRect(secondary.XMin - sw, secondary.XMin, main.ZMax, main.ZMax + sw),
                new CellRect(secondary.XMax, secondary.XMax + sw, main.ZMax, main.ZMax + sw)
            };
        }

        /// <summary>
        /// Ground-level utility strips along the block-facing edge of every sidewalk.
        /// </summary>
        public static List<CellRect> GetUtilityStripRects(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float cellMaxX = origin.x + settings.CellSize;
            float cellMaxZ = origin.y + settings.CellSize;
            float sw = settings.SidewalkWidth;
            float mainBandMin = main.ZMin - sw;
            float mainBandMax = main.ZMax + sw;
            float westOuter = secondary.XMin - sw;
            float eastOuter = secondary.XMax + sw;

            return new List<CellRect>
            {
                // Main avenue strips, split at the secondary road's sidewalk columns so they
                // never cross the secondary road.
                new CellRect(origin.x, westOuter - UtilityStripWidth, mainBandMax, mainBandMax + UtilityStripWidth),
                new CellRect(eastOuter + UtilityStripWidth, cellMaxX, mainBandMax, mainBandMax + UtilityStripWidth),
                new CellRect(origin.x, westOuter - UtilityStripWidth, mainBandMin - UtilityStripWidth, mainBandMin),
                new CellRect(eastOuter + UtilityStripWidth, cellMaxX, mainBandMin - UtilityStripWidth, mainBandMin),
                new CellRect(westOuter - UtilityStripWidth, westOuter, origin.y, mainBandMin - UtilityStripWidth),
                new CellRect(westOuter - UtilityStripWidth, westOuter, mainBandMax + UtilityStripWidth, cellMaxZ),
                new CellRect(eastOuter, eastOuter + UtilityStripWidth, origin.y, mainBandMin - UtilityStripWidth),
                new CellRect(eastOuter, eastOuter + UtilityStripWidth, mainBandMax + UtilityStripWidth, cellMaxZ)
            };
        }

        /// <summary>
        /// Street light base positions on both main avenue sidewalks, spaced along the avenue and
        /// skipping the intersection zone.
        /// </summary>
        public static List<Vector2> GetStreetLightPositions(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            CellRect main = ProceduralCityLayout.GetMainRoadRect(cell, settings);
            CellRect secondary = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);
            Vector2 origin = ProceduralCityLayout.GetCellOrigin(cell, settings);
            float sw = settings.SidewalkWidth;

            float northZ = main.ZMax + sw / 2f;
            float southZ = main.ZMin - sw / 2f;
            float exclusionMin = secondary.XMin - sw;
            float exclusionMax = secondary.XMax + sw;

            List<Vector2> positions = new List<Vector2>();
            for (float x = origin.x + StreetLightEdgeOffset; x < origin.x + settings.CellSize; x += StreetLightSpacing)
            {
                if (x > exclusionMin && x < exclusionMax)
                {
                    continue;
                }

                positions.Add(new Vector2(x, northZ));
                positions.Add(new Vector2(x, southZ));
            }

            return positions;
        }

        /// <summary>
        /// Traffic sign positions, one on each corner curb pad around the intersection.
        /// </summary>
        public static List<Vector2> GetTrafficSignPositions(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            List<Vector2> positions = new List<Vector2>();
            foreach (CellRect corner in GetCornerCurbRects(cell, settings))
            {
                positions.Add(new Vector2(corner.CenterX, corner.CenterZ));
            }

            return positions;
        }
    }
}
