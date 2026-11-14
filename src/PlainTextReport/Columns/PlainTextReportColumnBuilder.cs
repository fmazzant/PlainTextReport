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
    using Mafesoft.PlainTextReport.Conversions;
    using Mafesoft.PlainTextReport.Factory;
    using System;

    /// <summary>
    /// Sets every setting of a column in one place, used by
    /// <see cref="PlainTextReportOptionsColumns{M}.Add{T}(System.Linq.Expressions.Expression{Func{M, T}}, Action{PlainTextReportColumnBuilder})"/>:
    /// <code>
    /// options.Columns.Add(m => m.Price, column => column
    ///     .Caption("Price")
    ///     .Format("N2")
    ///     .Width(10)
    ///     .Align(PlainTextReportAlignment.Right));
    /// </code>
    /// Every method returns the builder, so the calls can be chained; when a setting is set twice the last one wins.
    /// </summary>
    public sealed class PlainTextReportColumnBuilder
    {
        private readonly PlainTextReportColumn column;

        /// <summary>
        /// Create a builder of the given column
        /// </summary>
        /// <param name="column">The column to configure</param>
        internal PlainTextReportColumnBuilder(PlainTextReportColumn column)
        {
            this.column = column;
        }

        /// <summary>
        /// The caption of the column, printed in the header. When it is not set the name of the column is used.
        /// </summary>
        /// <param name="caption">The caption; null for the name of the column</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder Caption(string caption)
        {
            column.Style.Caption = caption;
            return this;
        }

        /// <summary>
        /// The position of the column: the columns are printed in index order; columns with the same index keep the
        /// order they were added in. When it is not set the index is the number of columns already added.
        /// </summary>
        /// <param name="index">The index, zero or greater</param>
        /// <returns>The builder</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is negative</exception>
        public PlainTextReportColumnBuilder Index(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The index of column '{column.ColumnName}' cannot be negative.");
            }
            column.ColumnIndex = index;
            return this;
        }

        /// <summary>
        /// The format of the values, for example N2 or dd/MM/yyyy, applied in the culture of the report
        /// </summary>
        /// <param name="format">The format; null for the default format of the type</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder Format(string format)
        {
            column.ColumnFormat = format;
            return this;
        }

        /// <summary>
        /// The format provider of the values; it wins over the culture of the report. When it is a
        /// <see cref="System.Globalization.CultureInfo"/>, <see cref="Format"/> is applied in that culture:
        /// Format("N2").FormatProvider(new CultureInfo("it-IT")) prints 1.234,50.
        /// </summary>
        /// <param name="formatProvider">The format provider, for example new CultureInfo("it-IT"); null for the culture of the report</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder FormatProvider(IFormatProvider formatProvider)
        {
            column.ColumnFormatProvider = formatProvider;
            return this;
        }

        /// <summary>
        /// Converts the values to text
        /// </summary>
        /// <param name="converter">The converter; null for the default converter of the type</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder Converter(IValueConverter converter)
        {
            column.Converter = converter ?? ValueConverterFactory.CreateValueConverterByType(column.ColumnType);
            column.HasDefaultConverter = converter == null;
            return this;
        }

        /// <summary>
        /// The width of the column: a number of characters (Width(20)) or a percentage of the free space (Width("50%")).
        /// When it is not set the width is computed from the header and the values. See <see cref="ColumnWidth"/>.
        /// </summary>
        /// <param name="width">The width; null for the automatic width</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder Width(ColumnWidth? width)
        {
            column.Style.Width = width;
            return this;
        }

        /// <summary>
        /// The alignment of the values. When it is not set numeric columns are right aligned and the others are left aligned.
        /// </summary>
        /// <param name="alignment">The alignment; null for the default one</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder Align(PlainTextReportAlignment? alignment)
        {
            column.Style.Alignment = alignment;
            return this;
        }

        /// <summary>
        /// The alignment of the header. When it is not set the alignment of the values is used.
        /// </summary>
        /// <param name="alignment">The alignment; null for the alignment of the values</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder HeaderAlign(PlainTextReportAlignment? alignment)
        {
            column.Style.HeaderAlignment = alignment;
            return this;
        }

        /// <summary>
        /// Multi-line values: a value longer than the column wraps on more lines, and its line breaks start a new line.
        /// When it is not set <see cref="PlainTextReportOptions.MultiLineRows"/> is used.
        /// </summary>
        /// <param name="multiLine">True for multi-line values, false for single-line values, null for the report setting</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder MultiLine(bool? multiLine = true)
        {
            column.Style.MultiLine = multiLine;
            return this;
        }

        /// <summary>
        /// The text printed when the value is null, for example "-" or "n/a".
        /// When it is not set <see cref="PlainTextReportOptions.NullText"/> is used.
        /// </summary>
        /// <param name="nullText">The text; null for the report setting</param>
        /// <returns>The builder</returns>
        public PlainTextReportColumnBuilder NullText(string nullText)
        {
            column.Style.NullText = nullText;
            return this;
        }

        /// <summary>
        /// Sets the style of the column directly, like the style lambda of the AddColumn overloads
        /// </summary>
        /// <param name="style">Sets the style: caption, width, alignment, multi-line, null text</param>
        /// <returns>The builder</returns>
        /// <exception cref="ArgumentNullException">The action is null</exception>
        public PlainTextReportColumnBuilder Style(Action<PlainTextReportColumnStyle> style)
        {
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }
            style(column.Style);
            return this;
        }
    }
}
