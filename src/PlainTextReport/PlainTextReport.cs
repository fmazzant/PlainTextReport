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

namespace PlainTextReport
{
    using PlainTextReport.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// IPlainTextReport interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPlainTextReport<T> where T : class, new()
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
    }

    /// <summary>
    /// PlainTextReport is a simple PlainTextReport reader/writer library.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed partial class PlainTextReport<T> : IPlainTextReport<T> where T : class, new()
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
        /// Create a new instance of TinyPlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(Action<PlainTextReportOptions<T>> options)
        {
            Options = new PlainTextReportOptions<T>();
            options(Options);
        }

        /// <summary>
        /// Create a new instance of TinyPlainTextReport
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReport(PlainTextReportOptions<T> options)
        {
            Options = options;
        }

        /// <summary>
        /// Bind the collection
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IPlainTextReport<T> Bind(IEnumerable<T> values)
        {
            Values = values;
            return this;
        }

        /// <summary>
        /// Generate text report
        /// </summary>
        /// <returns></returns>
        public string GetTextReport()
        {
            var lines = this.GetTextLinesReport();
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Generate text line report
        /// </summary>
        /// <returns></returns>
        public string[] GetTextLinesReport()
        {
            var lines = new List<string>();
            return lines.ToArray();
        }
    }
}
