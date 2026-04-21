using Puma.MDE.OPUS.Exceptions;
using System.Globalization;
using System;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    public partial class OpusCircuitBreaker
    {
        public const string DefaultConfigPrefix = "Opus.CircuitBreaker";

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

        public OpusCircuitBreaker()
            : this(DefaultConfigPrefix)
        {
        }

        public OpusCircuitBreaker(string appSettingsPrefix)
            : this(LoadFromAppSettings(appSettingsPrefix))
        {
        }

        public OpusCircuitBreaker(OpusCircuitBreakerSettings settings)
            : this(
                (settings ?? throw new ArgumentNullException(nameof(settings))).FailureThreshold,
                settings.BreakSeconds,
                settings.MaxRetries,
                settings.BaseRetryDelayMs,
                settings.BackoffFactor,
                settings.JitterMaxFactor)
        {
        }

        public OpusCircuitBreaker(int failureThreshold, int breakSeconds, int maxRetries)
            : this(
                failureThreshold,
                breakSeconds,
                maxRetries,
                LoadFromAppSettings(DefaultConfigPrefix).BaseRetryDelayMs,
                LoadFromAppSettings(DefaultConfigPrefix).BackoffFactor,
                LoadFromAppSettings(DefaultConfigPrefix).JitterMaxFactor)
        {
        }

        public OpusCircuitBreaker(
            int failureThreshold,
            int breakSeconds,
            int maxRetries,
            int baseRetryDelayMs,
            double backoffFactor,
            double jitterMaxFactor)
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

        private static OpusCircuitBreakerSettings LoadFromAppSettings(string appSettingsPrefix)
        {
            string prefix = string.IsNullOrWhiteSpace(appSettingsPrefix)
                ? DefaultConfigPrefix
                : appSettingsPrefix.Trim();

            OpusCircuitBreakerSettings hardcodedFallback = new OpusCircuitBreakerSettings
            {
                FailureThreshold = 5,
                BreakSeconds = 60,
                MaxRetries = 1,
                BaseRetryDelayMs = 1000,
                BackoffFactor = 2.0,
                JitterMaxFactor = 0.5
            };

            OpusCircuitBreakerSettings globalDefaults = new OpusCircuitBreakerSettings
            {
                FailureThreshold = Puma.MDE.AppSettings.GetInt(DefaultConfigPrefix + ".FailureThreshold", hardcodedFallback.FailureThreshold),
                BreakSeconds = Puma.MDE.AppSettings.GetInt(DefaultConfigPrefix + ".BreakSeconds", hardcodedFallback.BreakSeconds),
                MaxRetries = Puma.MDE.AppSettings.GetInt(DefaultConfigPrefix + ".Retries", hardcodedFallback.MaxRetries),
                BaseRetryDelayMs = Puma.MDE.AppSettings.GetInt(DefaultConfigPrefix + ".BaseRetryDelayMs", hardcodedFallback.BaseRetryDelayMs),
                BackoffFactor = Puma.MDE.AppSettings.GetAs(DefaultConfigPrefix + ".BackoffFactor", ParseDoubleInvariant, hardcodedFallback.BackoffFactor),
                JitterMaxFactor = Puma.MDE.AppSettings.GetAs(DefaultConfigPrefix + ".JitterMaxFactor", ParseDoubleInvariant, hardcodedFallback.JitterMaxFactor)
            };

            OpusCircuitBreakerSettings resolved = new OpusCircuitBreakerSettings
            {
                FailureThreshold = Puma.MDE.AppSettings.GetInt(prefix + ".FailureThreshold", globalDefaults.FailureThreshold),
                BreakSeconds = Puma.MDE.AppSettings.GetInt(prefix + ".BreakSeconds", globalDefaults.BreakSeconds),
                MaxRetries = Puma.MDE.AppSettings.GetInt(prefix + ".Retries", globalDefaults.MaxRetries),
                BaseRetryDelayMs = Puma.MDE.AppSettings.GetInt(prefix + ".BaseRetryDelayMs", globalDefaults.BaseRetryDelayMs),
                BackoffFactor = Puma.MDE.AppSettings.GetAs(prefix + ".BackoffFactor", ParseDoubleInvariant, globalDefaults.BackoffFactor),
                JitterMaxFactor = Puma.MDE.AppSettings.GetAs(prefix + ".JitterMaxFactor", ParseDoubleInvariant, globalDefaults.JitterMaxFactor)
            };

            if (resolved.FailureThreshold < 1)
                resolved.FailureThreshold = globalDefaults.FailureThreshold;

            if (resolved.BreakSeconds < 1)
                resolved.BreakSeconds = globalDefaults.BreakSeconds;

            if (resolved.MaxRetries < 0)
                resolved.MaxRetries = globalDefaults.MaxRetries;

            if (resolved.BaseRetryDelayMs < 0)
                resolved.BaseRetryDelayMs = globalDefaults.BaseRetryDelayMs;

            if (resolved.BackoffFactor <= 0)
                resolved.BackoffFactor = globalDefaults.BackoffFactor;

            if (resolved.JitterMaxFactor < 0)
                resolved.JitterMaxFactor = globalDefaults.JitterMaxFactor;

            return resolved;
        }

        private static double ParseDoubleInvariant(string value)
        {
            double parsed;
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                throw new FormatException("Invalid double value: '" + value + "'.");

            return parsed;
        }

        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            CircuitState currentState;
            lock (_lock)
            {
                currentState = _state;
                Engine.Instance.Log.Debug($"[CircuitBreaker] ExecuteAsync starting. CurrentState: {_state}, " +
                    $"ConsecutiveFailures: {_consecutiveFailures}/{_failureThreshold}");

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
                    var openException = new CircuitBreakerOpenException(
                        $"Circuit open until {_circuitOpenedAt + _breakDuration:HH:mm:ss}. " +
                        $"Total open events: {_totalCircuitOpenEvents}");
                    Engine.Instance.Log.Error($"[CircuitBreaker] BLOCKED - Circuit is OPEN. " +
                        $"Will retry after {_circuitOpenedAt + _breakDuration:HH:mm:ss}");
                    throw openException;
                }
            }

            Exception lastException = null;

            for (int attempt = 1; attempt <= _maxRetries + 1; attempt++)
            {
                try
                {
                    Engine.Instance.Log.Debug($"[CircuitBreaker] Attempt {attempt}/{_maxRetries + 1}");
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
                        Engine.Instance.Log.Error($"[CircuitBreaker] Non-transient error on attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
                        throw;
                    }

                    lock (_lock)
                    {
                        _consecutiveFailures++;
                        _totalFailures++;
                        _lastFailureTime = DateTime.UtcNow;

                        Engine.Instance.Log.Warn($"[CircuitBreaker] Transient error on attempt {attempt}: {ex.GetType().Name} - {ex.Message}");

                        if (_state == CircuitState.Closed)
                        {
                            if (_consecutiveFailures >= _failureThreshold)
                            {
                                _state = CircuitState.Open;
                                _circuitOpenedAt = DateTime.UtcNow;
                                _totalCircuitOpenEvents++;
                                _lastCircuitOpenTime = DateTime.UtcNow;
                                _consecutiveFailures = 0;

                                Engine.Instance.Log.Error($"[CircuitBreaker] OPENED | Total failures: {_totalFailures} | " +
                                                  $"Open until {_circuitOpenedAt + _breakDuration:HH:mm:ss}");
                            }
                            else
                            {
                                Engine.Instance.Log.Debug($"[CircuitBreaker] Failures: {_consecutiveFailures}/{_failureThreshold}");
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
                        Engine.Instance.Log.Error($"[CircuitBreaker] Max retries ({_maxRetries}) exceeded. Final error: {lastException.Message}");
                        throw lastException;
                    }

                    double backoff = _baseRetryDelayMs * Math.Pow(_backoffFactor, attempt - 1);
                    double jitter = new Random().NextDouble() * backoff * _jitterMaxFactor;
                    int delayMs = (int)(backoff + jitter);

                    Engine.Instance.Log.Info($"[CircuitBreaker] Retry attempt {attempt}/{_maxRetries}. Waiting {delayMs}ms before next attempt...");

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

    }
}
