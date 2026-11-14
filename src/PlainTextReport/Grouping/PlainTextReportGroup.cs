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
    /// The settings of the groups of the report, set by <see cref="PlainTextReportOptions{T}.GroupBy{TKey}"/>
    /// </summary>
    public sealed class PlainTextReportGroup
    {
        /// <summary>
        /// The text printed above every group. The placeholder {key} is replaced by the key of the group,
        /// and {key:format} formats it, for example "{key:yyyy-MM}" for a date, in the culture of the report.
        /// A null key is printed as <see cref="PlainTextReportOptions.NullText"/>. The default is "{key}".
        /// </summary>
        public string Header { get; set; } = "{key}";

        /// <summary>
        /// Prints a subtotals row at the end of every group, with the aggregate of every column
        /// (<see cref="PlainTextReportColumnStyle.Total"/>, the same aggregates of the totals row) computed on the
        /// rows of the group. The default is false.
        /// </summary>
        public bool ShowSubtotals { get; set; }

        /// <summary>
        /// With pages, every group after the first starts on a new page. The default is false.
        /// </summary>
        public bool NewPage { get; set; }
    }
}
