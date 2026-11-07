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
    using Mafesoft.PlainTextReport.Helpers;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    ///  PlainTextReport Options abstract definition
    /// </summary>
    public interface IPlainTextReportOptions
    {
        /// <summary>
        /// Report's title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Report's multi title
        /// </summary>
        public string[] MultiLineTitle { get; set; }

        /// <summary>
        /// Returns is report's title is multi-line
        /// </summary>
        public bool IsMultiLineTitle { get; }

        /// <summary>
        /// Show Title
        /// </summary>
        public bool ShowTitle { get; set; }

        /// <summary>
        /// Show header columns
        /// </summary>
        public bool ShowHeader { get; set; }

        /// <summary>
        /// Show header separator
        /// </summary>
        public bool ShowHeaderSeparator { get; set; }

        /// <summary>
        /// Show column separator
        /// </summary>
        public bool ShowColumnSeparator { get; set; }

        /// <summary>
        /// Character used to separate the columns
        /// </summary>
        public char ColumnSeparator { get; set; }

        /// <summary>
        /// Character used to draw the line below the header
        /// </summary>
        public char HeaderSeparator { get; set; }

        /// <summary>
        /// Maximum report width, in characters. Zero means no limit.
        /// </summary>
        public int Width { get; set; }
    }

    /// <summary>
    ///  PlainTextReport Options abstract definition
    /// </summary>
    public abstract class PlainTextReportOptions : IPlainTextReportOptions
    {
        /// <summary>
        /// Report's title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Report's multi title
        /// </summary>
        public string[] MultiLineTitle { get; set; }

        /// <summary>
        /// Returns is report's title is multi-line
        /// </summary>
        public bool IsMultiLineTitle => this.MultiLineTitle != null && this.MultiLineTitle.Length > 0;

        /// <summary>
        /// Show Title
        /// </summary>
        public bool ShowTitle { get; set; }

        /// <summary>
        /// Show header columns
        /// </summary>
        public bool ShowHeader { get; set; }

        /// <summary>
        /// Show header separator
        /// </summary>
        public bool ShowHeaderSeparator { get; set; }

        /// <summary>
        /// Show column separator
        /// </summary>
        public bool ShowColumnSeparator { get; set; }

        /// <summary>
        /// Character used to separate the columns
        /// </summary>
        public char ColumnSeparator { get; set; } = '|';

        /// <summary>
        /// Character used to draw the line below the header
        /// </summary>
        public char HeaderSeparator { get; set; } = '-';

        /// <summary>
        /// Maximum report width, in characters. Zero means no limit.
        /// </summary>
        public int Width { get; set; }
    }

    /// <summary>
    /// PlainTextReport Options definition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PlainTextReportOptions<T> : PlainTextReportOptions
    {
        /// <summary>
        /// Columns
        /// </summary>
        public PlainTextReportOptionsColumns<T> Columns { get; internal set; }

        /// <summary>
        /// Object's faster properties
        /// </summary>
        internal ConcurrentDictionary<string, PropertyHelper<T>> FasterProperties { get; set; }

        /// <summary>
        /// Create a PlainTextReportOptions
        /// </summary>
        public PlainTextReportOptions()
        {
            Columns = new PlainTextReportOptionsColumns<T>();
            this.SetOptionPropertiesFromType(typeof(T));
        }

        /// <summary>
        /// Set Option Properties From Type
        /// </summary>
        /// <param name="type"></param>
        private void SetOptionPropertiesFromType(Type type)
        {
            FasterProperties = PropertyHelper<T>.GetPropertiesExpressionTree(type);
        }

    }
}
