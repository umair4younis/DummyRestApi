using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS.Test
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        private readonly System.Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();
        public HttpRequestMessage LastRequest { get; private set; }

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _responses = new Queue<HttpResponseMessage>();
            _responses.Enqueue(response);
        }

        public FakeHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public FakeHttpMessageHandler(System.Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
            _responses = new Queue<HttpResponseMessage>();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            LastRequest = request;
            Requests.Add(request);

            if (_responseFactory != null)
            {
                return Task.FromResult(_responseFactory(request));
            }

            if (_responses.Count > 1)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            if (_responses.Count == 1)
            {
                return Task.FromResult(_responses.Peek());
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
