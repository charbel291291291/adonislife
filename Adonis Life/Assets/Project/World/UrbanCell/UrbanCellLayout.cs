using System;
using UnityEngine;

namespace AdonisLife.World.UrbanCell
{
    /// <summary>
    /// Identifies one of the four development block quadrants around the central intersection.
    /// </summary>
    public enum UrbanCellBlock
    {
        NW,
        NE,
        SW,
        SE
    }

    /// <summary>
    /// An axis-aligned footprint expressed in cell-local X/Z coordinates (0..CellSize).
    /// </summary>
    public readonly struct CellRect
    {
        public readonly float XMin;
        public readonly float XMax;
        public readonly float ZMin;
        public readonly float ZMax;

        public CellRect(float xMin, float xMax, float zMin, float zMax)
        {
            XMin = xMin;
            XMax = xMax;
            ZMin = zMin;
            ZMax = zMax;
        }

        public float Width => XMax - XMin;
        public float Depth => ZMax - ZMin;
        public float CenterX => (XMin + XMax) / 2f;
        public float CenterZ => (ZMin + ZMax) / 2f;

        /// <summary>
        /// Returns true if this rect shares any area with another rect.
        /// </summary>
        public bool Overlaps(CellRect other)
        {
            bool separated = XMax <= other.XMin || other.XMax <= XMin ||
                              ZMax <= other.ZMin || other.ZMax <= ZMin;
            return !separated;
        }
    }

    /// <summary>
    /// Pure geometry calculations for the reference urban base cell prototype (chunk C0_0).
    /// Covers cell-local X/Z 0..250. Contains no scene, editor, or asset dependencies so the
    /// layout math can be unit tested directly.
    /// </summary>
    public static class UrbanCellLayout
    {
        public const float CellSize = 250f;
        public const float CellCenter = CellSize / 2f;

        public const float MainRoadWidth = 20f;
        public const float SecondaryRoadWidth = 14f;
        public const float SidewalkWidth = 4f;

        public static Vector2 GetCellCenter() => new Vector2(CellCenter, CellCenter);

        /// <summary>East-west avenue, full cell length, centered on Z.</summary>
        public static CellRect GetMainRoadRect()
        {
            float half = MainRoadWidth / 2f;
            return new CellRect(0f, CellSize, CellCenter - half, CellCenter + half);
        }

        /// <summary>South-north road, full cell length, centered on X.</summary>
        public static CellRect GetSecondaryRoadRect()
        {
            float half = SecondaryRoadWidth / 2f;
            return new CellRect(CellCenter - half, CellCenter + half, 0f, CellSize);
        }

        /// <summary>Sidewalk bordering the main avenue, spanning the full cell width.</summary>
        public static CellRect GetMainSidewalkRect(bool north)
        {
            CellRect road = GetMainRoadRect();
            return north
                ? new CellRect(0f, CellSize, road.ZMax, road.ZMax + SidewalkWidth)
                : new CellRect(0f, CellSize, road.ZMin - SidewalkWidth, road.ZMin);
        }

        /// <summary>
        /// Sidewalk bordering the secondary road. Split into a south and north segment that stop
        /// at the main avenue's sidewalk band, so the two sidewalk systems never overlap.
        /// </summary>
        public static CellRect GetSecondarySidewalkRect(bool east, bool north)
        {
            CellRect secondaryRoad = GetSecondaryRoadRect();
            float xMin = east ? secondaryRoad.XMax : secondaryRoad.XMin - SidewalkWidth;
            float xMax = east ? secondaryRoad.XMax + SidewalkWidth : secondaryRoad.XMin;

            float mainBandMin = GetMainSidewalkRect(north: false).ZMin;
            float mainBandMax = GetMainSidewalkRect(north: true).ZMax;

            return north
                ? new CellRect(xMin, xMax, mainBandMax, CellSize)
                : new CellRect(xMin, xMax, 0f, mainBandMin);
        }

        /// <summary>
        /// Development block footprint for one quadrant, inset from the roads and their sidewalks.
        /// </summary>
        public static CellRect GetBlockRect(UrbanCellBlock block)
        {
            CellRect secondaryRoad = GetSecondaryRoadRect();
            float mainBandMin = GetMainSidewalkRect(north: false).ZMin;
            float mainBandMax = GetMainSidewalkRect(north: true).ZMax;
            float westEdge = secondaryRoad.XMin - SidewalkWidth;
            float eastEdge = secondaryRoad.XMax + SidewalkWidth;

            switch (block)
            {
                case UrbanCellBlock.SW:
                    return new CellRect(0f, westEdge, 0f, mainBandMin);
                case UrbanCellBlock.SE:
                    return new CellRect(eastEdge, CellSize, 0f, mainBandMin);
                case UrbanCellBlock.NW:
                    return new CellRect(0f, westEdge, mainBandMax, CellSize);
                case UrbanCellBlock.NE:
                    return new CellRect(eastEdge, CellSize, mainBandMax, CellSize);
                default:
                    throw new ArgumentOutOfRangeException(nameof(block), block, null);
            }
        }
    }
}
