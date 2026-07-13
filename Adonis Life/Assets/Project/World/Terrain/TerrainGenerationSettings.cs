namespace AdonisLife.World.Terrain
{
    /// <summary>
    /// Pure, immutable parameters describing the procedural terrain region: a grid of terrain
    /// chunks with a coastline on the west edge, a beach band, a meandering river, a lake, and a
    /// cliff plateau in the north.
    /// </summary>
    public readonly struct TerrainGenerationSettings
    {
        public readonly int ChunksX;
        public readonly int ChunksZ;
        public readonly float ChunkSize;
        public readonly int HeightmapResolution;
        public readonly float OriginX;
        public readonly float OriginZ;
        public readonly float MaxHeight;
        public readonly float SeaLevel;
        public readonly float CoastWidth;
        public readonly float BeachBand;
        public readonly float RiverWidth;
        public readonly float RiverDepth;
        public readonly float LakeRadius;
        public readonly float LakeDepth;
        public readonly float CliffHeight;
        public readonly int Seed;

        public float TotalWidth => ChunksX * ChunkSize;
        public float TotalDepth => ChunksZ * ChunkSize;

        public TerrainGenerationSettings(
            int chunksX,
            int chunksZ,
            float chunkSize,
            int heightmapResolution,
            float originX,
            float originZ,
            float maxHeight,
            float seaLevel,
            float coastWidth,
            float beachBand,
            float riverWidth,
            float riverDepth,
            float lakeRadius,
            float lakeDepth,
            float cliffHeight,
            int seed)
        {
            ChunksX = chunksX;
            ChunksZ = chunksZ;
            ChunkSize = chunkSize;
            HeightmapResolution = heightmapResolution;
            OriginX = originX;
            OriginZ = originZ;
            MaxHeight = maxHeight;
            SeaLevel = seaLevel;
            CoastWidth = coastWidth;
            BeachBand = beachBand;
            RiverWidth = riverWidth;
            RiverDepth = riverDepth;
            LakeRadius = lakeRadius;
            LakeDepth = lakeDepth;
            CliffHeight = cliffHeight;
            Seed = seed;
        }
    }
}
