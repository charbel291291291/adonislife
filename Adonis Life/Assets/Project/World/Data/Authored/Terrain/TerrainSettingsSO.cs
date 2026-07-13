using AdonisLife.World.Terrain;
using UnityEngine;

namespace AdonisLife.World.Authored
{
    /// <summary>
    /// Authored, configurable parameters for the procedural terrain generator prototype.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainSettings", menuName = "AdonisLife/World/TerrainSettings")]
    public class TerrainSettingsSO : ScriptableObject
    {
        [SerializeField] private int _chunksX = 4;
        public int ChunksX => _chunksX;

        [SerializeField] private int _chunksZ = 4;
        public int ChunksZ => _chunksZ;

        [SerializeField] private float _chunkSize = 250f;
        public float ChunkSize => _chunkSize;

        [SerializeField] private int _heightmapResolution = 129;
        public int HeightmapResolution => _heightmapResolution;

        [SerializeField] private float _originX = -1000f;
        public float OriginX => _originX;

        [SerializeField] private float _originZ = 0f;
        public float OriginZ => _originZ;

        [SerializeField] private float _maxHeight = 60f;
        public float MaxHeight => _maxHeight;

        [SerializeField] private float _seaLevel = 6f;
        public float SeaLevel => _seaLevel;

        [SerializeField] private float _coastWidth = 180f;
        public float CoastWidth => _coastWidth;

        [SerializeField] private float _beachBand = 2.5f;
        public float BeachBand => _beachBand;

        [SerializeField] private float _riverWidth = 14f;
        public float RiverWidth => _riverWidth;

        [SerializeField] private float _riverDepth = 7f;
        public float RiverDepth => _riverDepth;

        [SerializeField] private float _lakeRadius = 60f;
        public float LakeRadius => _lakeRadius;

        [SerializeField] private float _lakeDepth = 5f;
        public float LakeDepth => _lakeDepth;

        [SerializeField] private float _cliffHeight = 20f;
        public float CliffHeight => _cliffHeight;

        [SerializeField] private int _seed = 1234;
        public int Seed => _seed;

        public TerrainGenerationSettings ToGenerationSettings()
        {
            return new TerrainGenerationSettings(
                _chunksX, _chunksZ, _chunkSize, _heightmapResolution, _originX, _originZ,
                _maxHeight, _seaLevel, _coastWidth, _beachBand, _riverWidth, _riverDepth,
                _lakeRadius, _lakeDepth, _cliffHeight, _seed);
        }

        public bool IsValid(out string validationError)
        {
            if (_chunksX <= 0 || _chunksZ <= 0)
            {
                validationError = $"Chunk grid dimensions must be positive. Current grid: {_chunksX} x {_chunksZ}.";
                return false;
            }

            if (_chunkSize <= 0f)
            {
                validationError = $"Chunk size must be positive. Current: {_chunkSize}.";
                return false;
            }

            bool isPowerOfTwoPlusOne = _heightmapResolution >= 33 && ((_heightmapResolution - 1) & (_heightmapResolution - 2)) == 0;
            if (!isPowerOfTwoPlusOne)
            {
                validationError = $"Heightmap resolution must be 2^n + 1 (at least 33). Current: {_heightmapResolution}.";
                return false;
            }

            if (_maxHeight <= 0f || _seaLevel <= 0f || _seaLevel >= _maxHeight)
            {
                validationError = $"Sea level ({_seaLevel}) must be between 0 and max height ({_maxHeight}).";
                return false;
            }

            if (_seaLevel + _cliffHeight + 8f >= _maxHeight)
            {
                validationError = "Cliff height leaves no room for rolling hills below max height.";
                return false;
            }

            if (_coastWidth <= 0f || _beachBand <= 0f || _riverWidth <= 0f || _riverDepth <= 0f ||
                _lakeRadius <= 0f || _lakeDepth <= 0f || _cliffHeight <= 0f)
            {
                validationError = "All terrain feature dimensions must be positive.";
                return false;
            }

            validationError = null;
            return true;
        }
    }
}
