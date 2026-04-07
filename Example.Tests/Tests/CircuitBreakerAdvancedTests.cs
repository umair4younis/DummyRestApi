using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    class CircuitBreakerAdvancedTests
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Advanced Circuit Breaker Test Suite ===\n");

            var opusCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 3,
                breakSeconds: 10,
                maxRetries: 2,
                baseRetryDelayMs: 400,
                backoffFactor: 2.0,
                jitterMaxFactor: 0.3
            );

            // ── Test 1: Normal success ─────────────────────────────────────────────
            await RunTest("1. Normal success", opusCircuitBreaker, async () =>
            {
                await Task.Delay(150);
                return "OK";
            });

            // ── Test 2: Transient failures → open circuit ──────────────────────────
            Console.WriteLine("\n2. Transient failures (3× HttpStatusException) → should open");
            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    await opusCircuitBreaker.ExecuteAsync(async () =>
                    {
                        throw HttpStatusException.ServiceUnavailable("Test 503");
                    });
                }
                catch (CircuitBreakerOpenException cbex)
                {
                    Console.WriteLine($"  Attempt {i}: {cbex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Attempt {i}: {ex.GetType().Name} - {ex.Message}");
                }
                Console.WriteLine($"  Metrics: {opusCircuitBreaker.GetMetricsSnapshot()}");
                await Task.Delay(600);
            }

            // ── Test 3: Non-transient error → no retry, circuit stays closed ───────
            Console.WriteLine("\n3. Non-transient error (ArgumentException) → no retry, no open");
            try
            {
                await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    throw new ArgumentException("Invalid argument - non-transient");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"  Metrics (should remain closed): {opusCircuitBreaker.GetMetricsSnapshot()}");

            // ── Test 4: Inner exception (transient inside AggregateException) ──────
            Console.WriteLine("\n4. Transient inner exception (should count as transient)");
            try
            {
                await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    throw new AggregateException(
                        new HttpStatusException("Inner 429", (HttpStatusCode)429));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"  Metrics: {opusCircuitBreaker.GetMetricsSnapshot()}");
            
            // ── Test 5: Wait → half-open → success → close ─────────────────────────
            DateTime? next = opusCircuitBreaker.NextRetryAfter;
            string waitMsg = next.HasValue ? $"{(next.Value - DateTime.UtcNow).TotalSeconds:F0}" : "now";
            Console.WriteLine($"\n5. Wait for half-open (break ~{waitMsg}s)");
            await Task.Delay(12000);

            Console.WriteLine("Half-open: success test");
            try
            {
                string result = await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    await Task.Delay(100);
                    return "Test call succeeded";
                });
                Console.WriteLine($"  → {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.Message}");
            }
            Console.WriteLine($"  Metrics after half-open success: {opusCircuitBreaker.GetMetricsSnapshot()}");

            // ── Test 6: Half-open → failure → re-open ──────────────────────────────
            Console.WriteLine("\n6. Half-open: force failure → should re-open");
            try
            {
                await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    throw HttpStatusException.ServiceUnavailable("Test failure in half-open");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.Message}");
            }
            Console.WriteLine($"  Metrics after half-open failure: {opusCircuitBreaker.GetMetricsSnapshot()}");

            Console.WriteLine("\n=== Test Suite Finished ===");
            Console.ReadLine();
        }

        private static async Task RunTest(string title, OpusCircuitBreaker opusCircuitBreaker, Func<Task<string>> action)
        {
            Console.WriteLine($"\n{title}");
            try
            {
                string result = await opusCircuitBreaker.ExecuteAsync(action);
                Console.WriteLine($"  → Success: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"  → Metrics: {opusCircuitBreaker.GetMetricsSnapshot()}");
        }
    }
}
