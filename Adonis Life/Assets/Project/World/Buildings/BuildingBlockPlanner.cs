using System.Collections.Generic;
using AdonisLife.World.Common;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;

namespace AdonisLife.World.Buildings
{
    /// <summary>
    /// Deterministic building planner for the procedural city. Assigns a zone to every
    /// development block (civic center cell, commercial ring, industrial corners, residential
    /// elsewhere), subdivides non-civic blocks into a 3x3 lot grid, chooses a building type per
    /// lot (with hard guarantees that every <see cref="BuildingType"/> appears in a default
    /// city), applies per-type setbacks, and orients each entrance toward the nearest road that
    /// the block faces. Pure math — no scene, editor, or asset dependencies.
    /// </summary>
    public static class BuildingBlockPlanner
    {
        public const int LotsPerAxis = 3;
        public const float CivicSetback = 4f;
        public const float EntranceWidth = 2f;
        public const float EntranceDepth = 0.4f;

        private const int ParkingLotIndex = 6;
        private const int SchoolLotIndex = 4;
        private const int HotelLotIndex = 4;
        private const int RestaurantLotIndex = 2;
        private const int GasStationLotIndex = 0;
        private const float ResidentialRestaurantChance = 0.15f;
        private const float CommercialHotelChance = 0.2f;

        /// <summary>Zone of a development block, driven by its cell position in the grid.</summary>
        public static BlockZone GetZone(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            int centerX = settings.CellsX / 2;
            int centerZ = settings.CellsZ / 2;

            if (cell.X == centerX && cell.Z == centerZ)
            {
                return BlockZone.Civic;
            }

            if (System.Math.Abs(cell.X - centerX) + System.Math.Abs(cell.Z - centerZ) == 1)
            {
                return BlockZone.Commercial;
            }

            bool isCorner = (cell.X == 0 || cell.X == settings.CellsX - 1) &&
                            (cell.Z == 0 || cell.Z == settings.CellsZ - 1);
            if (isCorner && cell.X == cell.Z)
            {
                return BlockZone.Industrial;
            }

            return BlockZone.Residential;
        }

        /// <summary>One lot rect of the 3x3 subdivision of a development block.</summary>
        public static CellRect GetLotRect(
            CellCoordinate2D cell, DevelopmentBlockQuadrant quadrant, int lotIndex, CityGenerationSettings settings)
        {
            CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
            int row = lotIndex / LotsPerAxis;
            int col = lotIndex % LotsPerAxis;
            float lotWidth = block.Width / LotsPerAxis;
            float lotDepth = block.Depth / LotsPerAxis;

            return new CellRect(
                block.XMin + col * lotWidth,
                block.XMin + (col + 1) * lotWidth,
                block.ZMin + row * lotDepth,
                block.ZMin + (row + 1) * lotDepth);
        }

        /// <summary>All planned buildings for one development block.</summary>
        public static List<BuildingSpec> PlanBlock(
            CellCoordinate2D cell, DevelopmentBlockQuadrant quadrant, CityGenerationSettings settings)
        {
            var specs = new List<BuildingSpec>();
            BlockZone zone = GetZone(cell, settings);

            if (zone == BlockZone.Civic)
            {
                CellRect block = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
                BuildingType civicType = GetCivicType(quadrant);
                specs.Add(CreateSpec(civicType, Inset(block, CivicSetback), cell, quadrant, 0, settings));
                return specs;
            }

            for (int lotIndex = 0; lotIndex < LotsPerAxis * LotsPerAxis; lotIndex++)
            {
                BuildingType type = GetLotType(zone, quadrant, lotIndex, cell, settings);
                CellRect lot = GetLotRect(cell, quadrant, lotIndex, settings);
                CellRect footprint = Inset(lot, BuildingCatalog.GetDefinition(type).Setback);
                specs.Add(CreateSpec(type, footprint, cell, quadrant, lotIndex, settings));
            }

            return specs;
        }

        /// <summary>All planned buildings for the whole city.</summary>
        public static List<BuildingSpec> PlanCity(CityGenerationSettings settings)
        {
            var specs = new List<BuildingSpec>();
            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                foreach (DevelopmentBlockQuadrant quadrant in
                    (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
                {
                    specs.AddRange(PlanBlock(cell, quadrant, settings));
                }
            }

            return specs;
        }

        /// <summary>Fixed civic building per quadrant of the city-center cell.</summary>
        public static BuildingType GetCivicType(DevelopmentBlockQuadrant quadrant)
        {
            switch (quadrant)
            {
                case DevelopmentBlockQuadrant.NW: return BuildingType.Hospital;
                case DevelopmentBlockQuadrant.NE: return BuildingType.Government;
                case DevelopmentBlockQuadrant.SW: return BuildingType.Police;
                case DevelopmentBlockQuadrant.SE: return BuildingType.FireStation;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(quadrant), quadrant, null);
            }
        }

