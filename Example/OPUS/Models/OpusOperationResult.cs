using System.Collections.Generic;


namespace Puma.MDE.OPUS.Models
{
    public class OpusOperationResult
    {
        public bool IsSuccess { get; set; }
        public string FriendlyMessage { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> FriendlyMessages { get; set; }

        public void AddFriendlyContext(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (FriendlyMessages == null)
            {
                FriendlyMessages = new List<string>();
            }

            FriendlyMessages.Add(message);

            if (string.IsNullOrWhiteSpace(FriendlyMessage) && FriendlyMessages.Count > 0)
            {
                FriendlyMessage = FriendlyMessages[0];
            }
        }

        public static OpusOperationResult Success(string friendlyMessage = null)
        {
            string resolvedFriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage)
                ? "Operation completed successfully."
                : friendlyMessage;

            return new OpusOperationResult
            {
                IsSuccess = true,
                FriendlyMessage = resolvedFriendlyMessage,
                ErrorMessage = null,
                FriendlyMessages = new List<string> { resolvedFriendlyMessage }
            };
        }

        public static OpusOperationResult Failure(string friendlyMessage, string errorMessage)
        {
            string resolvedFriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage)
                ? "Something went wrong while processing OPUS data."
                : friendlyMessage;

            return new OpusOperationResult
            {
                IsSuccess = false,
                FriendlyMessage = resolvedFriendlyMessage,
                ErrorMessage = errorMessage ?? "Unknown OPUS error",
                FriendlyMessages = new List<string> { resolvedFriendlyMessage }
            };
        }
    }

    public class OpusOperationResult<T> : OpusOperationResult
    {
        public T Data { get; set; }

        public static OpusOperationResult<T> SuccessWithData(T data, string friendlyMessage = null)
        {
            string resolvedFriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage)
                ? "Operation completed successfully."
                : friendlyMessage;

            return new OpusOperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                FriendlyMessage = resolvedFriendlyMessage,
                ErrorMessage = null,
                FriendlyMessages = new List<string> { resolvedFriendlyMessage }
            };
        }

        public static OpusOperationResult<T> FailureWithData(string friendlyMessage, string errorMessage)
        {
            string resolvedFriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage)
                ? "Something went wrong while processing OPUS data."
                : friendlyMessage;

            return new OpusOperationResult<T>
            {
                IsSuccess = false,
                Data = default(T),
                FriendlyMessage = resolvedFriendlyMessage,
                ErrorMessage = errorMessage ?? "Unknown OPUS error",
                FriendlyMessages = new List<string> { resolvedFriendlyMessage }
            };
        }
    }
}
