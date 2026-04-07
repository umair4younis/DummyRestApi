using System;
using System.Net;


namespace Puma.MDE.OPUS.Exceptions
{
    public class ApiRequestException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseBody { get; }
        
        public ApiRequestException(string message) : base(message) { }

        public ApiRequestException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public ApiRequestException(string message, HttpStatusCode statusCode, string responseBody)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public ApiRequestException(string message, HttpStatusCode statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public ApiRequestException(string message, HttpStatusCode statusCode, string responseBody, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public ApiRequestException(string message, HttpStatusCode? statusCode = null, string responseBody = null, Exception inner = null)
           : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
