using System.Collections.Generic;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Interface for high-level world streaming coordination.
    /// Manages observer anchors, calculates chunks to load/unload, and manages the streaming tick.
    /// </summary>
    public interface IWorldStreamer
    {
        /// <summary>
        /// Gets the current streaming radius in world units.
        /// Chunks within this radius of any observer will be scheduled for loading.
        /// </summary>
        float LoadRadius { get; }

        /// <summary>
        /// Gets the current unload radius in world units.
        /// Chunks outside this radius of all observers will be scheduled for unloading.
        /// </summary>
        float UnloadRadius { get; }

        /// <summary>
        /// Registers a dynamic observer position in world space.
        /// </summary>
        /// <param name="id">Unique identifier for the observer.</param>
        /// <param name="position">The current position of the observer.</param>
        void RegisterObserver(string id, WorldCoordinate position);

        /// <summary>
        /// Updates the position of an existing observer.
        /// </summary>
        /// <param name="id">Unique identifier for the observer.</param>
        /// <param name="newPosition">The new position of the observer.</param>
        void UpdateObserver(string id, WorldCoordinate newPosition);

        /// <summary>
        /// Unregisters an observer from the streaming system.
        /// </summary>
        /// <param name="id">Unique identifier of the observer to remove.</param>
        void UnregisterObserver(string id);

        /// <summary>
        /// Checks if a chunk at the specified coordinate is fully loaded and available.
        /// </summary>
        /// <param name="coordinate">The chunk coordinate.</param>
        /// <returns>True if loaded; otherwise, false.</returns>
        bool IsChunkLoaded(ChunkCoordinate coordinate);

        /// <summary>
        /// Attempts to get the loaded state of a chunk.
        /// </summary>
        /// <param name="coordinate">The chunk coordinate.</param>
        /// <param name="chunkState">The retrieved chunk state, if loaded.</param>
        /// <returns>True if the chunk was retrieved; otherwise, false.</returns>
        bool TryGetChunkState(ChunkCoordinate coordinate, out ChunkState chunkState);

        /// <summary>
        /// Ticks the streaming system, evaluating observer positions, processing queues, and dispatching loads/unloads.
        /// </summary>
        /// <param name="deltaTime">The time passed since the last tick.</param>
        void Tick(float deltaTime);
    }
}