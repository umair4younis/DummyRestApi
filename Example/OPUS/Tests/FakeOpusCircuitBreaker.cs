using System;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS.Tests
{
    public class FakeOpusCircuitBreaker : OpusCircuitBreaker
    {
        public FakeOpusCircuitBreaker() : base(1, 1, 0) { }

        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await operation(); // no breaker logic in tests
        }

        public override async Task ExecuteAsync(Func<Task> operation)
        {
            await operation();
        }
    }
}