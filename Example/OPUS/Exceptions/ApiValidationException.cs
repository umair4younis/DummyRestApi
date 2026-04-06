using System.Net;


namespace Example.OPUS.Exceptions
{
    public class ApiValidationException : ApiRequestException
    {
        public ApiValidationException(string message, string responseBody = null)
            : base(message, HttpStatusCode.BadRequest, responseBody) { }
    }
}
