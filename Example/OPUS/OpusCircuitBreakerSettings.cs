
namespace Puma.MDE.OPUS
{
    public sealed class OpusCircuitBreakerSettings
    {
        public int FailureThreshold { get; set; }
        public int BreakSeconds { get; set; }
        public int MaxRetries { get; set; }
        public int BaseRetryDelayMs { get; set; }
        public double BackoffFactor { get; set; }
        public double JitterMaxFactor { get; set; }
    }
}
