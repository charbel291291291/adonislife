using UnityEngine;

namespace AdonisLife.World.Authored
{
    /// <summary>
    /// Single authored source of truth for the world's physical layout.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "AdonisLife/World/WorldConfig")]
    public class WorldConfigSO : ScriptableObject, IWorldEntity
    {
        private const float DivisionTolerance = 0.001f;

        [SerializeField] private string _id = "adonis-main-world";
        public string Id => _id;

        [SerializeField] private Vector2 _worldSize = new Vector2(2000f, 2000f);
        public Vector2 WorldSize => _worldSize;

        [SerializeField] private float _chunkSize = 250f;
        public float ChunkSize => _chunkSize;

        [SerializeField] private DistrictConfigSO[] _districts;
        public DistrictConfigSO[] Districts => _districts;

        /// <summary>
        /// Gets the number of chunks along the world's X axis, rounded to the nearest whole chunk.
        /// </summary>
        public int ChunkCountX => _chunkSize > 0f ? Mathf.RoundToInt(_worldSize.x / _chunkSize) : 0;

        /// <summary>
        /// Gets the number of chunks along the world's Z axis, rounded to the nearest whole chunk.
        /// </summary>
        public int ChunkCountZ => _chunkSize > 0f ? Mathf.RoundToInt(_worldSize.y / _chunkSize) : 0;

        /// <summary>
        /// Validates this configuration against the rules required for a usable authored world.
        /// </summary>
        /// <param name="validationError">A human-readable description of the failure, or null when valid.</param>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool IsValid(out string validationError)
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                validationError = "WorldConfig ID must not be empty.";
                return false;
            }

            if (_worldSize.x <= 0f || _worldSize.y <= 0f)
            {
                validationError = $"World dimensions must be positive. Current size: {_worldSize.x} x {_worldSize.y}.";
                return false;
            }

            if (_chunkSize <= 0f)
            {
                validationError = $"Chunk size must be positive. Current chunk size: {_chunkSize}.";
                return false;
            }

            if (!DividesEvenly(_worldSize.x, _chunkSize) || !DividesEvenly(_worldSize.y, _chunkSize))
            {
                validationError = $"World dimensions ({_worldSize.x} x {_worldSize.y}) must divide evenly by chunk size ({_chunkSize}).";
                return false;
            }

            validationError = null;
            return true;
        }

        private static bool DividesEvenly(float dimension, float chunkSize)
        {
            float chunkCount = dimension / chunkSize;
            float roundedCount = Mathf.Round(chunkCount);
            return Mathf.Abs(chunkCount - roundedCount) <= DivisionTolerance;
        }
    }
}