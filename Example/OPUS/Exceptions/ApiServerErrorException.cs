using System.Net;


namespace Example.OPUS.Exceptions
{
    public class ApiServerErrorException : ApiRequestException
    {
        public ApiServerErrorException(string message, HttpStatusCode statusCode, string responseBody = null)
            : base(message, statusCode, responseBody) { }
    }
}
