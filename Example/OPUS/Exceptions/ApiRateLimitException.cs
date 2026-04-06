using System.Net;


namespace Example.OPUS.Exceptions
{
    public class ApiRateLimitException : ApiRequestException
    {
        public ApiRateLimitException(string message, string responseBody = null)
            : base(message, (HttpStatusCode)429, responseBody) { }
    }
}
