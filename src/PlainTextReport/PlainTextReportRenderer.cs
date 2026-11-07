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
    using System.Numerics;
    using System.Text;

    /// <summary>
    /// Renders the report as lines of text.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class PlainTextReportRenderer<T>
    {
        private const char Ellipsis = '…';
        private static readonly char[] LayoutBreakers = new[] { '\r', '\n', '\t' };

        private readonly PlainTextReportOptions<T> options;
        private readonly PlainTextReportColumn[] columns;
        private readonly Func<T, object>[] getters;

        /// <summary>
        /// Create a renderer for the given options
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReportRenderer(PlainTextReportOptions<T> options)
        {
            this.options = options;
            this.columns = options.Columns.OrderBy(c => c.ColumnIndex).ToArray();
            this.getters = columns.Select(GetGetter).ToArray();
        }

        /// <summary>
        /// Renders the values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public string[] Render(IEnumerable<T> values)
        {
            var rows = (values ?? Enumerable.Empty<T>()).Select(GetCells).ToList();
            var widths = GetColumnWidths(rows);
            var separator = options.ShowColumnSeparator ? $" {options.ColumnSeparator} " : " ";
            var tableWidth = columns.Length == 0 ? 0 : widths.Sum() + separator.Length * (columns.Length - 1);

            var lines = new List<string>(rows.Count + 5);
            var builder = new StringBuilder(tableWidth);

            AddTitle(lines, tableWidth);

            if (columns.Length == 0)
            {
                return lines.ToArray();
            }

            if (options.ShowHeader)
            {
                builder.Clear();
                for (int i = 0; i < columns.Length; i++)
                {
                    if (i > 0) builder.Append(separator);
                    AppendCell(builder, columns[i].ColumnName ?? string.Empty, widths[i], GetHeaderAlignment(columns[i]));
                }
                lines.Add(builder.ToString().TrimEnd());

                if (options.ShowHeaderSeparator)
                {
                    var join = options.ShowColumnSeparator ? $"{options.HeaderSeparator}+{options.HeaderSeparator}" : " ";
                    lines.Add(string.Join(join, widths.Select(w => new string(options.HeaderSeparator, w))));
                }
            }

            var alignments = columns.Select(GetAlignment).ToArray();
            foreach (var cells in rows)
            {
                builder.Clear();
                for (int i = 0; i < cells.Length; i++)
                {
                    if (i > 0) builder.Append(separator);
                    AppendCell(builder, cells[i], widths[i], alignments[i]);
                }
                lines.Add(builder.ToString().TrimEnd());
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Adds the title lines, centered on the report width
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="tableWidth"></param>
        private void AddTitle(List<string> lines, int tableWidth)
        {
            if (!options.ShowTitle)
            {
                return;
            }

            var titleLines = options.IsMultiLineTitle
                ? options.MultiLineTitle
                : string.IsNullOrEmpty(options.Title) ? new string[0] : options.Title.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (titleLines.Length == 0)
            {
                return;
            }

            var width = tableWidth > 0 ? tableWidth : options.Width;
            foreach (var titleLine in titleLines)
            {
                var text = Clean(titleLine);
                if (options.Width > 0 && text.Length > options.Width)
                {
                    text = text.Substring(0, options.Width);
                }
                var padding = Math.Max(0, (width - text.Length) / 2);
                lines.Add((new string(' ', padding) + text).TrimEnd());
            }
            lines.Add(string.Empty);
        }

        /// <summary>
        /// Returns the cells of a row
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string[] GetCells(T model)
        {
            var cells = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var value = model == null ? null : getters[i](model);
                cells[i] = Clean(column.Converter.Convert(value, null, column.ColumnFormatProvider));
            }
            return cells;
        }

        /// <summary>
        /// Returns the width of every column, fitted in the report width
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        private int[] GetColumnWidths(List<string[]> rows)
        {
            var widths = new int[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Style.Width is int fixedWidth)
                {
                    widths[i] = Math.Max(1, fixedWidth);
                    continue;
                }

                var width = options.ShowHeader ? (columns[i].ColumnName ?? string.Empty).Length : 0;
                foreach (var cells in rows)
                {
                    width = Math.Max(width, cells[i].Length);
                }
                widths[i] = Math.Max(1, width);
            }

            if (options.Width > 0 && columns.Length > 0)
            {
                var separatorLength = options.ShowColumnSeparator ? 3 : 1;
                var excess = widths.Sum() + separatorLength * (columns.Length - 1) - options.Width;
                // shrink the automatic text columns first, then the automatic numeric ones, then the fixed ones
                for (int tier = 0; tier < 3 && excess > 0; tier++)
                {
                    int index;
                    while (excess > 0 && (index = GetWidestColumn(widths, tier)) >= 0)
                    {
                        widths[index]--;
                        excess--;
                    }
                }
            }

            return widths;
        }

        /// <summary>
        /// Returns the index of the widest column of the tier that can be shrunk, or -1.
        /// Tier 0: automatic text columns; tier 1: automatic columns; tier 2: all columns.
        /// </summary>
        private int GetWidestColumn(int[] widths, int tier)
        {
            var index = -1;
            for (int i = 0; i < widths.Length; i++)
            {
                if (widths[i] <= 1) continue;
                if (tier < 2 && columns[i].Style.Width.HasValue) continue;
                if (tier < 1 && IsNumeric(columns[i].ColumnType)) continue;
                if (index < 0 || widths[i] > widths[index]) index = i;
            }
            return index;
        }

        /// <summary>
        /// Appends a cell, aligned to the width. A truncated text ends with an ellipsis.
        /// </summary>
        private static void AppendCell(StringBuilder builder, string text, int width, PlainTextReportAlignment alignment)
        {
            if (text.Length == width)
            {
                builder.Append(text);
                return;
            }

            if (text.Length > width)
            {
                if (width == 1)
                {
                    builder.Append(Ellipsis);
                }
                else
                {
                    builder.Append(text, 0, width - 1).Append(Ellipsis);
                }
                return;
            }

            var padding = width - text.Length;
            switch (alignment)
            {
                case PlainTextReportAlignment.Right:
                    builder.Append(' ', padding).Append(text);
                    break;
                case PlainTextReportAlignment.Center:
                    builder.Append(' ', padding / 2).Append(text).Append(' ', padding - padding / 2);
                    break;
                default:
                    builder.Append(text).Append(' ', padding);
                    break;
            }
        }

        /// <summary>
        /// Removes line breaks and tabs, which would break the layout
        /// </summary>
        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            if (text.IndexOfAny(LayoutBreakers) < 0)
            {
                return text;
            }
            return text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        }

        /// <summary>
        /// Returns the alignment of the values
        /// </summary>
        private static PlainTextReportAlignment GetAlignment(PlainTextReportColumn column)
        {
            if (column.Style.Alignment is PlainTextReportAlignment alignment)
            {
                return alignment;
            }
            return IsNumeric(column.ColumnType) ? PlainTextReportAlignment.Right : PlainTextReportAlignment.Left;
        }

        /// <summary>
        /// Returns the alignment of the header
        /// </summary>
        private static PlainTextReportAlignment GetHeaderAlignment(PlainTextReportColumn column)
        {
            return column.Style.HeaderAlignment ?? GetAlignment(column);
        }

        /// <summary>
        /// Returns true if the type is numeric
        /// </summary>
        private static bool IsNumeric(Type type)
        {
            if (type == null)
            {
                return false;
            }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
            {
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return type == typeof(BigInteger);
            }
        }

        /// <summary>
        /// Returns the getter of the column's property
        /// </summary>
        private Func<T, object> GetGetter(PlainTextReportColumn column)
        {
            if (column.ColumnNameInternal != null
                && options.FasterProperties.TryGetValue(column.ColumnNameInternal, out var property)
                && property.Getter != null)
            {
                return property.Getter;
            }
            throw new InvalidOperationException($"The property '{column.ColumnNameInternal}' of '{typeof(T).Name}' cannot be read.");
        }
    }
}