        private static BuildingType GetLotType(
            BlockZone zone, DevelopmentBlockQuadrant quadrant, int lotIndex,
            CellCoordinate2D cell, CityGenerationSettings settings)
        {
            if (lotIndex == ParkingLotIndex)
            {
                return BuildingType.Parking;
            }

            float roll = Hash(cell, quadrant, lotIndex, settings.Seed, salt: 11);
            switch (zone)
            {
                case BlockZone.Residential:
                    if (quadrant == DevelopmentBlockQuadrant.NW && lotIndex == SchoolLotIndex)
                    {
                        return BuildingType.School;
                    }

                    return roll < ResidentialRestaurantChance ? BuildingType.Restaurant : BuildingType.Residential;

                case BlockZone.Commercial:
                    if (lotIndex == RestaurantLotIndex)
                    {
                        return BuildingType.Restaurant;
                    }

                    if (quadrant == DevelopmentBlockQuadrant.NE && lotIndex == HotelLotIndex)
                    {
                        return BuildingType.Hotel;
                    }

                    return roll < CommercialHotelChance ? BuildingType.Hotel : BuildingType.Commercial;

                case BlockZone.Industrial:
                    if (lotIndex == GasStationLotIndex)
                    {
                        return BuildingType.GasStation;
                    }

                    return BuildingType.Industrial;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(zone), zone, null);
            }
        }

        private static BuildingSpec CreateSpec(
            BuildingType type, CellRect footprint,
            CellCoordinate2D cell, DevelopmentBlockQuadrant quadrant, int lotIndex,
            CityGenerationSettings settings)
        {
            BuildingDefinition definition = BuildingCatalog.GetDefinition(type);
            float floorRoll = Hash(cell, quadrant, lotIndex, settings.Seed, salt: 29);
            int floors = definition.FloorsMin +
                         (int)(floorRoll * (definition.FloorsMax - definition.FloorsMin + 1));
            floors = System.Math.Min(floors, definition.FloorsMax);

            EntranceSide entrance = GetEntranceSide(quadrant, lotIndex);
            CellRect marker = GetEntranceMarker(footprint, entrance);

            return new BuildingSpec(type, footprint, floors, BuildingCatalog.GetHeight(type, floors), entrance, marker);
        }

        /// <summary>
        /// Road-facing entrance side. Each quadrant block borders the main avenue on one Z side
        /// and the secondary road on one X side; the entrance faces whichever is nearer to the
        /// lot, preferring the main avenue on ties.
        /// </summary>
        public static EntranceSide GetEntranceSide(DevelopmentBlockQuadrant quadrant, int lotIndex)
        {
            int row = lotIndex / LotsPerAxis;
            int col = lotIndex % LotsPerAxis;

            bool mainIsSouth = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.NE;
            bool secondaryIsEast = quadrant == DevelopmentBlockQuadrant.NW || quadrant == DevelopmentBlockQuadrant.SW;

            int mainDistance = mainIsSouth ? row : LotsPerAxis - 1 - row;
            int secondaryDistance = secondaryIsEast ? LotsPerAxis - 1 - col : col;

            if (mainDistance <= secondaryDistance)
            {
                return mainIsSouth ? EntranceSide.South : EntranceSide.North;
            }

            return secondaryIsEast ? EntranceSide.East : EntranceSide.West;
        }

        private static CellRect GetEntranceMarker(CellRect footprint, EntranceSide side)
        {
            float halfWidth = EntranceWidth / 2f;
            switch (side)
            {
                case EntranceSide.South:
                    return new CellRect(
                        footprint.CenterX - halfWidth, footprint.CenterX + halfWidth,
                        footprint.ZMin - EntranceDepth, footprint.ZMin);
                case EntranceSide.North:
                    return new CellRect(
                        footprint.CenterX - halfWidth, footprint.CenterX + halfWidth,
                        footprint.ZMax, footprint.ZMax + EntranceDepth);
                case EntranceSide.West:
                    return new CellRect(
                        footprint.XMin - EntranceDepth, footprint.XMin,
                        footprint.CenterZ - halfWidth, footprint.CenterZ + halfWidth);
                case EntranceSide.East:
                    return new CellRect(
                        footprint.XMax, footprint.XMax + EntranceDepth,
                        footprint.CenterZ - halfWidth, footprint.CenterZ + halfWidth);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static float Hash(
            CellCoordinate2D cell, DevelopmentBlockQuadrant quadrant, int lotIndex, int seed, int salt)
        {
            return DeterministicHash.Value01(
                cell.X * LotsPerAxis * LotsPerAxis + lotIndex,
                cell.Z * 4 + (int)quadrant,
                salt,
                seed);
        }

        private static CellRect Inset(CellRect rect, float inset)
        {
            return new CellRect(rect.XMin + inset, rect.XMax - inset, rect.ZMin + inset, rect.ZMax - inset);
        }
    }
}
