
namespace Example.OPUS.Models
{
    /// <summary>
    /// Response wrapper for delta update (assuming API returns resource)
    /// </summary>
    public class SwapDeltaUpdateResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string UpdatedSwapId { get; set; }
    }
}
