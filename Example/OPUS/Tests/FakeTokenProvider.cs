using System.Threading.Tasks;


namespace Puma.MDE.OPUS.Tests
{
    public class FakeTokenProvider : OpusTokenProvider
    {
        public FakeTokenProvider(OpusConfiguration config)
            : base(config)
        {
        }

        public override Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult("fake-jwt-token-for-tests");
        }
    }
}
