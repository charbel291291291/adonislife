using System.Threading;
using System.Threading.Tasks;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Minimal deterministic chunk loader used until real per-chunk content exists. Produces a
    /// <see cref="ChunkState"/> synchronously so streaming behavior can be exercised and tested
    /// without asset or scene dependencies.
    /// </summary>
    public class PlaceholderChunkLoader : IChunkLoader
    {
        public Task<ChunkState> LoadChunkAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<ChunkState>(cancellationToken);
            }

            var state = new ChunkState
            {
                chunkId = $"C{coordinate.X}_{coordinate.Y}",
                coordinateX = coordinate.X,
                coordinateY = coordinate.Y,
                isLoaded = true
            };
            return Task.FromResult(state);
        }
    }

    /// <summary>
    /// Minimal chunk unloader counterpart to <see cref="PlaceholderChunkLoader"/>.
    /// </summary>
    public class PlaceholderChunkUnloader : IChunkUnloader
    {
        public Task UnloadChunkAsync(ChunkCoordinate coordinate, ChunkState state, CancellationToken cancellationToken)
        {
            if (state != null)
            {
                state.isLoaded = false;
            }

            return Task.CompletedTask;
        }
    }
}
