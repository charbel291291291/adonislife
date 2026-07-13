namespace AdonisLife.World.ProceduralCity
{
    /// <summary>
    /// Pure, immutable parameters describing a procedurally tiled city grid of urban cells.
    /// </summary>
    public readonly struct CityGenerationSettings
    {
        public readonly int CellsX;
        public readonly int CellsZ;
        public readonly float CellSize;
        public readonly float MainRoadWidth;
        public readonly float SecondaryRoadWidth;
        public readonly float SidewalkWidth;
        public readonly float BlockInset;
        public readonly int Seed;

        public CityGenerationSettings(
            int cellsX,
            int cellsZ,
            float cellSize,
            float mainRoadWidth,
            float secondaryRoadWidth,
            float sidewalkWidth,
            float blockInset,
            int seed)
        {
            CellsX = cellsX;
            CellsZ = cellsZ;
            CellSize = cellSize;
            MainRoadWidth = mainRoadWidth;
            SecondaryRoadWidth = secondaryRoadWidth;
            SidewalkWidth = sidewalkWidth;
            BlockInset = blockInset;
            Seed = seed;
        }
    }
}
