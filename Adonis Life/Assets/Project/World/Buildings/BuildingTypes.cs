using AdonisLife.World.UrbanCell;

namespace AdonisLife.World.Buildings
{
    /// <summary>All building categories the city planner can place.</summary>
    public enum BuildingType
    {
        Residential,
        Commercial,
        Industrial,
        Government,
        Hospital,
        School,
        Police,
        FireStation,
        Hotel,
        Restaurant,
        GasStation,
        Parking
    }

    /// <summary>Zoning category assigned to a development block.</summary>
    public enum BlockZone
    {
        Residential,
        Commercial,
        Industrial,
        Civic
    }

    /// <summary>Which facade of a building holds its entrance (world axis directions).</summary>
    public enum EntranceSide
    {
        North,
        South,
        East,
        West
    }

    /// <summary>
    /// A fully planned building: its type, world-space footprint, vertical extents, and the
    /// road-facing entrance marker on its facade.
    /// </summary>
    public readonly struct BuildingSpec
    {
        public readonly BuildingType Type;
        public readonly CellRect Footprint;
        public readonly int Floors;
        public readonly float Height;
        public readonly EntranceSide Entrance;
        public readonly CellRect EntranceMarker;

        public BuildingSpec(
            BuildingType type,
            CellRect footprint,
            int floors,
            float height,
            EntranceSide entrance,
            CellRect entranceMarker)
        {
            Type = type;
            Footprint = footprint;
            Floors = floors;
            Height = height;
            Entrance = entrance;
            EntranceMarker = entranceMarker;
        }
    }

    /// <summary>Per-type planning parameters.</summary>
    public readonly struct BuildingDefinition
    {
        public readonly int FloorsMin;
        public readonly int FloorsMax;
        public readonly float Setback;

        public BuildingDefinition(int floorsMin, int floorsMax, float setback)
        {
            FloorsMin = floorsMin;
            FloorsMax = floorsMax;
            Setback = setback;
        }
    }

    /// <summary>
    /// Static data table of per-type planning parameters. Deterministic and unit-testable;
    /// promoted to an authored asset once building content becomes art-driven.
    /// </summary>
    public static class BuildingCatalog
    {
        public const float FloorHeight = 3f;
        public const float ParkingPadHeight = 0.3f;

        public static BuildingDefinition GetDefinition(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Residential: return new BuildingDefinition(2, 4, 3f);
                case BuildingType.Commercial: return new BuildingDefinition(3, 6, 1.5f);
                case BuildingType.Industrial: return new BuildingDefinition(1, 2, 4f);
                case BuildingType.Government: return new BuildingDefinition(4, 4, 4f);
                case BuildingType.Hospital: return new BuildingDefinition(5, 5, 4f);
                case BuildingType.School: return new BuildingDefinition(2, 2, 4f);
                case BuildingType.Police: return new BuildingDefinition(2, 2, 4f);
                case BuildingType.FireStation: return new BuildingDefinition(2, 2, 4f);
                case BuildingType.Hotel: return new BuildingDefinition(5, 8, 2f);
                case BuildingType.Restaurant: return new BuildingDefinition(1, 2, 2f);
                case BuildingType.GasStation: return new BuildingDefinition(1, 1, 3f);
                case BuildingType.Parking: return new BuildingDefinition(0, 0, 1f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>Building height in meters for a floor count of this type.</summary>
        public static float GetHeight(BuildingType type, int floors)
        {
            return type == BuildingType.Parking ? ParkingPadHeight : floors * FloorHeight;
        }
    }
}
