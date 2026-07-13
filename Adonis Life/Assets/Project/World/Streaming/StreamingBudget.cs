using System.Diagnostics;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Tracks and manages a time-based frame execution budget to prevent frame-rate hitching.
    /// This implementation uses high-precision timers and is safe for use in both EditMode and PlayMode.
    /// </summary>
    public class StreamingBudget
    {
        private readonly double _budgetDurationSeconds;
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Gets the configured budget duration in seconds.
        /// </summary>
        public double BudgetDurationSeconds => _budgetDurationSeconds;

        /// <summary>
        /// Gets the elapsed time in seconds since the current budget frame was started.
        /// </summary>
        public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

        /// <summary>
        /// Gets a value indicating whether the budget has been exceeded for the current frame.
        /// </summary>
        public bool IsExceeded => ElapsedSeconds >= _budgetDurationSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingBudget"/> class.
        /// </summary>
        /// <param name="budgetDurationSeconds">The execution time budget limit per frame in seconds.</param>
        public StreamingBudget(double budgetDurationSeconds)
        {
            _budgetDurationSeconds = budgetDurationSeconds;
            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Starts or restarts the budget timer for a new frame or operation batch.
        /// </summary>
        public void StartFrame()
        {
            _stopwatch.Restart();
        }

        /// <summary>
        /// Resets the budget timer.
        /// </summary>
        public void Reset()
        {
            _stopwatch.Reset();
        }
    }
}