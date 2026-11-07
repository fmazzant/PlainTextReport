/// <summary>
///
/// The MIT License (MIT)
///
/// Copyright (c) 2024 Federico Mazzanti
///
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
///
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
///
/// </summary>

namespace Mafesoft.PlainTextReport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// IPlainTextReport interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPlainTextReport<T> where T : class
    {
        /// <summary>
        /// Bind the collection
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        IPlainTextReport<T> Bind(IEnumerable<T> values);

        /// <summary>
        /// Generate text report
        /// </summary>
        /// <returns></returns>
        string GetTextReport();

        /// <summary>
        /// Generate text line report
        /// </summary>
        /// <returns></returns>
        string[] GetTextLinesReport();

        /// <summary>
        /// Generate the report split in pages
        /// </summary>
        /// <returns></returns>
        PlainTextReportPage[] GetPagesReport();
    }

    /// <summary>
    /// PlainTextReport generates a plain text report from a collection of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PlainTextReport<T> : IPlainTextReport<T> where T : class
    {
        /// <summary>
        /// Options
        /// </summary>
        internal PlainTextReportOptions<T> Options { get; private set; }

        /// <summary>
        /// Collection
        /// </summary>
        public IEnumerable<T> Values { get; private set; }

        /// <summary>
        /// Create a new instance of PlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(Action<PlainTextReportOptions<T>> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = new PlainTextReportOptions<T>();
            options(Options);
        }

        /// <summary>
        /// Create a new instance of PlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(PlainTextReportOptions<T> options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Bind the collection
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public IPlainTextReport<T> Bind(IEnumerable<T> values)
        {
            Values = values;
            return this;
        }

        /// <summary>
        /// Generate text report. When the page settings are set, every page after the first one
        /// starts with <see cref="PlainTextReportPageSettings.PageBreak"/> (form feed by default).
        /// </summary>
        /// <returns></returns>
        public string GetTextReport()
        {
            var pages = this.GetPagesReport();
            var pageBreak = Options.Page?.PageBreak ?? string.Empty;
            return string.Join(Environment.NewLine + pageBreak, pages.Select(page => page.ToString()));
        }

        /// <summary>
        /// Generate text line report. The lines of all the pages, without page breaks.
        /// </summary>
        /// <returns></returns>
        public string[] GetTextLinesReport()
        {
            return new PlainTextReportRenderer<T>(Options).Render(Values);
        }

        /// <summary>
        /// Generate the report split in pages. Without page settings the report is a single page.
        /// </summary>
        /// <returns></returns>
        public PlainTextReportPage[] GetPagesReport()
        {
            return new PlainTextReportRenderer<T>(Options).RenderPages(Values);
        }
    }
}
