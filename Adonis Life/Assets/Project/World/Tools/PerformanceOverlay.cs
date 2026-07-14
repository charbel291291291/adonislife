using UnityEngine;
using UnityEngine.Profiling;

namespace AdonisLife.World.Tools
{
    /// <summary>
    /// Minimal runtime performance overlay: smoothed FPS, frame time, and allocated memory,
    /// drawn in the top-left corner. Toggle with F3.
    /// </summary>
    public class PerformanceOverlay : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

        private const float Smoothing = 0.1f;
        private float _smoothedDeltaTime;

        public float SmoothedFps => _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;

        private void Update()
        {
            _smoothedDeltaTime = Mathf.Lerp(
                _smoothedDeltaTime <= 0f ? Time.unscaledDeltaTime : _smoothedDeltaTime,
                Time.unscaledDeltaTime,
                Smoothing);

            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            long allocatedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            string text = $"FPS: {SmoothedFps:F0}\n" +
                          $"Frame: {_smoothedDeltaTime * 1000f:F1} ms\n" +
                          $"Memory: {allocatedMb} MB";
            GUI.Label(new Rect(10f, 10f, 220f, 70f), text);
        }
    }
}
