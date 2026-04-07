using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Puma.MDE.OPUS.OpusCircuitBreaker;


namespace Puma.MDE.Tests
{
    [TestClass]
    internal class OpusCircuitBreakerTests
    {
        private OpusCircuitBreaker _breaker;

        [TestInitialize]
        public void Setup()
        {
            _breaker = new OpusCircuitBreaker(
                failureThreshold: 3,       // small for fast testing
                breakSeconds: 2,           // short break
                maxRetries: 2,
                baseRetryDelayMs: 10,      // very short delay
                backoffFactor: 1.5,
                jitterMaxFactor: 0.1);
        }

        [TestMethod]
        public async Task ExecuteAsync_Success_ReturnsValueAndResetsState()
        {
            var result = await _breaker.ExecuteAsync(() => Task.FromResult(42));

            Assert.AreEqual(42, result);
            Assert.AreEqual("Closed", _breaker.CurrentState);
            Assert.AreEqual(0, _breaker.ConsecutiveFailures);
            Assert.AreEqual(1, _breaker.TotalSuccesses);
        }

        [TestMethod]
        public async Task ExecuteAsync_TransientError_RetriesUntilSuccess()
        {
            int attempt = 0;

            Func<Task<string>> operation = async () =>
            {
                attempt++;
                if (attempt <= 2)
                    throw new HttpRequestException("transient");

                return "success on attempt " + attempt;
            };

            var result = await _breaker.ExecuteAsync(operation);

            Assert.AreEqual("success on attempt 3", result);
            Assert.AreEqual(3, attempt);
            Assert.AreEqual("Closed", _breaker.CurrentState);
            Assert.AreEqual(0, _breaker.ConsecutiveFailures);
            Assert.AreEqual(1, _breaker.TotalSuccesses);
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task ExecuteAsync_ExceedsRetries_ThrowsLastException()
        {
            Func<Task<int>> fail = () => throw new HttpRequestException("transient");

            await _breaker.ExecuteAsync(fail);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteAsync_NonTransientError_ThrowsImmediately()
        {
            Func<Task<int>> fail = () => throw new InvalidOperationException("critical");

            await _breaker.ExecuteAsync(fail);
        }

        [TestMethod]
        public async Task ExecuteAsync_ThresholdFailures_OpensCircuit()
        {
            Func<Task<int>> fail = () => throw new HttpRequestException("transient");

            // Trigger 3 failures → should open
            for (int i = 0; i < 3; i++)
            {
                try { await _breaker.ExecuteAsync(fail); }
                catch { }
            }

            // Circuit should now be open
            Assert.AreEqual("Open", _breaker.CurrentState);
            Assert.AreEqual(1, _breaker.TotalCircuitOpenEvents);

            // Next call should throw CircuitBreakerOpenException
            try
            {
                await _breaker.ExecuteAsync(() => Task.FromResult(1));
                Assert.Fail("Expected CircuitBreakerOpenException");
            }
            catch (CircuitBreakerOpenException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Circuit open until"));
            }
        }

        [TestMethod]
        public async Task ExecuteAsync_HalfOpenSuccess_ClosesCircuit()
        {
            // Simulate: force open state (via reflection only for this test)
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            stateField.SetValue(_breaker, CircuitState.Open);
            openedAtField.SetValue(_breaker, DateTime.UtcNow.AddSeconds(-10)); // past break duration

            // Act: test call in half-open window
            await _breaker.ExecuteAsync(() => Task.FromResult(42));

            // Should have transitioned to Closed
            Assert.AreEqual("Closed", _breaker.CurrentState);
            Assert.AreEqual(0, _breaker.ConsecutiveFailures);
        }

        [TestMethod]
        public async Task ExecuteAsync_HalfOpenFailure_ReOpensCircuit()
        {
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stateField.SetValue(_breaker, CircuitState.HalfOpen);

            try
            {
                await _breaker.ExecuteAsync(() => throw new HttpRequestException("transient"));
            }
            catch { }

            Assert.AreEqual("Open", _breaker.CurrentState);
            Assert.AreEqual(0, _breaker.ConsecutiveFailures); // reset on re-open
        }

        [TestMethod]
        public void GetMetricsSnapshot_ReturnsFormattedString()
        {
            var snapshot = _breaker.GetMetricsSnapshot();

            Assert.IsTrue(snapshot.Contains("State: Closed"));
            Assert.IsTrue(snapshot.Contains("Successes: 0"));
            Assert.IsTrue(snapshot.Contains("Failures: 0"));
            Assert.IsTrue(snapshot.Contains("Next retry: Now"));
        }
    }
}
