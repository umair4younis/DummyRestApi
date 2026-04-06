using System;
using System.Net;


namespace Example.OPUS.Exceptions
{
    /// <summary>
    /// Thrown when the API returns a 5xx server error (500, 502, 503, 504, etc.).
    /// Indicates a temporary or permanent issue on the server side.
    /// </summary>
    public class ApiServerException : ApiRequestException
    {
        public ApiServerException(string message)
            : base(message, HttpStatusCode.InternalServerError)
        {
        }

        public ApiServerException(string message, HttpStatusCode statusCode)
            : base(message, statusCode)
        {
            if ((int)statusCode < 500 || (int)statusCode >= 600)
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Must be a 5xx status code");
        }

        public ApiServerException(string message, string responseBody)
            : base(message, HttpStatusCode.InternalServerError, responseBody)
        {
        }

        public ApiServerException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message, statusCode, responseBody)
        {
            if ((int)statusCode < 500 || (int)statusCode >= 600)
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Must be a 5xx status code");
        }

        public ApiServerException(string message, Exception innerException)
            : base(message, HttpStatusCode.InternalServerError, innerException)
        {
        }

        public ApiServerException(string message, HttpStatusCode statusCode, Exception innerException)
            : base(message, statusCode, innerException)
        {
            if ((int)statusCode < 500 || (int)statusCode >= 600)
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Must be a 5xx status code");
        }

        public ApiServerException(string message, string responseBody, Exception innerException)
            : base(message, HttpStatusCode.InternalServerError, responseBody, innerException)
        {
        }

        public ApiServerException(string message, HttpStatusCode statusCode, string responseBody, Exception innerException)
            : base(message, statusCode, responseBody, innerException)
        {
            if ((int)statusCode < 500 || (int)statusCode >= 600)
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Must be a 5xx status code");
        }
    }
}