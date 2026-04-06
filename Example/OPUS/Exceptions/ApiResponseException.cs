using System;


namespace Example.OPUS.Exceptions
{
    /// <summary>
    /// Thrown when the API response cannot be deserialized or is malformed (invalid JSON, unexpected structure, etc.).
    /// This is typically a client-side parsing issue rather than a server error.
    /// </summary>
    public class ApiResponseException : Exception
    {
        public string ResponseBody { get; }
        public string RawContent { get; }

        public ApiResponseException(string message)
            : base(message)
        {
        }

        public ApiResponseException(string message, string responseBody)
            : base(message)
        {
            ResponseBody = responseBody;
        }

        public ApiResponseException(string message, string responseBody, Exception innerException)
            : base(message, innerException)
        {
            ResponseBody = responseBody;
            RawContent = responseBody; // can be same or truncated
        }

        public ApiResponseException(string message, string responseBody, string rawContent, Exception innerException)
            : base(message, innerException)
        {
            ResponseBody = responseBody;
            RawContent = rawContent;
        }
    }
}