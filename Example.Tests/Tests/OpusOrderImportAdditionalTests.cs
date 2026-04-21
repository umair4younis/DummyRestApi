using Example.OPUS.OrderImport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.Data;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.OrderImport;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;


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

        private sealed class NoopImportGateway : ISwapOrderImportGateway
        {
            public bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<OrderManager.Error> errors)
            {
                importedOrder = new SwapOrder();
                errors = new List<OrderManager.Error>();
                return true;
            }
        }
    }
}
