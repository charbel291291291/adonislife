using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Concrete world streaming coordinator. Tracks observers, computes the desired chunk set
    /// each tick, dispatches asynchronous loads (nearest first) and unloads (with hysteresis:
    /// chunks unload only beyond <see cref="UnloadRadius"/>, which must exceed
    /// <see cref="LoadRadius"/>), and enforces a maximum loaded-chunk budget for memory
    /// management. Pure C# — drivable from a MonoBehaviour, an editor tool, or a unit test.
    /// </summary>
    public class WorldStreamer : IWorldStreamer
    {
        private readonly WorldGrid _grid;
        private readonly IChunkLoader _loader;
        private readonly IChunkUnloader _unloader;
        private readonly float _loadRadius;
        private readonly float _unloadRadius;
        private readonly int _maxConcurrentOperations;
        private readonly int _maxLoadedChunks;

        private readonly Dictionary<string, WorldCoordinate> _observers = new Dictionary<string, WorldCoordinate>();
        private readonly Dictionary<ChunkCoordinate, ChunkHandle> _handles = new Dictionary<ChunkCoordinate, ChunkHandle>();
        private readonly Dictionary<ChunkCoordinate, Task<ChunkState>> _loadTasks = new Dictionary<ChunkCoordinate, Task<ChunkState>>();
        private readonly Dictionary<ChunkCoordinate, Task> _unloadTasks = new Dictionary<ChunkCoordinate, Task>();

        public float LoadRadius => _loadRadius;
        public float UnloadRadius => _unloadRadius;

        /// <summary>Number of chunks currently in the Loaded state.</summary>
        public int LoadedChunkCount
        {
            get
            {
                int count = 0;
                foreach (ChunkHandle handle in _handles.Values)
                {
                    if (handle.State == ChunkLifecycleState.Loaded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public WorldStreamer(
            WorldGrid grid,
            IChunkLoader loader,
            IChunkUnloader unloader,
            float loadRadius,
            float unloadRadius,
            int maxConcurrentOperations,
            int maxLoadedChunks)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _unloader = unloader ?? throw new ArgumentNullException(nameof(unloader));

            if (loadRadius <= 0f)
            {
                throw new ArgumentException("Load radius must be positive.", nameof(loadRadius));
            }

            if (unloadRadius <= loadRadius)
            {
                throw new ArgumentException(
                    $"Unload radius ({unloadRadius}) must exceed load radius ({loadRadius}) for hysteresis.",
                    nameof(unloadRadius));
            }

            if (maxConcurrentOperations <= 0)
            {
                throw new ArgumentException("Max concurrent operations must be positive.", nameof(maxConcurrentOperations));
            }

            if (maxLoadedChunks <= 0)
            {
                throw new ArgumentException("Max loaded chunks must be positive.", nameof(maxLoadedChunks));
            }

            _loadRadius = loadRadius;
            _unloadRadius = unloadRadius;
            _maxConcurrentOperations = maxConcurrentOperations;
            _maxLoadedChunks = maxLoadedChunks;
        }

        public void RegisterObserver(string id, WorldCoordinate position)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Observer id must not be null or empty.", nameof(id));
            }

            _observers[id] = position;
        }

        public void UpdateObserver(string id, WorldCoordinate newPosition)
        {
            if (!_observers.ContainsKey(id))
            {
                throw new ArgumentException($"Observer '{id}' is not registered.", nameof(id));
            }

            _observers[id] = newPosition;
        }

        public void UnregisterObserver(string id)
        {
            _observers.Remove(id);
        }

        public bool IsChunkLoaded(ChunkCoordinate coordinate)
        {
            return _handles.TryGetValue(coordinate, out ChunkHandle handle) &&
                   handle.State == ChunkLifecycleState.Loaded;
        }

        public bool TryGetChunkState(ChunkCoordinate coordinate, out ChunkState chunkState)
        {
            if (IsChunkLoaded(coordinate))
            {
                chunkState = _handles[coordinate].ChunkState;
                return chunkState != null;
            }

            chunkState = null;
            return false;
        }

        public void Tick(float deltaTime)
        {
            ProcessCompletedLoads();
            ProcessCompletedUnloads();
            ScheduleUnloads();
            ScheduleLoads();
        }

        /// <summary>
        /// Snapshot of all tracked chunks with their lifecycle state and distance to the nearest
        /// observer. Intended for validation and editor visualization.
        /// </summary>
        public List<(ChunkCoordinate coordinate, ChunkLifecycleState state, float distance)> GetChunkSnapshot()
        {
            var snapshot = new List<(ChunkCoordinate, ChunkLifecycleState, float)>();
            foreach (KeyValuePair<ChunkCoordinate, ChunkHandle> entry in _handles)
            {
                snapshot.Add((entry.Key, entry.Value.State, DistanceToNearestObserver(entry.Key)));
            }

            return snapshot;
        }

        private void ProcessCompletedLoads()
        {
            List<ChunkCoordinate> completed = null;
            foreach (KeyValuePair<ChunkCoordinate, Task<ChunkState>> entry in _loadTasks)
            {
                if (!entry.Value.IsCompleted)
                {
                    continue;
                }

                completed = completed ?? new List<ChunkCoordinate>();
                completed.Add(entry.Key);
            }

            if (completed == null)
            {
                return;
            }

            foreach (ChunkCoordinate coordinate in completed)
            {
                Task<ChunkState> task = _loadTasks[coordinate];
                _loadTasks.Remove(coordinate);
                ChunkHandle handle = _handles[coordinate];

                if (task.Status == TaskStatus.RanToCompletion && task.Result != null)
                {
                    handle.SetChunkState(task.Result);
                    handle.TryTransition(ChunkLifecycleState.Loaded);
                }
                else
                {
                    handle.TryTransition(ChunkLifecycleState.Unloaded);
                    RemoveHandle(coordinate);
                }
            }
        }

        private void ProcessCompletedUnloads()
        {
            List<ChunkCoordinate> completed = null;
            foreach (KeyValuePair<ChunkCoordinate, Task> entry in _unloadTasks)
            {
                if (!entry.Value.IsCompleted)
                {
                    continue;
                }

                completed = completed ?? new List<ChunkCoordinate>();
                completed.Add(entry.Key);
            }

            if (completed == null)
            {
                return;
            }

            foreach (ChunkCoordinate coordinate in completed)
            {
                _unloadTasks.Remove(coordinate);
                ChunkHandle handle = _handles[coordinate];
                handle.ClearChunkState();
                handle.TryTransition(ChunkLifecycleState.Unloaded);
                RemoveHandle(coordinate);
            }
        }

        private void ScheduleUnloads()
        {
            List<ChunkCoordinate> toUnload = null;
            foreach (KeyValuePair<ChunkCoordinate, ChunkHandle> entry in _handles)
            {
                if (entry.Value.State != ChunkLifecycleState.Loaded)
                {
                    continue;
                }

                if (_observers.Count == 0 || DistanceToNearestObserver(entry.Key) > _unloadRadius)
                {
                    toUnload = toUnload ?? new List<ChunkCoordinate>();
                    toUnload.Add(entry.Key);
                }
            }

            if (toUnload == null)
            {
                return;
            }

            foreach (ChunkCoordinate coordinate in toUnload)
            {
                StartUnload(coordinate);
            }
        }

        private void ScheduleLoads()
        {
            if (_observers.Count == 0)
            {
                return;
            }

            List<(ChunkCoordinate coordinate, float distance)> desired = ComputeDesiredChunks();
            desired.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach ((ChunkCoordinate coordinate, float _) in desired)
            {
                if (ActiveOperationCount() >= _maxConcurrentOperations)
                {
                    break;
                }

                if (LoadedOrLoadingCount() >= _maxLoadedChunks)
                {
                    break;
                }

                if (_handles.ContainsKey(coordinate))
                {
                    continue;
                }

                StartLoad(coordinate);
            }
        }

        private List<(ChunkCoordinate, float)> ComputeDesiredChunks()
        {
            var desired = new List<(ChunkCoordinate, float)>();
            var seen = new HashSet<ChunkCoordinate>();
            int chunkRange = (int)Math.Ceiling(_loadRadius / _grid.ChunkSize) + 1;

            foreach (WorldCoordinate observer in _observers.Values)
            {
                ChunkCoordinate center = _grid.GetChunkCoordinate(observer);
                for (int dy = -chunkRange; dy <= chunkRange; dy++)
                {
                    for (int dx = -chunkRange; dx <= chunkRange; dx++)
                    {
                        var candidate = new ChunkCoordinate(center.X + dx, center.Y + dy);
                        if (!seen.Add(candidate) || !_grid.IsChunkWithinBounds(candidate))
                        {
                            continue;
                        }

                        float distance = DistanceToNearestObserver(candidate);
                        if (distance <= _loadRadius)
                        {
                            desired.Add((candidate, distance));
                        }
                    }
                }
            }

            return desired;
        }

        private void StartLoad(ChunkCoordinate coordinate)
        {
            var handle = new ChunkHandle(coordinate);
            if (!handle.TryTransition(ChunkLifecycleState.Loading))
            {
                return;
            }

            _handles[coordinate] = handle;
            _loadTasks[coordinate] = _loader.LoadChunkAsync(coordinate, handle.StartOperation());
        }

        private void StartUnload(ChunkCoordinate coordinate)
        {
            ChunkHandle handle = _handles[coordinate];
            if (!handle.TryTransition(ChunkLifecycleState.Unloading))
            {
                return;
            }

            _unloadTasks[coordinate] = _unloader.UnloadChunkAsync(coordinate, handle.ChunkState, handle.StartOperation());
        }

        private void RemoveHandle(ChunkCoordinate coordinate)
        {
            if (_handles.TryGetValue(coordinate, out ChunkHandle handle))
            {
                handle.Dispose();
                _handles.Remove(coordinate);
            }
        }

        private int ActiveOperationCount() => _loadTasks.Count + _unloadTasks.Count;

        private int LoadedOrLoadingCount()
        {
            int count = 0;
            foreach (ChunkHandle handle in _handles.Values)
            {
                if (handle.State == ChunkLifecycleState.Loaded || handle.State == ChunkLifecycleState.Loading)
                {
                    count++;
                }
            }

            return count;
        }

        private float DistanceToNearestObserver(ChunkCoordinate coordinate)
        {
            WorldCoordinate center = _grid.GetChunkCenterWorld(coordinate);
            float nearest = float.MaxValue;
            foreach (WorldCoordinate observer in _observers.Values)
            {
                float distance = center.Distance2D(observer);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }
    }
}
