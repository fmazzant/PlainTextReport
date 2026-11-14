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
    /// <summary>
    /// The aggregate of a column, printed in the totals row (<see cref="PlainTextReportOptions.ShowTotals"/>)
    /// </summary>
    public enum PlainTextReportTotal
    {
        /// <summary>
        /// No aggregate: the cell is empty, or it holds <see cref="PlainTextReportOptions.TotalsCaption"/>
        /// when it is the first column without an aggregate
        /// </summary>
        None,

        /// <summary>
        /// The sum of the values; only for numeric columns
        /// </summary>
        Sum,

        /// <summary>
        /// The number of values that are not null
        /// </summary>
        Count,

        /// <summary>
        /// The average of the values that are not null, as a decimal (a double for float and double columns),
        /// so an integer column can show decimals; only for numeric columns
        /// </summary>
        Average,

        /// <summary>
        /// The smallest value; for every column whose values can be compared (numbers, dates, texts)
        /// </summary>
        Min,

        /// <summary>
        /// The largest value; for every column whose values can be compared (numbers, dates, texts)
        /// </summary>
        Max
    }
}
