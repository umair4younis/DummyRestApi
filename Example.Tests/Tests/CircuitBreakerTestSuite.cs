using Example.OPUS;
using Example.OPUS.Exceptions;
using System;
using System.Threading.Tasks;


namespace Example.Tests
{
    class CircuitBreakerTestSuite
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== OpusCircuitBreaker Test Suite with HttpStatusException ===\n");

            var opusCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 3,       // Open after 3 consecutive transient failures
                breakSeconds: 8,           // Short break for quick testing
                maxRetries: 2,             // 2 retries per attempt
                baseRetryDelayMs: 400,
                backoffFactor: 2.0,
                jitterMaxFactor: 0.3
            );

            var service = new UnreliableService();

            // Test 1: Normal success
            await RunTest("1. Normal success (should succeed)", opusCircuitBreaker, async () =>
            {
                return await service.CallAsync(forceFail: false);
            });

            // Test 2: Trigger circuit open (3 consecutive failures)
            Console.WriteLine("\n2. Trigger circuit open (3 consecutive 503 failures)");
            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    string result = await opusCircuitBreaker.ExecuteAsync(async () =>
                    {
                        return await service.CallAsync(forceFail: true);
                    });
                    Console.WriteLine($"  Attempt {i}: Unexpected success: {result}");
                }
                catch (CircuitBreakerOpenException cbex)
                {
                    Console.WriteLine($"  Attempt {i}: Circuit opened → {cbex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Attempt {i}: Failed: {ex.GetType().Name} - {ex.Message}");
                }

                Console.WriteLine($"  → Metrics: {opusCircuitBreaker.GetMetricsSnapshot()}");
                await Task.Delay(600);
            }

            // Test 3: Wait for break duration → half-open → success
            DateTime? next = opusCircuitBreaker.NextRetryAfter;
            string waitMsg = next.HasValue ? $"{(next.Value - DateTime.UtcNow).TotalSeconds:F0}" : "now";
            Console.WriteLine($"\n3. Waiting for break duration ({waitMsg}s)...");
            await Task.Delay(10000); // wait past 8s

            Console.WriteLine("Half-open test: should succeed and close circuit");
            try
            {
                string result = await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    return await service.CallAsync(forceFail: false);
                });
                Console.WriteLine($"  → Success in half-open: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → Half-open failed: {ex.Message}");
            }
            Console.WriteLine($"  → Metrics after half-open success: {opusCircuitBreaker.GetMetricsSnapshot()}");

            // Test 4: Half-open → failure → re-open
            Console.WriteLine("\n4. Force failure in half-open (should re-open circuit)");
            try
            {
                await opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    await service.CallAsync(forceFail: true);
                    return "never reached";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"  → Metrics after half-open failure: {opusCircuitBreaker.GetMetricsSnapshot()}");

            Console.WriteLine("\n=== All tests completed ===");
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
