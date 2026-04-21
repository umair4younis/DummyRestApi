using Puma.MDE.OPUS.Models;
using System;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS.OrderImport
{
    public static class OpusOrderImportRetryExecutor
    {
        public static T ImportRetryExecute<T>(Func<T> operation, string operationName, RetryPolicy policy)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            return ImportRetryExecuteAsync(
                () => Task.FromResult(operation()),
                operationName,
                policy).GetAwaiter().GetResult();
        }

        public static void ImportRetryExecute(Action operation, string operationName, RetryPolicy policy)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            ImportRetryExecute<object>(
                () =>
                {
                    operation();
                    return null;
                },
                operationName,
                policy);
        }

        private static async Task<T> ImportRetryExecuteAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy)
        {
            RetryPolicy effectivePolicy = policy ?? new RetryPolicy();
            string resolvedOperationName = string.IsNullOrWhiteSpace(operationName) ? "OPUS order import operation" : operationName;

            if (policy == null)
                Engine.Instance.Log.Warn("[ImportRetryExecuteAsync] No retry policy provided. Using default retry policy values.");

            if (effectivePolicy.IsRetryable == null)
                Engine.Instance.Log.Warn("[ImportRetryExecuteAsync] IsRetryable predicate is not configured. Failures will not be retried.");

            Engine.Instance.Log.Info($"[ImportRetryExecuteAsync] Starting '{resolvedOperationName}' with max {effectivePolicy.MaxRetries} retries, " +
                                    $"base delay {effectivePolicy.BaseDelayMs}ms, backoff factor {effectivePolicy.BackoffFactor}");

            Random random = new Random();

            for (int attempt = 1; attempt <= effectivePolicy.MaxRetries; attempt++)
            {
                try
                {
                    Engine.Instance.Log.Debug($"[ImportRetryExecuteAsync] '{resolvedOperationName}' - Attempt {attempt}/{effectivePolicy.MaxRetries}");
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (effectivePolicy.IsRetryable != null && effectivePolicy.IsRetryable(ex) && attempt < effectivePolicy.MaxRetries)
                    {
                        double backoff = effectivePolicy.BaseDelayMs * Math.Pow(effectivePolicy.BackoffFactor, attempt - 1);
                        double jitter = random.NextDouble() * backoff * effectivePolicy.JitterMaxFactor;
                        int delayMs = (int)(backoff + jitter);

                        Engine.Instance.Log.Info(string.Format(
                            "[ImportRetryExecuteAsync] '{0}' - Attempt {1}/{2} failed ({3}): {4}. Retrying in {5}ms...",
                            resolvedOperationName, attempt, effectivePolicy.MaxRetries, ex.GetType().Name, ex.Message.Trim(), delayMs));

                        await TestDelayService.Delay(delayMs).ConfigureAwait(false);
                        continue;
                    }

                    Engine.Instance.Log.Warn(string.Format(
                        "[ImportRetryExecuteAsync] '{0}' - Failed after {1} attempts ({2}): {3}",
                        resolvedOperationName, effectivePolicy.MaxRetries, ex.GetType().Name, ex.Message));

                    throw;
                }
            }

            throw new InvalidOperationException(
                string.Format("[ImportRetryExecuteAsync] '{0}' - Exhausted all {1} retries.", resolvedOperationName, effectivePolicy.MaxRetries));
        }
    }
}