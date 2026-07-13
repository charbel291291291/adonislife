using System;
using UnityEngine;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Translates between continuous world space (WorldCoordinate) and discrete grid coordinates (ChunkCoordinate, ParcelCoordinate).
    /// Safe for both multi-threaded planning and main-thread updates.
    /// </summary>
    public class WorldGrid
    {
        private readonly float _chunkSize;
        private readonly int _parcelsPerChunk;
        private readonly float _parcelSize;
        private readonly WorldBounds _bounds;

        /// <summary>
        /// Gets the width/length size of a chunk in world units.
        /// </summary>
        public float ChunkSize => _chunkSize;

        /// <summary>
        /// Gets the number of parcels along one axis of a chunk.
        /// </summary>
        public int ParcelsPerChunk => _parcelsPerChunk;

        /// <summary>
        /// Gets the size of a single parcel in world units.
        /// </summary>
        public float ParcelSize => _parcelSize;

        /// <summary>
        /// Gets the valid physical boundary limits of the world grid.
        /// </summary>
        public WorldBounds Bounds => _bounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldGrid"/> class.
        /// </summary>
        /// <param name="chunkSize">The physical size of a chunk (must be positive).</param>
        /// <param name="parcelsPerChunk">The number of parcels along one axis of a chunk (must be positive).</param>
        /// <param name="bounds">The absolute bounds of this world grid.</param>
        public WorldGrid(float chunkSize, int parcelsPerChunk, WorldBounds bounds)
        {
            if (chunkSize <= 0)
                throw new ArgumentException("Chunk size must be positive.", nameof(chunkSize));
            if (parcelsPerChunk <= 0)
                throw new ArgumentException("Parcels per chunk count must be positive.", nameof(parcelsPerChunk));

            _chunkSize = chunkSize;
            _parcelsPerChunk = parcelsPerChunk;
            _parcelSize = chunkSize / parcelsPerChunk;
            _bounds = bounds;
        }

        /// <summary>
        /// Calculates the chunk coordinate containing the specified world position.
        /// </summary>
        /// <param name="position">The continuous world position.</param>
        /// <returns>The discrete coordinate of the containing chunk.</returns>
        public ChunkCoordinate GetChunkCoordinate(WorldCoordinate position)
        {
            int cx = Mathf.FloorToInt(position.X / _chunkSize);
            int cy = Mathf.FloorToInt(position.Z / _chunkSize);
            return new ChunkCoordinate(cx, cy);
        }

        /// <summary>
        /// Calculates the parcel coordinate containing the specified world position.
        /// </summary>
        /// <param name="position">The continuous world position.</param>
        /// <returns>The discrete local coordinate of the containing parcel and parent chunk.</returns>
        public ParcelCoordinate GetParcelCoordinate(WorldCoordinate position)
        {
            ChunkCoordinate chunk = GetChunkCoordinate(position);
            
            // Map continuous world space to local offsets inside the chunk
            float localXWorld = position.X - (chunk.X * _chunkSize);
            float localZWorld = position.Z - (chunk.Y * _chunkSize);

            // Floor values to map to parcel index slots
            int px = Mathf.FloorToInt(localXWorld / _parcelSize);
            int py = Mathf.FloorToInt(localZWorld / _parcelSize);

            // Handle precision bounds clamping to [0, ParcelsPerChunk - 1]
            px = Mathf.Clamp(px, 0, _parcelsPerChunk - 1);
            py = Mathf.Clamp(py, 0, _parcelsPerChunk - 1);

            return new ParcelCoordinate(chunk, px, py);
        }

        /// <summary>
        /// Gets the center position of a chunk in world coordinates.
        /// </summary>
        /// <param name="coordinate">The chunk coordinate.</param>
        /// <returns>The 3D center position in world space.</returns>
        public WorldCoordinate GetChunkCenterWorld(ChunkCoordinate coordinate)
        {
            float x = (coordinate.X + 0.5f) * _chunkSize;
            float z = (coordinate.Y + 0.5f) * _chunkSize;
            return new WorldCoordinate(x, 0f, z);
        }

        /// <summary>
        /// Gets the center position of a parcel in world coordinates.
        /// </summary>
        /// <param name="coordinate">The parcel coordinate.</param>
        /// <returns>The 3D center position in world space.</returns>
        public WorldCoordinate GetParcelCenterWorld(ParcelCoordinate coordinate)
        {
            float chunkMinX = coordinate.Chunk.X * _chunkSize;
            float chunkMinZ = coordinate.Chunk.Y * _chunkSize;

            float x = chunkMinX + (coordinate.LocalX + 0.5f) * _parcelSize;
            float z = chunkMinZ + (coordinate.LocalY + 0.5f) * _parcelSize;
            return new WorldCoordinate(x, 0f, z);
        }

        /// <summary>
        /// Checks if a chunk at a coordinate lies entirely or partially within the world bounds.
        /// </summary>
        /// <param name="coordinate">The chunk coordinate.</param>
        /// <returns>True if the chunk intersects with the bounds; otherwise, false.</returns>
        public bool IsChunkWithinBounds(ChunkCoordinate coordinate)
        {
            float minX = coordinate.X * _chunkSize;
            float maxX = minX + _chunkSize;
            float minZ = coordinate.Y * _chunkSize;
            float maxZ = minZ + _chunkSize;

            if (maxX < _bounds.Min.x || minX > _bounds.Max.x) return false;
            if (maxZ < _bounds.Min.z || minZ > _bounds.Max.z) return false;

            return true;
        }
    }
}