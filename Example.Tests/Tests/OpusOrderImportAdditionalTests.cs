using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.Data;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.OrderImport;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusOrderImportAdditionalTests
    {
        [TestMethod]
        public void RetryExecutor_Retries_And_Succeeds()
        {
            int attempts = 0;
            RetryPolicy policy = new RetryPolicy
            {
                MaxRetries = 3,
                BaseDelayMs = 0,
                BackoffFactor = 1,
                JitterMaxFactor = 0,
                IsRetryable = ex => ex is InvalidOperationException
            };

            int result = OpusOrderImportRetryExecutor.ImportRetryExecute(
                () =>
                {
                    attempts++;
                    if (attempts < 3)
                        throw new InvalidOperationException("temporary");

                    return 42;
                },
                "retry-test",
                policy);

            Assert.AreEqual(42, result);
            Assert.AreEqual(3, attempts);
        }

        [TestMethod]
        public void RetryExecutor_Throws_For_Null_Delegate()
        {
            AssertCompat.Throws<ArgumentNullException>(() =>
                OpusOrderImportRetryExecutor.ImportRetryExecute<string>(null, "retry-test", new RetryPolicy()));

            AssertCompat.Throws<ArgumentNullException>(() =>
                OpusOrderImportRetryExecutor.ImportRetryExecute((Action)null, "retry-test", new RetryPolicy()));
        }

        [TestMethod]
        public void Configuration_Validate_Requires_File_Source()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                Password = "secret"
            };

            AssertCompat.Throws<ConfigurationErrorsException>(() => configuration.Validate());
        }

        [TestMethod]
        public void Configuration_Validate_Requires_Authentication()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                RemoteDirectory = "/orders"
            };

            AssertCompat.Throws<ConfigurationErrorsException>(() => configuration.Validate());
        }

        [TestMethod]
        public void Parser_Throws_For_Unsupported_File_Extension()
        {
            OpusSwapOrderFileParser parser = new OpusSwapOrderFileParser();
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration();
            SwapAccount selectedAccount = new SwapAccount { AccountName = "ACC-1", Currency = "EUR" };

            AssertCompat.Throws<NotSupportedException>(() =>
                parser.Parse(Encoding.UTF8.GetBytes("x"), "SwapRawOrder.txt", selectedAccount, configuration));
        }

        [TestMethod]
        public void Parser_Throws_When_No_Trade_Rows_Exist()
        {
            OpusSwapOrderFileParser parser = new OpusSwapOrderFileParser();
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration();
            SwapAccount selectedAccount = new SwapAccount { AccountName = "ACC-1", Currency = "EUR" };

            string csv = string.Join(Environment.NewLine, new[]
            {
                "AccountName,Description",
                "ACC-1,No trades"
            });

            AssertCompat.Throws<InvalidOperationException>(() =>
                parser.Parse(Encoding.UTF8.GetBytes(csv), "SwapRawOrder.csv", selectedAccount, configuration));
        }

        [TestMethod]
        public void Service_GetFullUnwindOrder_Throws_For_Null_SelectedAccount()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration();
            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) => new SwapRawOrder(),
                new NoopImportGateway());

            SwapRawOrder fullUnwind;
            AssertCompat.Throws<ArgumentNullException>(() => service.GetFullUnwindOrder(null, out fullUnwind));
        }

        [TestMethod]
        public void Service_GetFullUnwindOrder_Applies_Defaults_When_Parser_Returns_Minimal_Order()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                DefaultOrderType = TypeManastOrder.Transaction,
                DefaultUpdatePoolUpon = TypeManastUpdatePoolUpon.eDoNotUpdate
            };

            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) => new SwapRawOrder { Type = TypeManastOrder.None },
                new NoopImportGateway());

            SwapRawOrder fullUnwind;
            service.GetFullUnwindOrder(new SwapAccount { AccountName = "ACC-DEF", Currency = "EUR" }, out fullUnwind);

            Assert.AreEqual("ACC-DEF", fullUnwind.AccountName);
            Assert.AreEqual(TypeManastOrder.Transaction, fullUnwind.Type);
            Assert.AreEqual(TypeManastUpdatePoolUpon.eDoNotUpdate, fullUnwind.UpdatePoolUpon);
        }

        [TestMethod]
        public void SftpCompatibilityValidation_Requires_Private_Key()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                RemoteDirectory = "/orders"
            };

            ConfigurationErrorsException ex = AssertCompat.Throws<ConfigurationErrorsException>(() =>
                OpusSftpOrderFileDownloader.ValidateStartupCompatibility(configuration));

            StringAssert.Contains(ex.Message, "Either Opus.Sftp.Password or Opus.Sftp.PrivateKeyPath");
        }

        [TestMethod]
        public void SftpCompatibilityValidation_Allows_Password_Only_Configuration()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                Password = "secret",
                RemoteDirectory = "/orders"
            };

            OpusSftpOrderFileDownloader.ValidateStartupCompatibility(configuration);
        }

        [TestMethod]
        public void Configuration_BuildAuthModesLabel_Includes_Otp_When_Configured()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Password = "secret",
                Otp = "123456"
            };

            MethodInfo method = typeof(OpusSftpOrderImportConfiguration)
                .GetMethod("BuildAuthModesLabel", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            string label = (string)method.Invoke(null, new object[] { configuration });
            Assert.AreEqual("password+otp", label);
        }

        [TestMethod]
        public void SftpPromptResolver_Uses_Otp_For_OtpLike_Prompts()
        {
            string response = InvokePromptResolver("Enter OTP/PAS token:", "main-password", "654321");
            Assert.AreEqual("654321", response);
        }

        [TestMethod]
        public void SftpPromptResolver_Uses_Password_For_Password_Prompts()
        {
            string response = InvokePromptResolver("Password:", "main-password", "654321");
            Assert.AreEqual("main-password", response);
        }

        [TestMethod]
        public void SftpPromptResolver_FallsBack_To_Password_When_Otp_Not_Set()
        {
            string response = InvokePromptResolver("Passcode:", "main-password", null);
            Assert.AreEqual("main-password", response);
        }

        [TestMethod]
        public void SftpOtpWarning_Is_Returned_When_Otp_Set_And_KeyboardInteractive_Not_Expected()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Otp = "123456",
                ExpectKeyboardInteractiveAuth = false
            };

            MethodInfo warningMethod = typeof(OpusSftpOrderFileDownloader)
                .GetMethod("GetOtpUsageWarningIfAny", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(warningMethod);

            string warning = (string)warningMethod.Invoke(null, new object[] { configuration });
            StringAssert.Contains(warning, "Opus.Sftp.Otp is configured");
            StringAssert.Contains(warning, "ExpectKeyboardInteractiveAuth");
        }

        [TestMethod]
        public void SftpOtpWarning_Is_Not_Returned_When_KeyboardInteractive_Is_Expected()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Otp = "123456",
                ExpectKeyboardInteractiveAuth = true
            };

            MethodInfo warningMethod = typeof(OpusSftpOrderFileDownloader)
                .GetMethod("GetOtpUsageWarningIfAny", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(warningMethod);

            string warning = (string)warningMethod.Invoke(null, new object[] { configuration });
            Assert.IsTrue(string.IsNullOrWhiteSpace(warning));
        }

        [TestMethod]
        public void SftpCompatibilityValidation_Allows_PrivateKey_Only_Configuration()
        {
            string privateKeyPath = CreateTemporaryFile("dummy-key");
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                PrivateKeyPath = privateKeyPath,
                RemoteDirectory = "/orders"
            };

            try
            {
                OpusSftpOrderFileDownloader.ValidateStartupCompatibility(configuration);
            }
            finally
            {
                if (File.Exists(privateKeyPath))
                    File.Delete(privateKeyPath);
            }
        }

        [TestMethod]
        public void SftpCompatibilityValidation_Rejects_Passphrase_Without_PrivateKey()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                Password = "secret",
                PrivateKeyPassphrase = "topsecret",
                RemoteDirectory = "/orders"
            };

            ConfigurationErrorsException ex = AssertCompat.Throws<ConfigurationErrorsException>(() =>
                OpusSftpOrderFileDownloader.ValidateStartupCompatibility(configuration));

            StringAssert.Contains(ex.Message, "PrivateKeyPassphrase");
        }

        [TestMethod]
        public void Service_Constructor_Fails_Fast_When_Default_Downloader_Is_Not_Compatible()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                RemoteDirectory = "/orders"
            };

            ConfigurationErrorsException ex = AssertCompat.Throws<ConfigurationErrorsException>(() =>
                new OpusSwapOrderImportService(configuration));

            StringAssert.Contains(ex.Message, "Either Opus.Sftp.Password or Opus.Sftp.PrivateKeyPath");
        }

        [TestMethod]
        public void Service_GetFullUnwindOrder_Retries_Parser_On_Transient_Error()
        {
            int parserAttempts = 0;
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                DefaultOrderType = TypeManastOrder.Transaction,
                DefaultUpdatePoolUpon = TypeManastUpdatePoolUpon.eDoNotUpdate,
                RetryPolicy = new RetryPolicy
                {
                    MaxRetries = 3,
                    BaseDelayMs = 0,
                    BackoffFactor = 1,
                    JitterMaxFactor = 0,
                    IsRetryable = ex => ex is IOException
                }
            };

            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) =>
                {
                    parserAttempts++;
                    if (parserAttempts == 1)
                        throw new IOException("temporary parser read issue");

                    return new SwapRawOrder
                    {
                        AccountName = selectedAccount.AccountName,
                        Type = TypeManastOrder.Transaction
                    };
                },
                new NoopImportGateway());

            SwapRawOrder fullUnwind;
            service.GetFullUnwindOrder(new SwapAccount { AccountName = "ACC-RET", Currency = "EUR" }, out fullUnwind);

            Assert.AreEqual(2, parserAttempts);
            Assert.AreEqual("ACC-RET", fullUnwind.AccountName);
        }

        [TestMethod]
        public void Service_ImportFullUnwindOrder_Uses_CircuitBreaker_For_Download_Parse_And_Import()
        {
            RecordingCircuitBreaker breaker = new RecordingCircuitBreaker();
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                RetryPolicy = new RetryPolicy
                {
                    MaxRetries = 1,
                    BaseDelayMs = 0,
                    BackoffFactor = 1,
                    JitterMaxFactor = 0,
                    IsRetryable = ex => ex is IOException
                }
            };

            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) => new SwapRawOrder
                {
                    AccountName = selectedAccount.AccountName,
                    Type = TypeManastOrder.Transaction
                },
                new NoopImportGateway(),
                breaker);

            SwapRawOrder fullUnwind;
            SwapOrder importedOrder;
            List<OrderManager.Error> errors;
            service.ImportFullUnwindOrder(new SwapAccount { AccountName = "ACC-CB", Currency = "EUR" }, out fullUnwind, out importedOrder, out errors);

            Assert.AreEqual(3, breaker.ExecuteCount);
            Assert.IsNotNull(importedOrder);
            Assert.IsNotNull(errors);
        }

        [TestMethod]
        public void Service_GetFullUnwindOrder_Fails_When_CircuitBreaker_Is_Open()
        {
            RecordingCircuitBreaker breaker = new RecordingCircuitBreaker { ThrowOpenException = true };
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                RetryPolicy = new RetryPolicy { MaxRetries = 1, BaseDelayMs = 0, BackoffFactor = 1, JitterMaxFactor = 0 }
            };

            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) => new SwapRawOrder(),
                new NoopImportGateway(),
                breaker);

            SwapRawOrder fullUnwind;
            InvalidOperationException ex = AssertCompat.Throws<InvalidOperationException>(() =>
                service.GetFullUnwindOrder(new SwapAccount { AccountName = "ACC-BLOCK", Currency = "EUR" }, out fullUnwind));

            StringAssert.Contains(ex.Message, "temporarily unavailable");
        }

        [TestMethod]
        public void Service_Uses_ConfigDriven_CircuitBreaker_Settings_When_No_Breaker_Is_Injected()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = "localhost",
                Port = 22,
                Username = "opus",
                Password = "secret",
                RemoteDirectory = "/orders",
                CircuitBreakerFailureThreshold = 7,
                CircuitBreakerBreakSeconds = 45,
                CircuitBreakerRetries = 2
            };

            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => new DownloadedOrderFile { FileName = "order.csv", RemotePath = "/orders/order.csv", Content = Encoding.UTF8.GetBytes("x") },
                (content, fileName, selectedAccount, cfg) => new SwapRawOrder(),
                new NoopImportGateway());

            FieldInfo breakerField = typeof(OpusSwapOrderImportService)
                .GetField("_orderImportCircuitBreaker", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(breakerField);
            object breaker = breakerField.GetValue(service);
            Assert.IsNotNull(breaker);

            Type breakerType = breaker.GetType();
            FieldInfo thresholdField = breakerType.GetField("_failureThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo breakDurationField = breakerType.GetField("_breakDuration", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo retriesField = breakerType.GetField("_maxRetries", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(thresholdField);
            Assert.IsNotNull(breakDurationField);
            Assert.IsNotNull(retriesField);

            Assert.AreEqual(7, (int)thresholdField.GetValue(breaker));
            Assert.AreEqual(TimeSpan.FromSeconds(45), (TimeSpan)breakDurationField.GetValue(breaker));
            Assert.AreEqual(2, (int)retriesField.GetValue(breaker));
        }

        private static string CreateTemporaryFile(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(path, content ?? string.Empty);
            return path;
        }

        private static string InvokePromptResolver(string prompt, string password, string otp)
        {
            MethodInfo resolver = typeof(OpusSftpOrderFileDownloader)
                .GetMethod("ResolveInteractivePromptResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(resolver);

            return (string)resolver.Invoke(null, new object[] { prompt, password, otp });
        }

        private sealed class NoopImportGateway : ISwapOrderImportGateway
        {
            public bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<OrderManager.Error> errors)
            {
                importedOrder = new SwapOrder();
                errors = new List<OrderManager.Error>();
                return true;
            }
        }

        private sealed class RecordingCircuitBreaker : OpusCircuitBreaker
        {
            public RecordingCircuitBreaker() : base(1, 1, 0, 0, 1, 0)
            {
            }

            public int ExecuteCount { get; private set; }

            public bool ThrowOpenException { get; set; }

            public override async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
            {
                ExecuteCount++;
                if (ThrowOpenException)
                    throw new CircuitBreakerOpenException("forced-open");

                return await operation().ConfigureAwait(false);
            }
        }
    }
}
