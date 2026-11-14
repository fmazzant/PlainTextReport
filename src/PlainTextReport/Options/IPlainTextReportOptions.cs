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
    using System.Globalization;

    /// <summary>
    ///  PlainTextReport Options abstract definition
    /// </summary>
    public interface IPlainTextReportOptions
    {
        /// <summary>
        /// Report's title
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Report's multi title
        /// </summary>
        string[] MultiLineTitle { get; set; }

        /// <summary>
        /// Returns is report's title is multi-line
        /// </summary>
        bool IsMultiLineTitle { get; }

        /// <summary>
        /// Multi-line rows: a value longer than its column wraps on more lines, and the line breaks
        /// of a value start a new line. When false a value is kept on one line and truncated with an ellipsis.
        /// A column can override it with <see cref="PlainTextReportColumnStyle.MultiLine"/>.
        /// </summary>
        bool MultiLineRows { get; set; }

        /// <summary>
        /// Multi-line header: a caption longer than its column wraps on more lines, and the line breaks of a caption
        /// ("Created\non") start a new line. The automatic columns are sized on the longest word of the caption, not
        /// on the whole caption. When false a caption is kept on one line and truncated.
        /// </summary>
        bool MultiLineHeader { get; set; }

        /// <summary>
        /// Show Title
        /// </summary>
        bool ShowTitle { get; set; }

        /// <summary>
        /// Show header columns
        /// </summary>
        bool ShowHeader { get; set; }

        /// <summary>
        /// Show header separator
        /// </summary>
        bool ShowHeaderSeparator { get; set; }

        /// <summary>
        /// Show column separator
        /// </summary>
        bool ShowColumnSeparator { get; set; }

        /// <summary>
        /// Character used to separate the columns
        /// </summary>
        char ColumnSeparator { get; set; }

        /// <summary>
        /// Character used to draw the line below the header
        /// </summary>
        char HeaderSeparator { get; set; }

        /// <summary>
        /// Maximum report width, in characters. Zero means no limit.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Culture used to format the values (numbers, dates, the column formats) and the {date} placeholder.
        /// When null the invariant culture is used.
        /// </summary>
        CultureInfo Culture { get; set; }

        /// <summary>
        /// Page settings. When set, the report is split in pages with header and footer.
        /// </summary>
        PlainTextReportPageSettings Page { get; set; }

        /// <summary>
        /// Line separator of the report, for example "\r\n" for a printer or "\n" for the Linux tools.
        /// When null or empty <see cref="System.Environment.NewLine"/> is used.
        /// </summary>
        string NewLine { get; set; }

        /// <summary>
        /// Text printed when a value is null, for example "-" or "n/a"; also when an intermediate value of a nested
        /// property is null, and when a custom converter returns null. When null the cell is empty.
        /// A column can override it with <see cref="PlainTextReportColumnStyle.NullText"/>.
        /// </summary>
        string NullText { get; set; }

        /// <summary>
        /// Text at the end of a truncated value, of a truncated title and of a truncated header or footer text.
        /// The default is the ellipsis "…"; "" truncates without a marker, and "..." or "~" are fine for the printer
        /// code pages without the ellipsis (437, 850). When null the default is used. When a column is narrower than
        /// the marker, the text is cut without it.
        /// </summary>
        string TruncationMarker { get; set; }

        /// <summary>
        /// Measures the text by its display width: East Asian wide characters (Chinese, Japanese, Korean) and emoji
        /// take 2 columns, combining marks 0, and a character is never cut in half. The default is true; false counts
        /// the UTF-16 code units (string.Length), which is faster for texts without such characters.
        /// </summary>
        bool UnicodeWidth { get; set; }

        /// <summary>
        /// Prints a totals row at the end of the report, with the aggregate of every column
        /// (<see cref="PlainTextReportColumnStyle.Total"/>). With pages it is printed at the end of the last page.
        /// It is preceded by a separator line when <see cref="ShowHeaderSeparator"/> is true.
        /// </summary>
        bool ShowTotals { get; set; }

        /// <summary>
        /// The text printed in the totals row, in the first column without an aggregate; "Total" by default.
        /// When every column has an aggregate it is not printed.
        /// </summary>
        string TotalsCaption { get; set; }
    }
}
