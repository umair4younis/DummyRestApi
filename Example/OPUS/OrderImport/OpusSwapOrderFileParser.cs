using Puma.MDE.Common.Utilities;
using Puma.MDE.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSwapOrderFileParser
    {
        public SwapRawOrder Parse(byte[] fileContent, string fileName, SwapAccount selectedAccount, OpusSftpOrderImportConfiguration configuration)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new InvalidOperationException("The OPUS order file is empty.");

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderFileParser] Parsing started. FileName={0}, SizeBytes={1}, HeaderRowIndex={2}, SheetName={3}",
                fileName ?? "<null>",
                fileContent.Length,
                configuration.HeaderRowIndex,
                string.IsNullOrWhiteSpace(configuration.SheetName) ? "<default>" : configuration.SheetName));

            TabularOrderData tabularOrderData = ReadTabularData(fileContent, fileName, configuration);
            SwapRawOrder order = BuildOrder(tabularOrderData, fileName, selectedAccount, configuration);

            if (order.Trades.Count == 0)
                throw new InvalidOperationException("The OPUS order file does not contain any trade rows.");

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderFileParser] Parsing completed. AccountName={0}, OrderType={1}, Trades={2}",
                order.AccountName ?? "<null>",
                order.Type,
                order.Trades.Count));

            return order;
        }

        private static TabularOrderData ReadTabularData(byte[] fileContent, string fileName, OpusSftpOrderImportConfiguration configuration)
        {
            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            switch (extension)
            {
                case ".csv":
                    return ReadCsv(fileContent, configuration);
                case ".xls":
                    return ReadLegacyWorkbookWithOleDb(fileContent, ".xls", configuration);
                case ".xlsx":
                    return ReadXlsx(fileContent, configuration);
                default:
                    throw new NotSupportedException("Unsupported OPUS order file extension '" + extension + "'.");
            }
        }

        private static TabularOrderData ReadCsv(byte[] fileContent, OpusSftpOrderImportConfiguration configuration)
        {
            List<IList<string>> rows = new List<IList<string>>();
            using (StreamReader streamReader = new StreamReader(new MemoryStream(fileContent), Encoding.UTF8, true))
            using (CsvReader csvReader = new CsvReader(streamReader))
            {
                List<string> row;
                while ((row = csvReader.ReadRow()) != null)
                {
                    rows.Add(row);
                }
            }

            Engine.Instance.Log.Debug("[OpusSwapOrderFileParser] CSV rows read: " + rows.Count);

            return BuildTabularData(rows, configuration);
        }

        private static TabularOrderData ReadXlsx(byte[] fileContent, OpusSftpOrderImportConfiguration configuration)
        {
            List<IList<string>> rows = new List<IList<string>>();

            using (MemoryStream memoryStream = new MemoryStream(fileContent))
            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, true))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                Dictionary<string, string> rels = ReadWorkbookRelationships(archive);
                string sheetPartPath = ResolveWorksheetPartPath(archive, rels, configuration.SheetName);
                rows = ReadWorksheetRows(archive, sheetPartPath, sharedStrings);
            }

            Engine.Instance.Log.Debug("[OpusSwapOrderFileParser] Workbook rows read: " + rows.Count);

            return BuildTabularData(rows, configuration);
        }

        private static TabularOrderData ReadLegacyWorkbookWithOleDb(byte[] fileContent, string extension, OpusSftpOrderImportConfiguration configuration)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
            try
            {
                File.WriteAllBytes(tempFilePath, fileContent);

                string connectionString = extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
                    ? string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\";", tempFilePath)
                    : string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0 Xml;HDR=NO;IMEX=1\";", tempFilePath);

                List<IList<string>> rows = new List<IList<string>>();
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    DataTable sheets = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    if (sheets == null || sheets.Rows.Count == 0)
                        throw new InvalidOperationException("No worksheets were found in the OPUS order file.");

                    string selectedSheet = FindSheetName(sheets, configuration.SheetName);
                    if (string.IsNullOrWhiteSpace(selectedSheet))
                        throw new InvalidOperationException("The configured worksheet '" + configuration.SheetName + "' was not found in the OPUS order file.");

                    using (OleDbCommand command = new OleDbCommand("SELECT * FROM [" + selectedSheet + "]", connection))
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                List<string> row = new List<string>();
                                for (int columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                                {
                                    row.Add(ConvertCellToString(reader.GetValue(columnIndex)));
                                }

                                rows.Add(row);
                            }
                        }
                    }
                }

                Engine.Instance.Log.Debug("[OpusSwapOrderFileParser] Legacy workbook rows read (OLE DB): " + rows.Count);
                return BuildTabularData(rows, configuration);
            }
            catch (OleDbException ex)
            {
                throw new InvalidOperationException(
                    "Failed to parse Excel .xls file without NuGet dependencies. Ensure Microsoft Access Database Engine is installed.",
                    ex);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch
                {
                }
            }
        }

        private static string FindSheetName(DataTable sheets, string configuredSheetName)
        {
            string defaultSheetName = null;
            foreach (DataRow sheetRow in sheets.Rows)
            {
                string tableName = Convert.ToString(sheetRow["TABLE_NAME"], CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(tableName))
                    continue;

                if (defaultSheetName == null && tableName.EndsWith("$", StringComparison.OrdinalIgnoreCase))
                    defaultSheetName = tableName;

                if (string.IsNullOrWhiteSpace(configuredSheetName))
                    continue;

                string normalized = tableName.Trim('\'', '"');
                string expected = configuredSheetName.EndsWith("$", StringComparison.Ordinal) ? configuredSheetName : configuredSheetName + "$";
                if (normalized.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    return tableName;
            }

            return string.IsNullOrWhiteSpace(configuredSheetName) ? defaultSheetName : null;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            List<string> values = new List<string>();
            if (entry == null)
                return values;

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            using (Stream stream = entry.Open())
            {
                XDocument document = XDocument.Load(stream);
                IEnumerable<XElement> stringItems = document.Root == null
                    ? Enumerable.Empty<XElement>()
                    : document.Root.Elements(ns + "si");

                foreach (XElement si in stringItems)
                {
                    string value = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                    values.Add(value ?? string.Empty);
                }
            }

            return values;
        }

        private static Dictionary<string, string> ReadWorkbookRelationships(ZipArchive archive)
        {
            Dictionary<string, string> rels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ZipArchiveEntry relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (relsEntry == null)
                return rels;

            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            using (Stream stream = relsEntry.Open())
            {
                XDocument document = XDocument.Load(stream);
                if (document.Root == null)
                    return rels;

                foreach (XElement relationship in document.Root.Elements(relNs + "Relationship"))
                {
                    XAttribute idAttr = relationship.Attribute("Id");
                    XAttribute targetAttr = relationship.Attribute("Target");
                    if (idAttr == null || targetAttr == null)
                        continue;

                    rels[idAttr.Value] = targetAttr.Value;
                }
            }

            return rels;
        }

        private static string ResolveWorksheetPartPath(ZipArchive archive, IDictionary<string, string> rels, string configuredSheetName)
        {
            ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
            if (workbookEntry == null)
                throw new InvalidOperationException("The OPUS .xlsx file is missing xl/workbook.xml.");

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            using (Stream stream = workbookEntry.Open())
            {
                XDocument workbook = XDocument.Load(stream);
                XElement sheets = workbook.Root == null ? null : workbook.Root.Element(ns + "sheets");
                if (sheets == null)
                    throw new InvalidOperationException("The OPUS .xlsx file does not contain any worksheets.");

                XElement selectedSheet = null;
                foreach (XElement sheet in sheets.Elements(ns + "sheet"))
                {
                    string name = (string)sheet.Attribute("name");
                    if (selectedSheet == null)
                        selectedSheet = sheet;

                    if (!string.IsNullOrWhiteSpace(configuredSheetName)
                        && string.Equals(name, configuredSheetName, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedSheet = sheet;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(configuredSheetName))
                {
                    string selectedName = (string)(selectedSheet == null ? null : selectedSheet.Attribute("name"));
                    if (!string.Equals(selectedName, configuredSheetName, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The configured worksheet '" + configuredSheetName + "' was not found in the OPUS order file.");
                }

                if (selectedSheet == null)
                    throw new InvalidOperationException("No worksheet could be selected from the OPUS .xlsx file.");

                string relId = (string)selectedSheet.Attribute(relNs + "id");
                if (string.IsNullOrWhiteSpace(relId) || !rels.ContainsKey(relId))
                    throw new InvalidOperationException("Worksheet relationship could not be resolved in the OPUS .xlsx file.");

                string target = rels[relId].Replace('\\', '/').TrimStart('/');
                if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                    target = "xl/" + target;

                if (archive.GetEntry(target) == null)
                    throw new InvalidOperationException("Worksheet XML part was not found: " + target);

                return target;
            }
        }

        private static List<IList<string>> ReadWorksheetRows(ZipArchive archive, string sheetPartPath, IList<string> sharedStrings)
        {
            ZipArchiveEntry sheetEntry = archive.GetEntry(sheetPartPath);
            if (sheetEntry == null)
                throw new InvalidOperationException("Worksheet XML part was not found: " + sheetPartPath);

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            List<IList<string>> rows = new List<IList<string>>();

            using (Stream stream = sheetEntry.Open())
            {
                XDocument document = XDocument.Load(stream);
                IEnumerable<XElement> rowElements = document.Descendants(ns + "row");
                foreach (XElement rowElement in rowElements)
                {
                    List<string> rowValues = new List<string>();
                    foreach (XElement cell in rowElement.Elements(ns + "c"))
                    {
                        string reference = (string)cell.Attribute("r");
                        int columnIndex = GetCellColumnIndex(reference);
                        while (rowValues.Count <= columnIndex)
                            rowValues.Add(string.Empty);

                        rowValues[columnIndex] = ReadCellValue(cell, ns, sharedStrings);
                    }

                    rows.Add(rowValues);
                }
            }

            return rows;
        }

        private static int GetCellColumnIndex(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return 0;

            int index = 0;
            for (int i = 0; i < cellReference.Length; i++)
            {
                char character = cellReference[i];
                if (!char.IsLetter(character))
                    break;

                index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
            }

            return Math.Max(0, index - 1);
        }

        private static string ReadCellValue(XElement cell, XNamespace ns, IList<string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");

            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                XElement inline = cell.Element(ns + "is");
                if (inline == null)
                    return string.Empty;

                return string.Concat(inline.Descendants(ns + "t").Select(t => t.Value));
            }

            string rawValue = (string)cell.Element(ns + "v");
            if (string.IsNullOrWhiteSpace(rawValue))
                return string.Empty;

            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                int sharedStringIndex;
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out sharedStringIndex)
                    && sharedStringIndex >= 0
                    && sharedStringIndex < sharedStrings.Count)
                {
                    return sharedStrings[sharedStringIndex] ?? string.Empty;
                }

                return string.Empty;
            }

            if (string.Equals(type, "b", StringComparison.OrdinalIgnoreCase))
                return rawValue == "1" ? "true" : "false";

            return rawValue;
        }

        private static TabularOrderData BuildTabularData(IList<IList<string>> rawRows, OpusSftpOrderImportConfiguration configuration)
        {
            if (rawRows == null || rawRows.Count < configuration.HeaderRowIndex)
                throw new InvalidOperationException("The OPUS order file does not contain a valid header row.");

            TabularOrderData tabularOrderData = new TabularOrderData();

            for (int rowIndex = 0; rowIndex < configuration.HeaderRowIndex - 1; rowIndex++)
            {
                CaptureMetadata(tabularOrderData.Metadata, rawRows[rowIndex]);
            }

            IList<string> headerRow = rawRows[configuration.HeaderRowIndex - 1];
            List<string> headers = new List<string>();
            for (int columnIndex = 0; columnIndex < headerRow.Count; columnIndex++)
            {
                string normalized = NormalizeHeader(headerRow[columnIndex]);
                headers.Add(string.IsNullOrWhiteSpace(normalized) ? "column" + columnIndex.ToString(CultureInfo.InvariantCulture) : normalized);
            }

            for (int rowIndex = configuration.HeaderRowIndex; rowIndex < rawRows.Count; rowIndex++)
            {
                IList<string> row = rawRows[rowIndex];
                if (row == null || row.All(string.IsNullOrWhiteSpace))
                    continue;

                Dictionary<string, string> mappedRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    string value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    mappedRow[headers[columnIndex]] = value == null ? string.Empty : value.Trim();
                }

                tabularOrderData.Rows.Add(mappedRow);
            }

            Engine.Instance.Log.Debug(string.Format(
                "[OpusSwapOrderFileParser] Tabular data prepared. MetadataRows={0}, DataRows={1}, Headers={2}",
                tabularOrderData.Metadata.Count,
                tabularOrderData.Rows.Count,
                headers.Count));

            return tabularOrderData;
        }

        private static SwapRawOrder BuildOrder(TabularOrderData tabularOrderData, string fileName, SwapAccount selectedAccount, OpusSftpOrderImportConfiguration configuration)
        {
            string fileLabel = Path.GetFileNameWithoutExtension(fileName ?? "opus-order");
            SwapRawOrder order = new SwapRawOrder
            {
                AccountName = GetFirstValue(tabularOrderData, "accountname", "account", "portfolio") ?? (selectedAccount == null ? null : selectedAccount.AccountName),
                Description = GetFirstValue(tabularOrderData, "description", "orderdescription", "comment") ?? ("OPUS full unwind import " + fileLabel),
                Type = ParseOrderType(GetFirstValue(tabularOrderData, "type", "ordertype"), configuration.DefaultOrderType),
                IndexValue = ParseDouble(GetFirstValue(tabularOrderData, "indexvalue", "index"), 0.0),
                TradeDate = ParseDate(GetFirstValue(tabularOrderData, "tradedate", "date"), DateTime.Today),
                ValueDate = ParseDate(GetFirstValue(tabularOrderData, "valuedate", "settlementdate"), DateTime.Today),
                TradingInstruction = ParseDate(GetFirstValue(tabularOrderData, "tradinginstruction", "instructiondate"), DateTime.Today),
                Certificates = ParseDouble(GetFirstValue(tabularOrderData, "certificates"), 0.0),
                UpdatePoolUpon = configuration.DefaultUpdatePoolUpon
            };

            if (string.IsNullOrWhiteSpace(order.AccountName))
                throw new InvalidOperationException("The OPUS order file does not define an account name and no selected account was supplied.");

            int skippedRows = 0;

            foreach (Dictionary<string, string> row in tabularOrderData.Rows)
            {
                if (!IsTradeRow(row))
                {
                    skippedRows++;
                    continue;
                }

                double nominal = ParseDouble(GetValue(row, "nominal", "size", "quantity", "shares"), 0.0);
                string sideValue = GetValue(row, "side", "tradeside", "orderside", "buyorsell");
                TypeManastTradeSide side = ParseTradeSide(sideValue, nominal);

                SwapRawOrder.TradeRow tradeRow = new SwapRawOrder.TradeRow(
                    side,
                    GetTradeName(row),
                    GetValue(row, "ric", "reuters", "ticker"),
                    GetValue(row, "isin"),
                    Math.Abs(nominal),
                    ParseDouble(GetValue(row, "price", "tradeprice"), 0.0),
                    ParseDouble(GetValue(row, "brokerfee", "brokercommission", "brokercost"), 0.0),
                    ParseDouble(GetValue(row, "hvbfee", "internalfee"), 0.0),
                    GetValue(row, "currency", "tradecurrency") ?? configuration.DefaultCurrency ?? GetAccountCurrency(selectedAccount),
                    ParseDouble(GetValue(row, "fxrate", "fx", "exchangerate"), configuration.DefaultFxRate),
                    ParseDouble(GetValue(row, "accrued", "accruedinterest"), 0.0),
                    GetValue(row, "comment", "description", "notes"),
                    ParseDouble(GetValue(row, "contractsize", "lotsize"), configuration.DefaultContractSize),
                    GetValue(row, "instrumenttypename", "instrumenttype", "securitytype") ?? configuration.DefaultInstrumentTypeName,
                    ParseDouble(GetValue(row, "targetweight", "weight"), 0.0),
                    ParseDouble(GetValue(row, "targetnominal", "targetsize"), 0.0),
                    GetValue(row, "bbg", "bloomberg", "bbgticker"),
                    GetValue(row, "wkn"),
                    ParseBool(GetValue(row, "pool", "updatepool"), true));

                order.Trades.Add(tradeRow);
            }

            if (skippedRows > 0)
                Engine.Instance.Log.Debug("[OpusSwapOrderFileParser] Non-trade rows skipped: " + skippedRows);

            return order;
        }

        private static string GetAccountCurrency(SwapAccount selectedAccount)
        {
            return selectedAccount == null ? null : selectedAccount.Currency;
        }

        private static string GetTradeName(IDictionary<string, string> row)
        {
            string value = GetValue(row, "name", "instrumentname", "securityname", "instrument");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = GetValue(row, "bbg", "bloomberg", "bbgticker");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = GetValue(row, "ric", "ticker", "isin", "wkn");
            return value ?? string.Empty;
        }

        private static bool IsTradeRow(IDictionary<string, string> row)
        {
            return !string.IsNullOrWhiteSpace(GetValue(row, "name", "instrumentname", "securityname", "instrument", "ric", "isin", "bbg", "wkn"))
                || !string.IsNullOrWhiteSpace(GetValue(row, "nominal", "size", "quantity", "shares"))
                || !string.IsNullOrWhiteSpace(GetValue(row, "side", "tradeside", "orderside", "buyorsell"));
        }

        private static void CaptureMetadata(IDictionary<string, string> metadata, IList<string> row)
        {
            if (row == null || row.Count < 2)
                return;

            string key = NormalizeHeader(row[0]);
            string value = row[1];
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            metadata[key] = value.Trim();
        }

        private static string GetFirstValue(TabularOrderData tabularOrderData, params string[] aliases)
        {
            string value = GetValue(tabularOrderData.Metadata, aliases);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            foreach (Dictionary<string, string> row in tabularOrderData.Rows)
            {
                value = GetValue(row, aliases);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string GetValue(IDictionary<string, string> values, params string[] aliases)
        {
            if (values == null || aliases == null)
                return null;

            foreach (string alias in aliases)
            {
                string normalizedAlias = NormalizeHeader(alias);
                string value;
                if (values.TryGetValue(normalizedAlias, out value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }

        private static string ConvertCellToString(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is DateTime)
                return ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);

            if (value is double)
                return ((double)value).ToString("G17", CultureInfo.InvariantCulture);

            if (value is float)
                return ((float)value).ToString("G9", CultureInfo.InvariantCulture);

            if (value is decimal)
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);

            if (value is bool)
                return ((bool)value) ? "true" : "false";

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static TypeManastOrder ParseOrderType(string value, TypeManastOrder fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string normalized = NormalizeHeader(value);
            if (normalized == "fullunwind")
                return TypeManastOrder.Transaction;

            TypeManastOrder parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
        }

        private static TypeManastTradeSide ParseTradeSide(string value, double nominal)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string normalized = NormalizeHeader(value);
                if (normalized == "sell" || normalized == "s")
                    return TypeManastTradeSide.Sell;

                if (normalized == "buy" || normalized == "b")
                    return TypeManastTradeSide.Buy;
            }

            return nominal < 0 ? TypeManastTradeSide.Sell : TypeManastTradeSide.Buy;
        }

        private static DateTime ParseDate(string value, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            DateTime parsed;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
                return parsed;

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsed))
                return parsed;

            if (DateTime.TryParse(value, new CultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out parsed))
                return parsed;

            double oaDate;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static double ParseDouble(string value, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string trimmed = value.Trim().Replace("%", string.Empty).Replace(" ", string.Empty);
            double parsed;
            if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                return parsed;

            if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                return parsed;

            if (double.TryParse(trimmed, NumberStyles.Any, new CultureInfo("de-DE"), out parsed))
                return parsed;

            string normalized = trimmed.Replace(",", string.Empty);
            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                return parsed;

            normalized = trimmed.Replace(".", string.Empty).Replace(",", ".");
            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                return parsed;

            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string normalized = NormalizeHeader(value);
            if (normalized == "true" || normalized == "1" || normalized == "yes" || normalized == "y")
                return true;

            if (normalized == "false" || normalized == "0" || normalized == "no" || normalized == "n")
                return false;

            return fallback;
        }

        private sealed class TabularOrderData
        {
            public TabularOrderData()
            {
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Rows = new List<Dictionary<string, string>>();
            }

            public Dictionary<string, string> Metadata { get; private set; }

            public List<Dictionary<string, string>> Rows { get; private set; }
        }
    }
}