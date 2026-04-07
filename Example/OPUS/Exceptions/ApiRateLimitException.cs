using System.Net;


namespace Puma.MDE.OPUS.Exceptions
{
    public class ApiRateLimitException : ApiRequestException
    {
        public ApiRateLimitException(string message, string responseBody = null)
            : base(message, (HttpStatusCode)429, responseBody) { }
    }
}
