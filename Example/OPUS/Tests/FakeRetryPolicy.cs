using Example.OPUS.Models;


namespace Example.OPUS.Tests
{
    public class FakeRetryPolicy : RetryPolicy
    {
        public FakeRetryPolicy() : base()
        {
            IsRetryable = ex => false; // no retries in tests
        }
    }
}
