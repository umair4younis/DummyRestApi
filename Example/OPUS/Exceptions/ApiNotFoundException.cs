using System.Net;


namespace Puma.MDE.OPUS.Exceptions
{
    public class ApiNotFoundException : ApiRequestException
    {
        public ApiNotFoundException(string message, string resourceId, string responseBody = null)
            : base($"{message} (resource: {resourceId})", HttpStatusCode.NotFound, responseBody) { }
    }
}
