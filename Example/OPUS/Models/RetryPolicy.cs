using System;


namespace Puma.MDE.OPUS.Models
{
    public class RetryPolicy
    {
        public int MaxRetries { get; set; }

        public int BaseDelayMs { get; set; }

        public double BackoffFactor { get; set; }

        public double JitterMaxFactor { get; set; }

        public RetryPolicy()
        {
            MaxRetries = 3;
            BaseDelayMs = 1000;
            BackoffFactor = 2.0;
            JitterMaxFactor = 0.5;
        }

        public Func<Exception, bool> IsRetryable { get; set; }
    }
}
