using System.Net.Http;


namespace Puma.MDE.OPUS.Tests
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
