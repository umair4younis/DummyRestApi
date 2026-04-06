
namespace Example.OPUS.Tests
{
    public class FakeOpusConfiguration : OpusConfiguration
    {
        public bool SkipCertificateLoadingForTests { get; set; } = true;   // ← NEW

        public FakeOpusConfiguration()
        {
            TokenUrl = "https://fake-token.url";
            BaseUrl = "https://fake-opus.url";
            RestUrl = "https://fake-opus.url";
            GraphQlUrl = "https://fake-opus.url/graphql";
            ProxyUrl = "http://proxy.fake";
            ClientId = "fake-client-id";
            ClientSecret = "fake-secret";
            ClientCertPath = "fake-cert.pfx";        // can stay, will be ignored
            ClientCertPassword = "fake-password";
        }
    }
}
