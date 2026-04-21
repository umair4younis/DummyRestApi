using System;


namespace Puma.MDE.OPUS
{
    public partial class OpusCircuitBreaker
    {
        public int FailureThreshold => _failureThreshold;
        public int BreakDurationSeconds => (int)_breakDuration.TotalSeconds;
        public int MaxRetries => _maxRetries;
        public int BaseRetryDelayMs => _baseRetryDelayMs;
        public double BackoffFactor => _backoffFactor;
        public double JitterMaxFactor => _jitterMaxFactor;

        public int TotalFailures => _totalFailures;
        public int TotalSuccesses => _totalSuccesses;
        public int ConsecutiveFailures => _consecutiveFailures;
        public int TotalCircuitOpenEvents => _totalCircuitOpenEvents;
        public DateTime LastCircuitOpenTime => _lastCircuitOpenTime;
        public DateTime LastSuccessTime => _lastSuccessTime;
        public DateTime LastFailureTime => _lastFailureTime;

        public string CurrentState
        {
            get
            {
                lock (_lock)
                {
                    return _state.ToString();
                }
            }
        }

        public bool IsCircuitOpen => CurrentState == "Open";
        public bool IsHalfOpen => CurrentState == "HalfOpen";

        public DateTime? NextRetryAfter
        {
            get
            {
                lock (_lock)
                {
                    return _state == CircuitState.Open
                        ? _circuitOpenedAt + _breakDuration
                        : (DateTime?)null;
                }
            }
        }

        public string GetMetricsSnapshot()
        {
            lock (_lock)
            {
                return string.Format(
                    "State: {0} | Successes: {1} | Failures: {2} | Consecutive: {3} | " +
                    "Open events: {4} | Last open: {5} | Last success: {6} | Last failure: {7} | " +
                    "Next retry: {8}",
                    CurrentState,
                    _totalSuccesses,
                    _totalFailures,
                    _consecutiveFailures,
                    _totalCircuitOpenEvents,
                    _lastCircuitOpenTime == DateTime.MinValue ? "N/A" : _lastCircuitOpenTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    _lastSuccessTime == DateTime.MinValue ? "N/A" : _lastSuccessTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    _lastFailureTime == DateTime.MinValue ? "N/A" : _lastFailureTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    NextRetryAfter?.ToString("HH:mm:ss") ?? "Now");
            }
        }
    }
}
