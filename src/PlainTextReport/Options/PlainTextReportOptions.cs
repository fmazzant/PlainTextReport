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
    using System.Globalization;

    /// <summary>
    ///  PlainTextReport Options abstract definition
    /// </summary>
    public abstract class PlainTextReportOptions : IPlainTextReportOptions
    {
        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public string[] MultiLineTitle { get; set; }

        /// <inheritdoc />
        public bool IsMultiLineTitle => this.MultiLineTitle != null && this.MultiLineTitle.Length > 0;

        /// <inheritdoc />
        public bool MultiLineRows { get; set; }

        /// <inheritdoc />
        public bool ShowTitle { get; set; }

        /// <inheritdoc />
        public bool ShowHeader { get; set; }

        /// <inheritdoc />
        public bool ShowHeaderSeparator { get; set; }

        /// <inheritdoc />
        public bool ShowColumnSeparator { get; set; }

        /// <inheritdoc />
        public char ColumnSeparator { get; set; } = '|';

        /// <inheritdoc />
        public char HeaderSeparator { get; set; } = '-';

        /// <inheritdoc />
        public int Width { get; set; }

        /// <inheritdoc />
        public CultureInfo Culture { get; set; }

        /// <inheritdoc />
        public PlainTextReportPageSettings Page { get; set; }

        /// <inheritdoc />
        public string NewLine { get; set; }

        /// <inheritdoc />
        public string NullText { get; set; }

        /// <inheritdoc />
        public string TruncationMarker { get; set; } = "…";

        /// <inheritdoc />
        public bool UnicodeWidth { get; set; } = true;

        /// <inheritdoc />
        public bool ShowTotals { get; set; }

        /// <inheritdoc />
        public string TotalsCaption { get; set; } = "Total";

        /// <summary>
        /// Returns the line separator: <see cref="NewLine"/>, or <see cref="Environment.NewLine"/> when it is not set
        /// </summary>
        internal string GetNewLine() => string.IsNullOrEmpty(NewLine) ? Environment.NewLine : NewLine;
    }
}
