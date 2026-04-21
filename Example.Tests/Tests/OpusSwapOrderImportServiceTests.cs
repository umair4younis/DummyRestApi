using Example.OPUS.OrderImport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.Data;
using Puma.MDE.OPUS.OrderImport;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusSwapOrderImportServiceTests
    {
        [TestMethod]
        public void Parser_Can_Read_Csv_Order_File()
        {
            OpusSwapOrderFileParser parser = new OpusSwapOrderFileParser();
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration();
            SwapAccount selectedAccount = new SwapAccount { AccountName = "ACC-1", Currency = "EUR" };

            string csv = string.Join(Environment.NewLine, new[]
            {
                "AccountName,Description,Type,TradeDate,ValueDate,TradingInstruction,IndexValue,Certificates,Side,Name,RIC,ISIN,Nominal,Price,BrokerFee,HVBFee,Currency,FXRate,Accrued,Comment,ContractSize,InstrumentTypeName,TargetWeight,TargetNominal,BBG,WKN,Pool",
                "ACC-1,OPUS unwind,Transaction,2026-04-21,2026-04-22,2026-04-21,101.5,10,Sell,ABC Corp,ABC.DE,DE000ABC,150,12.34,1.1,0.2,EUR,1,0.5,from csv,1,Stock,5,100,ABC GY,ABC1,false"
            });

            SwapRawOrder order = parser.Parse(Encoding.UTF8.GetBytes(csv), "SwapRawOrder.csv", selectedAccount, configuration);

            Assert.AreEqual("ACC-1", order.AccountName);
            Assert.AreEqual(TypeManastOrder.Transaction, order.Type);
            Assert.AreEqual(1, order.Trades.Count);
            Assert.AreEqual(TypeManastTradeSide.Sell, order.Trades[0].Side);
            Assert.AreEqual(150d, order.Trades[0].Nominal);
            Assert.AreEqual("ABC Corp", order.Trades[0].Name);
            Assert.AreEqual("EUR", order.Trades[0].Currency);
            Assert.IsFalse(order.Trades[0].Pool);
        }

        [TestMethod]
        public void Parser_Can_Read_Xlsx_Order_File()
        {
            OpusSwapOrderFileParser parser = new OpusSwapOrderFileParser();
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration { SheetName = "Orders" };
            SwapAccount selectedAccount = new SwapAccount { AccountName = "ACC-2", Currency = "USD" };

            string[,] values = new string[,]
            {
                { "AccountName", "Description", "Type", "TradeDate", "Side", "Name", "RIC", "Nominal", "Price", "Currency", "FXRate", "ContractSize", "Pool" },
                { "ACC-2", "xlsx unwind", "Transaction", "2026-04-21", "Buy", "XYZ Corp", "XYZ.N", "200", "45.67", "USD", "1", "1", "true" }
            };

            byte[] workbook = CreateSimpleXlsx("Orders", values);
            SwapRawOrder order = parser.Parse(workbook, "SwapRawOrder.xlsx", selectedAccount, configuration);

            Assert.AreEqual("ACC-2", order.AccountName);
            Assert.AreEqual(1, order.Trades.Count);
            Assert.AreEqual(TypeManastTradeSide.Buy, order.Trades[0].Side);
            Assert.AreEqual("XYZ Corp", order.Trades[0].Name);
            Assert.AreEqual(200d, order.Trades[0].Nominal);
            Assert.AreEqual("USD", order.Trades[0].Currency);
            Assert.IsTrue(order.Trades[0].Pool);
        }

        [TestMethod]
        public void Import_Service_Uses_Parsed_SwapRawOrder_For_Import()
        {
            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration();
            DownloadedOrderFile downloadedFile = new DownloadedOrderFile
            {
                FileName = "SwapRawOrder.csv",
                RemotePath = "/orders/SwapRawOrder.csv",
                Content = Encoding.UTF8.GetBytes("ignored")
            };

            SwapRawOrder parsedOrder = new SwapRawOrder
            {
                AccountName = "ACC-3",
                Description = "service unwind",
                Type = TypeManastOrder.Transaction,
                TradeDate = new DateTime(2026, 4, 21),
                ValueDate = new DateTime(2026, 4, 22),
                TradingInstruction = new DateTime(2026, 4, 21)
            };
            parsedOrder.Trades.Add(new SwapRawOrder.TradeRow(TypeManastTradeSide.Sell, "ABC", "ABC.DE", "DE000ABC", 10, 1, 0, 0, "EUR", 1, 0, string.Empty, 1, "Stock", 0, 0, "ABC GY", "ABC", true));

            FakeSwapOrderImportGateway gateway = new FakeSwapOrderImportGateway();
            OpusSwapOrderImportService service = new OpusSwapOrderImportService(
                configuration,
                cfg => downloadedFile,
                (content, fileName, selectedAccount, cfg) => parsedOrder,
                gateway);

            SwapRawOrder fullUnwind;
            SwapOrder importedOrder;
            List<OrderManager.Error> errors;

            service.ImportFullUnwindOrder(new SwapAccount { AccountName = "ACC-3" }, out fullUnwind, out importedOrder, out errors);

            Assert.AreSame(parsedOrder, fullUnwind);
            Assert.AreSame(parsedOrder, gateway.ReceivedOrder);
            Assert.AreSame(gateway.ImportedOrder, importedOrder);
            Assert.AreSame(gateway.Errors, errors);
        }

        private static byte[] CreateSimpleXlsx(string sheetName, string[,] values)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    AddEntry(archive, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                        "</Types>");

                    AddEntry(archive, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                        "</Relationships>");

                    AddEntry(archive, "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                        "<sheets><sheet name=\"" + EscapeXml(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                        "</workbook>");

                    AddEntry(archive, "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                        "</Relationships>");

                    AddEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(values));
                }

                return stream.ToArray();
            }
        }

        private static void AddEntry(ZipArchive archive, string path, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path);
            using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string BuildSheetXml(string[,] values)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            builder.Append("<sheetData>");

            int rowCount = values.GetLength(0);
            int columnCount = values.GetLength(1);
            for (int row = 0; row < rowCount; row++)
            {
                builder.Append("<row r=\"").Append(row + 1).Append("\">");
                for (int column = 0; column < columnCount; column++)
                {
                    string cellReference = GetCellReference(row, column);
                    string value = values[row, column] ?? string.Empty;
                    double numericValue;
                    if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numericValue))
                    {
                        builder.Append("<c r=\"").Append(cellReference).Append("\"><v>")
                            .Append(numericValue.ToString(System.Globalization.CultureInfo.InvariantCulture))
                            .Append("</v></c>");
                    }
                    else
                    {
                        builder.Append("<c r=\"").Append(cellReference).Append("\" t=\"inlineStr\"><is><t>")
                            .Append(EscapeXml(value))
                            .Append("</t></is></c>");
                    }
                }

                builder.Append("</row>");
            }

            builder.Append("</sheetData></worksheet>");
            return builder.ToString();
        }

        private static string GetCellReference(int rowIndex, int columnIndex)
        {
            int dividend = columnIndex + 1;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName + (rowIndex + 1).ToString();
        }

        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private sealed class FakeSwapOrderImportGateway : ISwapOrderImportGateway
        {
            public FakeSwapOrderImportGateway()
            {
                ImportedOrder = new SwapOrder();
                Errors = new List<OrderManager.Error>();
            }

            public SwapRawOrder ReceivedOrder { get; private set; }

            public SwapOrder ImportedOrder { get; private set; }

            public List<OrderManager.Error> Errors { get; private set; }

            public bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<OrderManager.Error> errors)
            {
                ReceivedOrder = rawOrder;
                importedOrder = ImportedOrder;
                errors = Errors;
                return true;
            }
        }
    }
}