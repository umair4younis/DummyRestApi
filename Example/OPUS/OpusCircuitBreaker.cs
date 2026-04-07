using Puma.MDE.OPUS.Exceptions;
using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    public class OpusCircuitBreaker
    {
        // ── Configuration ──────────────────────────────────────────────────────────
        private readonly int _failureThreshold;
        private readonly TimeSpan _breakDuration;
        private readonly int _maxRetries;
        private readonly int _baseRetryDelayMs;
        private readonly double _backoffFactor;
        private readonly double _jitterMaxFactor;

        // ── State ──────────────────────────────────────────────────────────────────
        public enum CircuitState { Closed, Open, HalfOpen }
        private CircuitState _state = CircuitState.Closed;
        private int _consecutiveFailures;
        private DateTime _circuitOpenedAt = DateTime.MinValue;
        private readonly object _lock = new object();

        // ── Metrics (now properly used and exposed) ────────────────────────────────
        private int _totalFailures;
        private int _totalSuccesses;
        private int _totalCircuitOpenEvents;
        private DateTime _lastCircuitOpenTime = DateTime.MinValue;
        private DateTime _lastSuccessTime = DateTime.MinValue;
        private DateTime _lastFailureTime = DateTime.MinValue;

        public OpusCircuitBreaker(
            int failureThreshold = 5,
            int breakSeconds = 60,
            int maxRetries = 1,
            int baseRetryDelayMs = 1000,
            double backoffFactor = 2.0,
            double jitterMaxFactor = 0.5)
        {
            if (failureThreshold < 1) throw new ArgumentOutOfRangeException(nameof(failureThreshold));
            if (breakSeconds < 1) throw new ArgumentOutOfRangeException(nameof(breakSeconds));
            if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));

            _failureThreshold = failureThreshold;
            _breakDuration = TimeSpan.FromSeconds(breakSeconds);
            _maxRetries = maxRetries;
            _baseRetryDelayMs = baseRetryDelayMs;
            _backoffFactor = backoffFactor;
            _jitterMaxFactor = jitterMaxFactor;
        }

        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            CircuitState currentState;
            lock (_lock)
            {
                currentState = _state;

                // Auto-transition from Open → HalfOpen when time expires
                if (_state == CircuitState.Open &&
                    DateTime.UtcNow >= _circuitOpenedAt + _breakDuration)
                {
                    _state = CircuitState.HalfOpen;
                    currentState = CircuitState.HalfOpen;
                    Engine.Instance.Log.Info("[CircuitBreaker] → HALF-OPEN (test call allowed)");
                }

                // Block if still fully open
                if (_state == CircuitState.Open)
                {
                    throw new CircuitBreakerOpenException(
                        $"Circuit open until {_circuitOpenedAt + _breakDuration:HH:mm:ss}. " +
                        $"Total open events: {_totalCircuitOpenEvents}");
                }
            }

            Exception lastException = null;

            for (int attempt = 1; attempt <= _maxRetries + 1; attempt++)
            {
                try
                {
                    T result = await operation().ConfigureAwait(false);

                    lock (_lock)
                    {
                        // Success always closes the circuit
                        if (_state == CircuitState.HalfOpen)
                        {
                            Engine.Instance.Log.Info("[CircuitBreaker] Test succeeded → CLOSED");
                        }

                        _state = CircuitState.Closed;
                        _consecutiveFailures = 0;
                        _circuitOpenedAt = DateTime.MinValue;

                        _totalSuccesses++;
                        _lastSuccessTime = DateTime.UtcNow;

                        Engine.Instance.Log.Info($"[CircuitBreaker] Success | Total successes: {_totalSuccesses}");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!IsTransient(ex))
                    {
                        throw;
                    }

                    lock (_lock)
                    {
                        _consecutiveFailures++;
                        _totalFailures++;
                        _lastFailureTime = DateTime.UtcNow;

                        if (_state == CircuitState.Closed)
                        {
                            if (_consecutiveFailures >= _failureThreshold)
                            {
                                _state = CircuitState.Open;
                                _circuitOpenedAt = DateTime.UtcNow;
                                _totalCircuitOpenEvents++;
                                _lastCircuitOpenTime = DateTime.UtcNow;
                                _consecutiveFailures = 0;

                                Engine.Instance.Log.Error($"[CircuitBreaker] OPENED | Failures: {_totalFailures} | " +
                                                  $"Open until {_circuitOpenedAt + _breakDuration:HH:mm:ss}");
                            }
                        }
                        else if (_state == CircuitState.HalfOpen)
                        {
                            // Test failed → re-open with full duration
                            _state = CircuitState.Open;
                            _circuitOpenedAt = DateTime.UtcNow;
                            _consecutiveFailures = 0;

                            Engine.Instance.Log.Error($"[CircuitBreaker] Test FAILED in half-open → RE-OPENED " +
                                              $"until {_circuitOpenedAt + _breakDuration:HH:mm:ss}");
                        }
                    }

                    if (attempt > _maxRetries)
                    {
                        throw lastException;
                    }

                    double backoff = _baseRetryDelayMs * Math.Pow(_backoffFactor, attempt - 1);
                    double jitter = new Random().NextDouble() * backoff * _jitterMaxFactor;
                    int delayMs = (int)(backoff + jitter);

                    Engine.Instance.Log.Error($"[Retry] Attempt {attempt}/{_maxRetries} failed. Waiting ~{delayMs} ms...");

                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            }

            throw lastException ?? new InvalidOperationException("Retry loop ended unexpectedly");
        }

        /// <summary>
        /// Executes an async operation that returns no value (fire-and-forget style).
        /// </summary>
        public virtual async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync<object>(async () =>
            {
                await operation().ConfigureAwait(false);
                return null;
            }).ConfigureAwait(false);
        }

        private static bool IsTransient(Exception ex)
        {
            if (ex == null) return false;

            // Direct checks
            if (ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                ex is OperationCanceledException ||
                ex is System.Net.WebException ||
                ex is HttpStatusException)  // ← NEW: treat as transient
            {
                return true;
            }

            // Check status code inside HttpStatusException
            if (ex is HttpStatusException hse && hse.StatusCode.HasValue)
            {
                int code = (int)hse.StatusCode.Value;
                // Common transient HTTP codes
                if (code >= 500 || code == 429 || code == 503 || code == 504)
                {
                    return true;
                }
            }

            // Message-based fallback (case-insensitive)
            string msg = (ex.Message ?? string.Empty).ToLowerInvariant();

            return
                msg.Contains("timeout") ||
                msg.Contains("connection") ||
                msg.Contains("network") ||
                msg.Contains("unreachable") ||
                msg.Contains("name resolution") ||
                msg.Contains("no route") ||
                msg.Contains("503") ||
                msg.Contains("504") ||
                msg.Contains("429") ||
                msg.Contains("request canceled") ||
                (ex.InnerException != null && IsTransient(ex.InnerException));
        }

        // ── Public metrics properties ──────────────────────────────────────────────
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

        // Optional: method to get full metrics snapshot
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
