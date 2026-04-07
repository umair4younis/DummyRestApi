using System.Threading.Tasks;


namespace Puma.MDE.OPUS.Tests
{
    public class FakeOpusGraphQLClient : OpusGraphQLClient
    {
        private object _executeResult;

        // Pass required parameters to base constructor
        public FakeOpusGraphQLClient(
            OpusHttpClientHandler httpClientHandler,     // can pass null in tests
            OpusTokenProvider tokenProvider,             // use fake or real
            OpusConfiguration configuration)
            : base(httpClientHandler, tokenProvider, configuration)
        {
            // No additional logic needed in constructor
        }

        // Allow test to set what ExecuteAsync should return
        public void SetExecuteResult<T>(T result)
        {
            _executeResult = result;
        }

        public override Task<T> ExecuteAsync<T>(string query, object variables = null)
        {
            // Immediately return the pre-set result (no real HTTP call)
            return Task.FromResult((T)_executeResult);
        }
    }
}
