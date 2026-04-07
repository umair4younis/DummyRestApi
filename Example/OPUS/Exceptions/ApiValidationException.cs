using System.Net;


namespace Puma.MDE.OPUS.Exceptions
{
    public class ApiValidationException : ApiRequestException
    {
        public ApiValidationException(string message, string responseBody = null)
            : base(message, HttpStatusCode.BadRequest, responseBody) { }
    }
}
