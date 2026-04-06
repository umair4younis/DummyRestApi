using Example.OPUS;
using Example.OPUS.Exceptions;
using System;
using System.Threading.Tasks;


namespace Example.Tests
{
    public class CircuitBreakerTests
    {
        static async Task Main(string[] args)
        {
            var breaker = new OpusCircuitBreaker(
                failureThreshold: 3,        // open after 3 consecutive failures
                breakSeconds: 10,           // short break for testing
                maxRetries: 2,              // 2 retries per attempt
                baseRetryDelayMs: 300,
                backoffFactor: 2.0,
                jitterMaxFactor: 0.4
            );

            var service = new UnreliableService();

            Console.WriteLine("=== Starting Circuit Breaker Tests ===\n");

            // Test 1: Normal success
            await RunTest("Test 1 - Normal success", breaker, async () =>
            {
                return await service.CallAsync(forceFail: false);
            });

            // Test 2: Trigger opening (3 failures in a row)
            Console.WriteLine("\nTest 2 - Trigger circuit open (3 failures)");
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await breaker.ExecuteAsync(async () =>
                    {
                        // Force failure to simulate consecutive errors
                        await service.CallAsync(forceFail: true);
                        return "never reached";
                    });
                }
                catch (CircuitBreakerOpenException cbex)
                {
                    Console.WriteLine($"  → Circuit opened: {cbex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Attempt failed: {ex.Message}");
                }

                Console.WriteLine($"  Metrics: {breaker.GetMetricsSnapshot()}");
                await Task.Delay(500); // spacing
            }

            // Test 3: Wait for break duration → half-open test
            Console.WriteLine($"\nWaiting {breaker.NextRetryAfter - DateTime.UtcNow} for half-open...");
            await Task.Delay(12000); // wait past 10s break

            Console.WriteLine("Test 3 - Half-open test call (should succeed)");
            try
            {
                string result = await breaker.ExecuteAsync(async () =>
                {
                    return await service.CallAsync(forceFail: false);
                });
                Console.WriteLine($"  → Success in half-open: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → Half-open failed: {ex.Message}");
            }
            Console.WriteLine($"  Metrics after half-open: {breaker.GetMetricsSnapshot()}");

            // Test 4: Force failure in half-open → should re-open
            Console.WriteLine("\nTest 4 - Force failure in half-open (should re-open)");
            try
            {
                await breaker.ExecuteAsync(async () =>
                {
                    await service.CallAsync(forceFail: true);
                    return "never";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → {ex.Message}");
            }
            Console.WriteLine($"  Metrics: {breaker.GetMetricsSnapshot()}");

            Console.WriteLine("\n=== All tests completed ===");
            Console.ReadLine();
        }

        private static async Task RunTest(string name, OpusCircuitBreaker breaker, Func<Task<string>> action)
        {
            Console.WriteLine($"\n{name}");
            try
            {
                string result = await breaker.ExecuteAsync(action);
                Console.WriteLine($"  → Success: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  → Failed: {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"  Metrics: {breaker.GetMetricsSnapshot()}");
        }
    }
}
