namespace Puma.MDE.OPUS.Models
{
    public class OpusOperationResult
    {
        public bool IsSuccess { get; set; }
        public string FriendlyMessage { get; set; }
        public string ErrorMessage { get; set; }

        public static OpusOperationResult Success(string friendlyMessage = null)
        {
            return new OpusOperationResult
            {
                IsSuccess = true,
                FriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage) ? "Operation completed successfully." : friendlyMessage,
                ErrorMessage = null
            };
        }

        public static OpusOperationResult Failure(string friendlyMessage, string errorMessage)
        {
            return new OpusOperationResult
            {
                IsSuccess = false,
                FriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage) ? "Something went wrong while processing OPUS data." : friendlyMessage,
                ErrorMessage = errorMessage ?? "Unknown OPUS error"
            };
        }
    }

    public class OpusOperationResult<T> : OpusOperationResult
    {
        public T Data { get; set; }

        public static OpusOperationResult<T> SuccessWithData(T data, string friendlyMessage = null)
        {
            return new OpusOperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                FriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage) ? "Operation completed successfully." : friendlyMessage,
                ErrorMessage = null
            };
        }

        public static OpusOperationResult<T> FailureWithData(string friendlyMessage, string errorMessage)
        {
            return new OpusOperationResult<T>
            {
                IsSuccess = false,
                Data = default(T),
                FriendlyMessage = string.IsNullOrWhiteSpace(friendlyMessage) ? "Something went wrong while processing OPUS data." : friendlyMessage,
                ErrorMessage = errorMessage ?? "Unknown OPUS error"
            };
        }
    }
}
