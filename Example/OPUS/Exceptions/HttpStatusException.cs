using System;
using System.Net;


namespace Example.OPUS.Exceptions
{
    /// <summary>
    /// Custom exception that includes HTTP status code information.
    /// Useful for simulating or propagating HTTP-like errors in tests or API clients.
    /// </summary>
    [Serializable]
    public class HttpStatusException : Exception
    {
        /// <summary>
        /// The HTTP status code associated with this error (e.g., 503, 429, 504)
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// Creates a new HTTP status exception
        /// </summary>
        public HttpStatusException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new HTTP status exception with status code
        /// </summary>
        public HttpStatusException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates a new HTTP status exception with inner exception
        /// </summary>
        public HttpStatusException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new HTTP status exception with status code and inner exception
        /// </summary>
        public HttpStatusException(string message, HttpStatusCode statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        // Required for serialization in .NET Framework
        protected HttpStatusException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                var statusCodeString = info.GetString("StatusCode");
                if (!string.IsNullOrEmpty(statusCodeString) && Enum.TryParse<HttpStatusCode>(statusCodeString, out var status))
                {
                    StatusCode = status;
                }
            }
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            if (info != null && StatusCode.HasValue)
            {
                info.AddValue("StatusCode", StatusCode.Value.ToString());
            }
        }

        // Convenience methods for common status codes
        public static HttpStatusException ServiceUnavailable(string message = "Service Unavailable")
            => new HttpStatusException(message, HttpStatusCode.ServiceUnavailable);

        public static HttpStatusException TooManyRequests(string message = "Too Many Requests")
            => new HttpStatusException(message, (HttpStatusCode)429);

        public static HttpStatusException GatewayTimeout(string message = "Gateway Timeout")
            => new HttpStatusException(message, HttpStatusCode.GatewayTimeout);
    }
}
