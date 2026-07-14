using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AdonisLife.World.Tools
{
    /// <summary>
    /// Lightweight section timer for profiling procedural generation. Wrap work in
    /// <see cref="Section"/> (disposable) and read timings back per section name.
    /// </summary>
    public class GenerationProfiler
    {
        private readonly Dictionary<string, double> _milliseconds = new Dictionary<string, double>();
        private readonly List<string> _order = new List<string>();

        public IReadOnlyList<string> SectionNames => _order;

        /// <summary>Starts a timed section; dispose to stop.</summary>
        public IDisposable Section(string name)
        {
            return new SectionScope(this, name);
        }

        /// <summary>Total recorded milliseconds for a section (0 if never recorded).</summary>
        public double GetMilliseconds(string name)
        {
            return _milliseconds.TryGetValue(name, out double value) ? value : 0d;
        }

        /// <summary>Human-readable report of all sections in recording order.</summary>
        public string FormatReport()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Generation profile:");
            foreach (string name in _order)
            {
                builder.AppendLine($"  {name}: {_milliseconds[name]:F2} ms");
            }

            return builder.ToString();
        }

        private void Record(string name, double elapsedMilliseconds)
        {
            if (!_milliseconds.ContainsKey(name))
            {
                _order.Add(name);
                _milliseconds[name] = 0d;
            }

            _milliseconds[name] += elapsedMilliseconds;
        }

        private sealed class SectionScope : IDisposable
        {
            private readonly GenerationProfiler _profiler;
            private readonly string _name;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private bool _disposed;

            public SectionScope(GenerationProfiler profiler, string name)
            {
                _profiler = profiler;
                _name = name;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _stopwatch.Stop();
                _profiler.Record(_name, _stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
