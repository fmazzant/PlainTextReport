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
    /// The column style definition
    /// </summary>
    public sealed class PlainTextReportColumnStyle
    {
        /// <summary>
        /// Caption of the column, printed in the header.
        /// When null the name of the column is used: the columnName given to AddColumn, or the name of the property.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Width of the column: a number of characters (style.Width = 20) or a percentage of the free space (style.Width = "50%").
        /// When null the width is computed from the header and the values. See <see cref="ColumnWidth"/>.
        /// </summary>
        public ColumnWidth? Width { get; set; }

        /// <summary>
        /// Alignment of the values.
        /// When null numeric columns are right aligned and the others are left aligned.
        /// </summary>
        public PlainTextReportAlignment? Alignment { get; set; }

        /// <summary>
        /// Alignment of the header.
        /// When null the alignment of the values is used.
        /// </summary>
        public PlainTextReportAlignment? HeaderAlignment { get; set; }

        /// <summary>
        /// Multi-line values: a value longer than the column wraps on more lines, and its line breaks start a new line.
        /// When null <see cref="PlainTextReportOptions.MultiLineRows"/> is used.
        /// </summary>
        public bool? MultiLine { get; set; }

        /// <summary>
        /// Text printed when the value is null, for example "-" or "n/a"; also when an intermediate value of a nested
        /// property is null, and when a custom converter returns null. When null <see cref="PlainTextReportOptions.NullText"/> is used.
        /// </summary>
        public string NullText { get; set; }

        /// <summary>
        /// The aggregate printed in the totals row when <see cref="PlainTextReportOptions.ShowTotals"/> is true:
        /// sum, count, average, minimum or maximum. It is computed from the values before they are converted to text,
        /// and formatted with the format of the column in the culture of the report. The default is none.
        /// </summary>
        public PlainTextReportTotal Total { get; set; }
    }
}
