//
// The MIT License (MIT)
//
// Copyright (c) 2024 Federico Mazzanti
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mafesoft.PlainTextReport
{
    using System;

    /// <summary>
    /// Thrown when the value of a cell cannot be read or converted: it says the row and the column,
    /// and the original exception is the <see cref="Exception.InnerException"/>.
    /// </summary>
    public sealed class PlainTextReportException : Exception
    {
        /// <summary>
        /// Create a new PlainTextReportException
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="rowNumber">The number of the row, starting from 1</param>
        /// <param name="columnName">The name of the column</param>
        /// <param name="value">The value that cannot be converted; null when it cannot be read</param>
        /// <param name="innerException">The original exception</param>
        public PlainTextReportException(string message, int rowNumber, string columnName, object value, Exception innerException)
            : base(message, innerException)
        {
            RowNumber = rowNumber;
            ColumnName = columnName;
            Value = value;
        }

        /// <summary>
        /// The number of the row, starting from 1: the position of the item in the bound collection
        /// </summary>
        public int RowNumber { get; }

        /// <summary>
        /// The name of the column
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// The value that cannot be converted; null when the value cannot be read
        /// </summary>
        public object Value { get; }
    }
}
