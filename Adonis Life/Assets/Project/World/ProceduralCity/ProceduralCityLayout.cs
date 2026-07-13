using System.Collections.Generic;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.ProceduralCity
{
    /// <summary>
    /// Identifies one of the four development block quadrants around a cell's central intersection.
    /// </summary>
    public enum DevelopmentBlockQuadrant
    {
        NW,
        NE,
        SW,
        SE
    }

    /// <summary>
    /// Pure geometry calculations tiling the approved 250x250 urban cell prototype into a larger
    /// city grid. Every cell contributes only its own local road/sidewalk/block segments,
    /// translated by its grid origin, so adjacent cells' segments abut without gaps or overlaps.
    /// Contains no scene, editor, or asset dependencies so the layout math can be unit tested directly.
    /// </summary>
    public static class ProceduralCityLayout
    {
        public static IEnumerable<CellCoordinate2D> GetAllCells(CityGenerationSettings settings)
        {
            for (int z = 0; z < settings.CellsZ; z++)
            {
                for (int x = 0; x < settings.CellsX; x++)
                {
                    yield return new CellCoordinate2D(x, z);
                }
            }
        }

        public static Vector2 GetCityDimensions(CityGenerationSettings settings)
        {
            return new Vector2(settings.CellsX * settings.CellSize, settings.CellsZ * settings.CellSize);
        }

        public static Vector2 GetCityCenter(CityGenerationSettings settings)
        {
            return GetCityDimensions(settings) / 2f;
        }

        public static Vector2 GetCellOrigin(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            return new Vector2(cell.X * settings.CellSize, cell.Z * settings.CellSize);
        }

        /// <summary>East-west avenue, spanning this cell's own X range, centered on this cell's Z.</summary>
        public static CellRect GetMainRoadRect(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            float half = settings.MainRoadWidth / 2f;
            float cellCenter = settings.CellSize / 2f;
            Vector2 origin = GetCellOrigin(cell, settings);
            return new CellRect(
                origin.x,
                origin.x + settings.CellSize,
                origin.y + cellCenter - half,
                origin.y + cellCenter + half);
        }

        /// <summary>South-north road, spanning this cell's own Z range, centered on this cell's X.</summary>
        public static CellRect GetSecondaryRoadRect(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            float half = settings.SecondaryRoadWidth / 2f;
            float cellCenter = settings.CellSize / 2f;
            Vector2 origin = GetCellOrigin(cell, settings);
            return new CellRect(
                origin.x + cellCenter - half,
                origin.x + cellCenter + half,
                origin.y,
                origin.y + settings.CellSize);
        }

        public static CellRect GetMainSidewalkRect(CellCoordinate2D cell, CityGenerationSettings settings, bool north)
        {
            CellRect road = GetMainRoadRect(cell, settings);
            Vector2 origin = GetCellOrigin(cell, settings);
            return north
                ? new CellRect(origin.x, origin.x + settings.CellSize, road.ZMax, road.ZMax + settings.SidewalkWidth)
                : new CellRect(origin.x, origin.x + settings.CellSize, road.ZMin - settings.SidewalkWidth, road.ZMin);
        }

        public static CellRect GetSecondarySidewalkRect(CellCoordinate2D cell, CityGenerationSettings settings, bool east, bool north)
        {
            CellRect secondaryRoad = GetSecondaryRoadRect(cell, settings);
            Vector2 origin = GetCellOrigin(cell, settings);

            float xMin = east ? secondaryRoad.XMax : secondaryRoad.XMin - settings.SidewalkWidth;
            float xMax = east ? secondaryRoad.XMax + settings.SidewalkWidth : secondaryRoad.XMin;

            float mainBandMin = GetMainSidewalkRect(cell, settings, north: false).ZMin;
            float mainBandMax = GetMainSidewalkRect(cell, settings, north: true).ZMax;

            return north
                ? new CellRect(xMin, xMax, mainBandMax, origin.y + settings.CellSize)
                : new CellRect(xMin, xMax, origin.y, mainBandMin);
        }

        /// <summary>
        /// Development block footprint for one quadrant of a cell, inset from the roads and their
        /// sidewalks, then shrunk further by the configured block inset (parcel setback).
        /// </summary>
        public static CellRect GetBlockRect(CellCoordinate2D cell, CityGenerationSettings settings, DevelopmentBlockQuadrant quadrant)
        {
            CellRect secondaryRoad = GetSecondaryRoadRect(cell, settings);
            Vector2 origin = GetCellOrigin(cell, settings);
            float mainBandMin = GetMainSidewalkRect(cell, settings, north: false).ZMin;
            float mainBandMax = GetMainSidewalkRect(cell, settings, north: true).ZMax;
            float westEdge = secondaryRoad.XMin - settings.SidewalkWidth;
            float eastEdge = secondaryRoad.XMax + settings.SidewalkWidth;

            CellRect raw;
            switch (quadrant)
            {
                case DevelopmentBlockQuadrant.SW:
                    raw = new CellRect(origin.x, westEdge, origin.y, mainBandMin);
                    break;
                case DevelopmentBlockQuadrant.SE:
                    raw = new CellRect(eastEdge, origin.x + settings.CellSize, origin.y, mainBandMin);
                    break;
                case DevelopmentBlockQuadrant.NW:
                    raw = new CellRect(origin.x, westEdge, mainBandMax, origin.y + settings.CellSize);
                    break;
                case DevelopmentBlockQuadrant.NE:
                    raw = new CellRect(eastEdge, origin.x + settings.CellSize, mainBandMax, origin.y + settings.CellSize);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(quadrant), quadrant, null);
            }

            return Inset(raw, settings.BlockInset);
        }

        private static CellRect Inset(CellRect rect, float inset)
        {
            return new CellRect(rect.XMin + inset, rect.XMax - inset, rect.ZMin + inset, rect.ZMax - inset);
        }
    }
}
