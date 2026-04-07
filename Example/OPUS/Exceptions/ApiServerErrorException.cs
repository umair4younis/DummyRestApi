using System.Net;


namespace Puma.MDE.OPUS.Exceptions
{
    public class ApiServerErrorException : ApiRequestException
    {
        public ApiServerErrorException(string message, HttpStatusCode statusCode, string responseBody = null)
            : base(message, statusCode, responseBody) { }
    }
}
