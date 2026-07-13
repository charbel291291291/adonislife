using System.Threading;
using System.Threading.Tasks;
using AdonisLife.World.Runtime;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Defines the contract for asynchronous unloading of world chunks.
    /// </summary>
    public interface IChunkUnloader
    {
        /// <summary>
        /// Asynchronously unloads a chunk at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate of the chunk to unload.</param>
        /// <param name="state">The runtime state of the chunk being unloaded.</param>
        /// <param name="cancellationToken">Token to cancel the async operation.</param>
        /// <returns>A task representing the async operation.</returns>
        Task UnloadChunkAsync(ChunkCoordinate coordinate, ChunkState state, CancellationToken cancellationToken);
    }
}