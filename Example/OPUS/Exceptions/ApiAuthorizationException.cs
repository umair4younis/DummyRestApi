using System;
using System.Net;


namespace Example.OPUS.Exceptions
{
    /// <summary>
    /// Thrown when the API returns 403 Forbidden or another authorization-related error.
    /// Typically indicates insufficient permissions, invalid token scope, or access denied.
    /// </summary>
    public class ApiAuthorizationException : ApiRequestException
    {
        public ApiAuthorizationException(string message)
            : base(message, HttpStatusCode.Forbidden)
        {
        }

        public ApiAuthorizationException(string message, string responseBody)
            : base(message, HttpStatusCode.Forbidden, responseBody)
        {
        }

        public ApiAuthorizationException(string message, Exception innerException)
            : base(message, HttpStatusCode.Forbidden, innerException)
        {
        }

        public ApiAuthorizationException(string message, string responseBody, Exception innerException)
            : base(message, HttpStatusCode.Forbidden, responseBody, innerException)
        {
        }

        // Optional: add more context if needed (e.g. required scopes)
        public string RequiredScope { get; set; }
    }
}