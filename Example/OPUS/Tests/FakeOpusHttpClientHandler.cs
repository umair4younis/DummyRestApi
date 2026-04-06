using System.Net.Http;


namespace Example.OPUS.Tests
{
    public class FakeOpusHttpClientHandler
    {
        public readonly HttpClientHandler _opusHttpClientHandler;

        public FakeOpusHttpClientHandler()
        {
            _opusHttpClientHandler = new HttpClientHandler
            {
                UseProxy = false,
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
    }
}
