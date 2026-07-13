using UnityEngine;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Editor visualization for the streaming system. With no streamer attached it previews the
    /// configured load/unload radii and the chunk grid around this transform (colored by
    /// predicted LOD ring); when a runtime <see cref="WorldStreamer"/> is attached via
    /// <see cref="SetStreamer"/>, it draws the live lifecycle state of every tracked chunk.
    /// </summary>
    public class WorldStreamerDebugView : MonoBehaviour
    {
        [SerializeField] private float _chunkSize = 250f;
        [SerializeField] private float _loadRadius = 500f;
        [SerializeField] private float _unloadRadius = 650f;
        [SerializeField] private float[] _lodDistances = { 300f, 600f };
        [SerializeField] private float _gizmoHeight = 1f;

        private WorldStreamer _streamer;

        private static readonly Color LoadRadiusColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        private static readonly Color UnloadRadiusColor = new Color(0.9f, 0.4f, 0.2f, 0.9f);
        private static readonly Color LoadedColor = new Color(0.2f, 0.7f, 0.9f, 0.5f);
        private static readonly Color LoadingColor = new Color(0.9f, 0.9f, 0.2f, 0.5f);
        private static readonly Color UnloadingColor = new Color(0.9f, 0.3f, 0.3f, 0.5f);

        /// <summary>Attaches a live streamer whose chunk states should be visualized.</summary>
        public void SetStreamer(WorldStreamer streamer)
        {
            _streamer = streamer;
        }

        private void OnDrawGizmos()
        {
            DrawRadius(_loadRadius, LoadRadiusColor);
            DrawRadius(_unloadRadius, UnloadRadiusColor);

            if (_streamer != null)
            {
                DrawLiveChunks();
            }
            else
            {
                DrawPredictedChunks();
            }
        }

        private void DrawRadius(float radius, Color color)
        {
            Gizmos.color = color;
            Vector3 center = new Vector3(transform.position.x, _gizmoHeight, transform.position.z);
            const int segments = 48;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * 2f * Mathf.PI;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private void DrawPredictedChunks()
        {
            if (_chunkSize <= 0f || !ChunkLodCalculator.AreValidLodDistances(_lodDistances))
            {
                return;
            }

            int range = Mathf.CeilToInt(_loadRadius / _chunkSize) + 1;
            int centerX = Mathf.FloorToInt(transform.position.x / _chunkSize);
            int centerZ = Mathf.FloorToInt(transform.position.z / _chunkSize);

            for (int dz = -range; dz <= range; dz++)
            {
                for (int dx = -range; dx <= range; dx++)
                {
                    Vector3 chunkCenter = new Vector3(
                        (centerX + dx + 0.5f) * _chunkSize,
                        _gizmoHeight,
                        (centerZ + dz + 0.5f) * _chunkSize);

                    float distance = Vector2.Distance(
                        new Vector2(chunkCenter.x, chunkCenter.z),
                        new Vector2(transform.position.x, transform.position.z));
                    if (distance > _loadRadius)
                    {
                        continue;
                    }

                    int lod = ChunkLodCalculator.GetLodLevel(distance, _lodDistances);
                    float t = lod / (float)_lodDistances.Length;
                    Gizmos.color = Color.Lerp(LoadedColor, UnloadingColor, t);
                    Gizmos.DrawWireCube(chunkCenter, new Vector3(_chunkSize * 0.95f, 0.1f, _chunkSize * 0.95f));
                }
            }
        }

        private void DrawLiveChunks()
        {
            foreach ((ChunkCoordinate coordinate, ChunkLifecycleState state, float _) in _streamer.GetChunkSnapshot())
            {
                Vector3 center = new Vector3(
                    (coordinate.X + 0.5f) * _chunkSize,
                    _gizmoHeight,
                    (coordinate.Y + 0.5f) * _chunkSize);

                switch (state)
                {
                    case ChunkLifecycleState.Loaded:
                        Gizmos.color = LoadedColor;
                        break;
                    case ChunkLifecycleState.Loading:
                        Gizmos.color = LoadingColor;
                        break;
                    default:
                        Gizmos.color = UnloadingColor;
                        break;
                }

                Gizmos.DrawWireCube(center, new Vector3(_chunkSize * 0.95f, 0.1f, _chunkSize * 0.95f));
            }
        }
    }
}
