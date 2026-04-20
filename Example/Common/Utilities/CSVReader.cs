using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Puma.MDE.Common.Utilities
{
    /// <summary>
    /// Read CSV-formatted data from a file or TextReader
    /// </summary>
    public class CsvReader : IDisposable
    {
        // Carraige return and newline
        public const string CrNewline = "\r\n";
        public const string Newline = "\n";

        /// <summary>
        /// This reader will read all of the CSV data
        /// </summary>
        private readonly BinaryReader _reader;

        #region Constructors

        /// <summary>
        /// Read CSV-formatted data from a file
        /// </summary>
        /// <param name="csvFileInfo">Name of the CSV file</param>
        public CsvReader(FileInfo csvFileInfo)
        {
            if (csvFileInfo == null)
                throw new ArgumentNullException("csvFileInfo", "Null FileInfo passed to CSVReader");

            _reader = new BinaryReader(File.OpenRead(csvFileInfo.FullName));
            _reader.BaseStream.Position = 0;
        }

        /// <summary>
        /// Read CSV-formatted data from a string
        /// </summary>
        /// <param name="csvData">String containing CSV data</param>
        public CsvReader(string csvData)
        {
            if (csvData == null)
                throw new ArgumentNullException("csvData", "Null string passed to CSVReader");


            _reader = new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes(csvData)));
        }

        /// <summary>
        /// Read CSV-formatted data from a TextReader
        /// </summary>
        /// <param name="reader">TextReader that's reading CSV-formatted data</param>
        public CsvReader(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", "Null TextReader passed to CSVReader");

            _reader = new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes(reader.ReadToEnd())));
        }

        #endregion



        string _currentLine = "";
        /// <summary>
        /// Read the next row from the CSV data
        /// </summary>
        /// <returns>A list of objects read from the row, or null if there is no next row</returns>
        public List<string> ReadRow()
        {
            // ReadLine() will return null if there's no next line
            if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
                return null;

            var builder = new StringBuilder();

            // Read the next line
            while ((_reader.BaseStream.Position < _reader.BaseStream.Length))
            {
                var soFar = builder.ToString();
                if (soFar.EndsWith(Newline) || soFar.EndsWith(CrNewline))
                    break;

                char c = _reader.ReadChar();
                builder.Append(c);
            }

            _currentLine = builder.ToString();
            if (_currentLine.EndsWith(Newline))
                _currentLine = _currentLine.Remove(_currentLine.IndexOf(Newline, StringComparison.InvariantCulture), Newline.Length);

            if (_currentLine.EndsWith(CrNewline))
                _currentLine = _currentLine.Remove(_currentLine.IndexOf(CrNewline, StringComparison.InvariantCulture), CrNewline.Length);

            // Build the list of objects in the line
            var fields = new List<string>();
            while (_currentLine != string.Empty)
                fields.Add(ReadNextObject());

            if (builder.ToString().EndsWith(","))
                fields.Add(string.Empty);

            return fields;
        }

        /// <summary>
        /// Read the next object from the currentLine string
        /// </summary>
        /// <returns>The next object in the currentLine string</returns>
        private string ReadNextObject()
        {
            if (_currentLine == null)
                return null;

            // Check to see if the next value is quoted
            var quoted = _currentLine.StartsWith("\"");

            // Find the end of the next value
            int i = 0;
            int len = _currentLine.Length;
            bool foundEnd = false;
            while (!foundEnd && i <= len)
            {
                // Check if we've hit the end of the string
                if ((!quoted && i == len) // non-quoted strings end with a comma or end of line
                    || (!quoted && _currentLine.Substring(i, 1) == ",")
                    // quoted strings end with a quote followed by a comma or end of line
                    || (quoted && i == len - 1 && _currentLine.EndsWith("\""))
                    || (quoted && _currentLine.Substring(i, 2) == "\","))
                    foundEnd = true;
                else
                    i++;
            }
            if (quoted)
            {
                if (i > len || !_currentLine.Substring(i, 1).StartsWith("\""))
                    throw new FormatException("Invalid CSV format: " + _currentLine.Substring(0, i));
                i++;
            }
            string nextObjectString = _currentLine.Substring(0, i).Replace("\"\"", "\"");

            if (i < len)
                _currentLine = _currentLine.Substring(i + 1);
            else
                _currentLine = string.Empty;

            if (quoted)
            {
                if (nextObjectString.StartsWith("\""))
                    nextObjectString = nextObjectString.Substring(1);
                if (nextObjectString.EndsWith("\""))
                    nextObjectString = nextObjectString.Substring(0, nextObjectString.Length - 1);
                return nextObjectString;
            }
            else
            {
                //object convertedValue;
                //StringConverter.ConvertString(nextObjectString, out convertedValue);
                //return convertedValue;
                return nextObjectString;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_reader == null)
                return;

            try
            {
                // Can't call BinaryReader.Dispose due to its protection level
                _reader.Close();
            }
            catch { }
        }

        #endregion
    }
}
