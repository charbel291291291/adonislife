using System.Threading;
using System.Threading.Tasks;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Defines the contract for asynchronous loading of world chunks.
    /// </summary>
    public interface IChunkLoader
    {
        /// <summary>
        /// Asynchronously loads a chunk at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate of the chunk to load.</param>
        /// <param name="cancellationToken">Token to cancel the async operation.</param>
        /// <returns>A task representing the async operation, returning the loaded ChunkState.</returns>
        Task<ChunkState> LoadChunkAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken);
    }
}