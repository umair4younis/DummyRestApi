using Puma.MDE.OPUS.Exceptions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Puma.MDE.OPUS
{
    public partial class OpusCircuitBreaker
    {
        private static bool IsTransient(Exception ex)
        {
            if (ex == null) return false;

            if (ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                ex is OperationCanceledException ||
                ex is System.Net.WebException ||
                ex is HttpStatusException)
            {
                return true;
            }

            if (ex is HttpStatusException hse && hse.StatusCode.HasValue)
            {
                int code = (int)hse.StatusCode.Value;
                if (code >= 500 || code == 429 || code == 503 || code == 504)
                {
                    return true;
                }
            }

            string msg = (ex.Message ?? string.Empty).ToLowerInvariant();

            return
                msg.Contains("timeout") ||
                msg.Contains("connection") ||
                msg.Contains("network") ||
                msg.Contains("unreachable") ||
                msg.Contains("name resolution") ||
                msg.Contains("no route") ||
                msg.Contains("503") ||
                msg.Contains("504") ||
                msg.Contains("429") ||
                msg.Contains("request canceled") ||
                (ex.InnerException != null && IsTransient(ex.InnerException));
        }
    }
}
