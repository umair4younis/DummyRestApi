using Puma.MDE.OPUS.Models;


namespace Puma.MDE.OPUS.Tests
{
    public class FakeRetryPolicy : RetryPolicy
    {
        public FakeRetryPolicy() : base()
        {
            IsRetryable = ex => false; // no retries in tests
        }
    }
}
