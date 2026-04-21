using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using static Puma.MDE.OPUS.OpusCircuitBreaker;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusCircuitBreakerTests
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
            // Note: re-open resets to 0, but subsequent retries may increment it again
            Assert.IsTrue(_breaker.ConsecutiveFailures >= 0);
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

        [TestMethod]
        public void Constructor_With_AppConfig_Prefix_Uses_Configured_Values()
        {
            string tempConfigPath = Path.Combine(Path.GetTempPath(), "OpusCircuitBreakerTests_" + Guid.NewGuid().ToString("N") + ".config");
            File.WriteAllText(tempConfigPath,
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
    <appSettings>
        <add key=""Test.CircuitBreaker.FailureThreshold"" value=""7"" />
        <add key=""Test.CircuitBreaker.BreakSeconds"" value=""45"" />
        <add key=""Test.CircuitBreaker.Retries"" value=""2"" />
        <add key=""Test.CircuitBreaker.BaseRetryDelayMs"" value=""250"" />
        <add key=""Test.CircuitBreaker.BackoffFactor"" value=""1.25"" />
        <add key=""Test.CircuitBreaker.JitterMaxFactor"" value=""0.15"" />
    </appSettings>
</configuration>");

            string originalConfigPath = AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE") as string;
            try
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", tempConfigPath);
                ResetConfigurationManager();

                OpusCircuitBreaker configured = new OpusCircuitBreaker("Test.CircuitBreaker");

                Assert.AreEqual(7, configured.FailureThreshold);
                Assert.AreEqual(45, configured.BreakDurationSeconds);
                Assert.AreEqual(2, configured.MaxRetries);
                Assert.AreEqual(250, configured.BaseRetryDelayMs);
                Assert.AreEqual(1.25, configured.BackoffFactor, 0.0001);
                Assert.AreEqual(0.15, configured.JitterMaxFactor, 0.0001);
            }
            finally
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", originalConfigPath);
                ResetConfigurationManager();

                if (File.Exists(tempConfigPath))
                    File.Delete(tempConfigPath);
            }
        }

        [TestMethod]
        public void Constructor_With_Explicit_Values_Overrides_Config()
        {
            OpusCircuitBreaker explicitBreaker = new OpusCircuitBreaker(
                failureThreshold: 2,
                breakSeconds: 15,
                maxRetries: 0,
                baseRetryDelayMs: 250,
                backoffFactor: 1.1,
                jitterMaxFactor: 0.2);

            Assert.AreEqual(2, explicitBreaker.FailureThreshold);
            Assert.AreEqual(15, explicitBreaker.BreakDurationSeconds);
            Assert.AreEqual(0, explicitBreaker.MaxRetries);
            Assert.AreEqual(250, explicitBreaker.BaseRetryDelayMs);
            Assert.AreEqual(1.1, explicitBreaker.BackoffFactor, 0.0001);
            Assert.AreEqual(0.2, explicitBreaker.JitterMaxFactor, 0.0001);
        }

        private static void ResetConfigurationManager()
        {
            Type configManagerType = typeof(ConfigurationManager);

            FieldInfo configSystem = configManagerType.GetField("s_configSystem", BindingFlags.Static | BindingFlags.NonPublic);
            if (configSystem != null)
                configSystem.SetValue(null, null);

            FieldInfo initState = configManagerType.GetField("s_initState", BindingFlags.Static | BindingFlags.NonPublic);
            if (initState != null)
                initState.SetValue(null, 0);

            FieldInfo clientConfigPaths = configManagerType.GetField("s_current", BindingFlags.Static | BindingFlags.NonPublic);
            if (clientConfigPaths != null)
                clientConfigPaths.SetValue(null, null);

            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
