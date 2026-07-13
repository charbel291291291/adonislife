using System;
using System.Threading;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Represents the streaming state of a specific chunk.
    /// </summary>
    public enum ChunkLifecycleState
    {
        /// <summary>
        /// The chunk is not in memory and is not scheduled to load.
        /// </summary>
        Unloaded,

        /// <summary>
        /// The chunk is currently loading asynchronously.
        /// </summary>
        Loading,

        /// <summary>
        /// The chunk is fully loaded and active in memory.
        /// </summary>
        Loaded,

        /// <summary>
        /// The chunk is currently unloading asynchronously.
        /// </summary>
        Unloading
    }

    /// <summary>
    /// Represents a thread-safe handle tracking the state, asynchronous tasks, and cancellation of a single world chunk.
    /// </summary>
    public class ChunkHandle : IDisposable
    {
        private readonly ChunkCoordinate _coordinate;
        private readonly object _lock = new object();

        private ChunkLifecycleState _state;
        private ChunkState _chunkState;
        private CancellationTokenSource _cts;

        /// <summary>
        /// Gets the coordinate of the chunk.
        /// </summary>
        public ChunkCoordinate Coordinate => _coordinate;

        /// <summary>
        /// Gets the current lifecycle state of the chunk.
        /// </summary>
        public ChunkLifecycleState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Gets the current loaded chunk state. May be null if not loaded.
        /// </summary>
        public ChunkState ChunkState
        {
            get
            {
                lock (_lock)
                {
                    return _chunkState;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkHandle"/> class.
        /// </summary>
        /// <param name="coordinate">The chunk coordinate.</param>
        public ChunkHandle(ChunkCoordinate coordinate)
        {
            _coordinate = coordinate;
            _state = ChunkLifecycleState.Unloaded;
        }

        /// <summary>
        /// Transitions the chunk handle state if the transition is valid.
        /// </summary>
        /// <param name="newState">The target state to transition to.</param>
        /// <returns>True if transition was successful; otherwise, false.</returns>
        public bool TryTransition(ChunkLifecycleState newState)
        {
            lock (_lock)
            {
                bool valid = IsTransitionValid(_state, newState);
                if (valid)
                {
                    _state = newState;
                }
                return valid;
            }
        }

        /// <summary>
        /// Prepares the chunk handle for a new asynchronous operation by generating a cancellation token.
        /// </summary>
        /// <returns>The CancellationToken associated with the active operation.</returns>
        public CancellationToken StartOperation()
        {
            lock (_lock)
            {
                CancelActiveOperation();
                _cts = new CancellationTokenSource();
                return _cts.Token;
            }
        }

        /// <summary>
        /// Sets the underlying serialized/runtime chunk state when loaded successfully.
        /// </summary>
        /// <param name="chunkState">The fully loaded chunk state.</param>
        public void SetChunkState(ChunkState chunkState)
        {
            lock (_lock)
            {
                _chunkState = chunkState;
            }
        }

        /// <summary>
        /// Cancels any currently running asynchronous loading or unloading operation.
        /// </summary>
        public void CancelActiveOperation()
        {
            lock (_lock)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }

        /// <summary>
        /// Clears the loaded chunk state data.
        /// </summary>
        public void ClearChunkState()
        {
            lock (_lock)
            {
                _chunkState = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CancelActiveOperation();
        }

        private static bool IsTransitionValid(ChunkLifecycleState current, ChunkLifecycleState target)
        {
            switch (current)
            {
                case ChunkLifecycleState.Unloaded:
                    return target == ChunkLifecycleState.Loading;

                case ChunkLifecycleState.Loading:
                    return target == ChunkLifecycleState.Loaded || target == ChunkLifecycleState.Unloaded;

                case ChunkLifecycleState.Loaded:
                    return target == ChunkLifecycleState.Unloading;

                case ChunkLifecycleState.Unloading:
                    return target == ChunkLifecycleState.Unloaded || target == ChunkLifecycleState.Loaded;

                default:
                    return false;
            }
        }
    }
}