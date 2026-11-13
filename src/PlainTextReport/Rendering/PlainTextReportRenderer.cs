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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Renders the report as pages of text.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class PlainTextReportRenderer<T>
    {
        private const char Ellipsis = '…';
        private const string DefaultDateFormat = "yyyy-MM-dd";
        private static readonly char[] LayoutBreakers = new[] { '\r', '\n', '\t' };
        private static readonly string[] NewLines = new[] { "\r\n", "\n" };
        private static readonly Regex Placeholder = new Regex(@"\{(page|pages|title|date)(?::([^}]*))?\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly PlainTextReportOptions<T> options;
        private readonly PlainTextReportColumn[] columns;
        private readonly Func<T, object>[] getters;
        private readonly bool[] multiLine;
        private readonly IFormatProvider[] formatProviders;
        private readonly string[] nullTexts;
        private readonly CultureInfo culture;
        private readonly string newLine;

        /// <summary>
        /// Create a renderer for the given options
        /// </summary>
        /// <param name="options"></param>
        public PlainTextReportRenderer(PlainTextReportOptions<T> options)
        {
            this.options = options;
            // OrderBy is a stable sort: the columns with the same index keep the order they were added in
            this.columns = options.Columns.OrderBy(c => c.ColumnIndex).ToArray();
            this.getters = columns.Select(GetGetter).ToArray();
            this.culture = options.Culture ?? CultureInfo.InvariantCulture;
            this.newLine = options.GetNewLine();
            this.multiLine = columns.Select(c => c.Style.MultiLine ?? options.MultiLineRows).ToArray();
            this.formatProviders = columns.Select(c => c.GetFormatProvider(culture)).ToArray();
            this.nullTexts = columns.Select(c => c.Style.NullText ?? options.NullText).ToArray();
        }

        /// <summary>
        /// Renders the values as lines
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public string[] Render(IEnumerable<T> values)
        {
            return RenderPages(values).SelectMany(page => page.Lines).ToArray();
        }

        /// <summary>
        /// Renders the values as pages. Without page settings the report is a single page.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public PlainTextReportPage[] RenderPages(IEnumerable<T> values)
        {
            return RenderPages(values, GetDate(options));
        }

        /// <summary>
        /// Renders the values as pages, with the given date for the {date} placeholder
        /// </summary>
        /// <param name="values"></param>
        /// <param name="date">The date of the report: read it once with <see cref="GetDate"/> and use it everywhere</param>
        /// <returns></returns>
        public PlainTextReportPage[] RenderPages(IEnumerable<T> values, DateTime date)
        {
            var page = options.Page;
            var maxWidth = options.Width;
            if (page != null)
            {
                ValidatePage(page);
                maxWidth = maxWidth > 0 ? Math.Min(maxWidth, page.PrintableWidth) : page.PrintableWidth;
            }

            var rows = (values ?? Enumerable.Empty<T>()).Select(GetCells).ToList();
            var widths = GetColumnWidths(rows, maxWidth);
            var separator = options.ShowColumnSeparator ? $" {options.ColumnSeparator} " : " ";
            var tableWidth = columns.Length == 0 ? 0 : widths.Sum() + separator.Length * (columns.Length - 1);

            var title = GetTitleLines(tableWidth, maxWidth);
            var columnHeader = GetColumnHeaderLines(widths, separator);
            var body = GetRowLines(rows, widths, separator);

            if (page == null)
            {
                var lines = new List<string>(title.Count + columnHeader.Count + body.Count);
                lines.AddRange(title);
                lines.AddRange(columnHeader);
                lines.AddRange(body.SelectMany(row => row));
                return new[] { new PlainTextReportPage(1, 1, lines.ToArray(), newLine) };
            }

            return Paginate(page, title, columnHeader, body, date);
        }

        /// <summary>
        /// Returns the date of the report: <see cref="PlainTextReportPageSettings.Date"/>, or the current date and time
        /// </summary>
        internal static DateTime GetDate(PlainTextReportOptions options)
        {
            return options.Page?.Date ?? DateTime.Now;
        }

        #region Table

        /// <summary>
        /// Returns the title lines, centered on the report width, followed by an empty line
        /// </summary>
        private List<string> GetTitleLines(int tableWidth, int maxWidth)
        {
            var lines = new List<string>();
            if (!options.ShowTitle)
            {
                return lines;
            }

            var titleLines = options.IsMultiLineTitle
                ? options.MultiLineTitle
                : string.IsNullOrEmpty(options.Title) ? new string[0] : options.Title.Split(NewLines, StringSplitOptions.None);

            if (titleLines.Length == 0)
            {
                return lines;
            }

            var width = tableWidth > 0 ? tableWidth : maxWidth;
            foreach (var titleLine in titleLines)
            {
                var text = Clean(titleLine);
                if (maxWidth > 0 && text.Length > maxWidth)
                {
                    text = text.Substring(0, maxWidth);
                }
                var padding = Math.Max(0, (width - text.Length) / 2);
                lines.Add((new string(' ', padding) + text).TrimEnd());
            }
            lines.Add(string.Empty);
            return lines;
        }

        /// <summary>
        /// Returns the column header and its separator line
        /// </summary>
        private List<string> GetColumnHeaderLines(int[] widths, string separator)
        {
            var lines = new List<string>(2);
            if (columns.Length == 0 || !options.ShowHeader)
            {
                return lines;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) builder.Append(separator);
                AppendCell(builder, Clean(columns[i].Caption), widths[i], GetHeaderAlignment(columns[i]));
            }
            lines.Add(builder.ToString().TrimEnd());

            if (options.ShowHeaderSeparator)
            {
                var join = options.ShowColumnSeparator ? $"{options.HeaderSeparator}+{options.HeaderSeparator}" : " ";
                lines.Add(string.Join(join, widths.Select(w => new string(options.HeaderSeparator, w))));
            }
            return lines;
        }

        /// <summary>
        /// Returns the lines of every row: one line, or more when a multi-line value wraps
        /// </summary>
        private List<string[]> GetRowLines(List<string[]> rows, int[] widths, string separator)
        {
            var result = new List<string[]>(rows.Count);
            if (columns.Length == 0)
            {
                return result;
            }

            var alignments = columns.Select(GetAlignment).ToArray();
            var builder = new StringBuilder();
            var cellLines = new string[columns.Length][];
            foreach (var cells in rows)
            {
                var height = 1;
                for (int i = 0; i < cells.Length; i++)
                {
                    cellLines[i] = multiLine[i] ? Wrap(cells[i], widths[i]) : new[] { cells[i] };
                    height = Math.Max(height, cellLines[i].Length);
                }

                var lines = new string[height];
                for (int k = 0; k < height; k++)
                {
                    builder.Clear();
                    for (int i = 0; i < cells.Length; i++)
                    {
                        if (i > 0) builder.Append(separator);
                        AppendCell(builder, k < cellLines[i].Length ? cellLines[i][k] : string.Empty, widths[i], alignments[i]);
                    }
                    lines[k] = builder.ToString().TrimEnd();
                }
                result.Add(lines);
            }
            return result;
        }

        /// <summary>
        /// Returns the cells of a row. The multi-line cells keep their line breaks, as '\n'.
        /// </summary>
        private string[] GetCells(T model)
        {
            var cells = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var value = model == null ? null : getters[i](model);
                // a null value is the null text, unless a custom converter prints it; a null converted text is the null text too
                var text = value == null && column.HasDefaultConverter
                    ? null
                    : column.Converter.Convert(value, null, formatProviders[i]);
                text = text ?? nullTexts[i];
                cells[i] = multiLine[i] ? NormalizeLineBreaks(text) : Clean(text);
            }
            return cells;
        }

        /// <summary>
        /// Returns the width a cell needs: its longest line
        /// </summary>
        private int GetCellWidth(string cell, int column)
        {
            if (!multiLine[column] || cell.IndexOf('\n') < 0)
            {
                return cell.Length;
            }
            return cell.Split('\n').Max(line => line.Length);
        }

        /// <summary>
        /// Wraps a multi-line cell in lines of the given width: the line breaks start a new line,
        /// the words are not split unless they are longer than the width.
        /// </summary>
        internal static string[] Wrap(string text, int width)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                var line = paragraph.TrimEnd();
                while (line.Length > width)
                {
                    // break at the last space that fits, otherwise split the word
                    var cut = line.LastIndexOf(' ', width);
                    if (cut <= 0)
                    {
                        lines.Add(line.Substring(0, width));
                        line = line.Substring(width);
                    }
                    else
                    {
                        lines.Add(line.Substring(0, cut).TrimEnd());
                        line = line.Substring(cut + 1);
                    }
                    line = line.TrimStart(' ');
                }
                lines.Add(line);
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Returns the width of every column, fitted in the maximum width
        /// </summary>
        private int[] GetColumnWidths(List<string[]> rows, int maxWidth)
        {
            var widths = new int[columns.Length];
            var percents = new double[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Style.Width is ColumnWidth columnWidth)
                {
                    if (!columnWidth.IsPercent)
                    {
                        widths[i] = Math.Max(1, (int)columnWidth.Value);
                        continue;
                    }
                    if (maxWidth > 0)
                    {
                        // computed below, from the free space
                        percents[i] = columnWidth.Value;
                        continue;
                    }
                    // without a report width a percentage column is automatic
                }

                var width = options.ShowHeader ? Clean(columns[i].Caption).Length : 0;
                foreach (var cells in rows)
                {
                    width = Math.Max(width, GetCellWidth(cells[i], i));
                }
                widths[i] = Math.Max(1, width);
            }

            if (maxWidth > 0 && columns.Length > 0)
            {
                var separatorLength = options.ShowColumnSeparator ? 3 : 1;
                SetPercentWidths(widths, percents, maxWidth - separatorLength * (columns.Length - 1));
                var excess = widths.Sum() + separatorLength * (columns.Length - 1) - maxWidth;

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
        /// Gives the percentage columns their share of the space left by the other columns.
        /// When the percentages add up to 100% or more, the whole free space is used:
        /// two columns at 100% get half of it each.
        /// </summary>
        /// <param name="widths">The widths; the percentage columns are still 0</param>
        /// <param name="percents">The percentage of every column, 0 for the others</param>
        /// <param name="contentWidth">The report width without the column separators</param>
        private static void SetPercentWidths(int[] widths, double[] percents, int contentWidth)
        {
            var totalPercent = percents.Sum();
            if (totalPercent <= 0)
            {
                return;
            }

            var free = Math.Max(0, contentWidth - widths.Sum());
            var scale = Math.Max(100, totalPercent);
            var exact = percents.Select(p => free * p / scale).ToArray();

            for (int i = 0; i < widths.Length; i++)
            {
                if (percents[i] > 0) widths[i] = (int)Math.Floor(exact[i]);
            }

            // when the whole free space is used, the characters lost by the rounding go to the largest remainders
            if (totalPercent >= 100)
            {
                var left = free - Enumerable.Range(0, widths.Length).Where(i => percents[i] > 0).Sum(i => widths[i]);
                var order = Enumerable.Range(0, widths.Length)
                    .Where(i => percents[i] > 0)
                    .OrderByDescending(i => exact[i] - Math.Floor(exact[i]))
                    .ThenBy(i => i)
                    .ToArray();
                for (int k = 0; k < left && order.Length > 0; k++)
                {
                    widths[order[k % order.Length]]++;
                }
            }

            for (int i = 0; i < widths.Length; i++)
            {
                if (percents[i] > 0 && widths[i] < 1) widths[i] = 1;
            }
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

        #endregion

        #region Pages

        /// <summary>
        /// Checks that the page settings can hold a report
        /// </summary>
        private static void ValidatePage(PlainTextReportPageSettings page)
        {
            if (page.PaperSize == null) throw new InvalidOperationException("The page settings must have a paper size.");
            if (page.CharactersPerInch <= 0) throw new InvalidOperationException("CharactersPerInch must be greater than zero.");
            if (page.LinesPerInch <= 0) throw new InvalidOperationException("LinesPerInch must be greater than zero.");
            if (page.MarginLeft < 0 || page.MarginRight < 0 || page.MarginTop < 0 || page.MarginBottom < 0)
            {
                throw new InvalidOperationException("The page margins cannot be negative.");
            }
            if (page.PrintableWidth < 1 || page.PrintableHeight < 1)
            {
                throw new InvalidOperationException($"The page {page.PaperSize} has no printable area with these margins.");
            }
        }

        /// <summary>
        /// Splits the report in pages, each one with header, body and footer and exactly the page height
        /// </summary>
        private PlainTextReportPage[] Paginate(PlainTextReportPageSettings page, List<string> title, List<string> columnHeader, List<string[]> rows, DateTime date)
        {
            var header = page.Header ?? new PlainTextReportPageBand();
            var footer = page.Footer ?? new PlainTextReportPageBand();
            var capacity = page.PrintableHeight - GetBandHeight(header) - GetBandHeight(footer);
            if (capacity < 1)
            {
                throw new InvalidOperationException($"The page {page.PaperSize} has no room for the body: reduce the header, the footer or the margins.");
            }

            // split the body in pages; the lines of a multi-line row stay on the same page,
            // unless the row is longer than a whole page
            var bodies = new List<List<string>>();
            var nextPageCapacity = capacity - (page.RepeatColumnHeader ? columnHeader.Count : 0);
            var row = 0;
            var rowLine = 0;
            do
            {
                var first = bodies.Count == 0;
                var lines = new List<string>(capacity);
                if (first) lines.AddRange(title);
                if (first || page.RepeatColumnHeader) lines.AddRange(columnHeader);

                var start = lines.Count;
                if (start > capacity || (!first && start == capacity && row < rows.Count))
                {
                    throw new InvalidOperationException($"The page {page.PaperSize} is too small for the title and the column header.");
                }

                while (row < rows.Count && lines.Count < capacity)
                {
                    var left = rows[row].Length - rowLine;
                    var free = capacity - lines.Count;
                    if (left > free && lines.Count > start && rowLine == 0 && rows[row].Length <= nextPageCapacity)
                    {
                        // the row fits on the next page
                        break;
                    }

                    var take = Math.Min(left, free);
                    for (int k = 0; k < take; k++)
                    {
                        lines.Add(rows[row][rowLine++]);
                    }
                    if (rowLine == rows[row].Length)
                    {
                        row++;
                        rowLine = 0;
                    }
                }
                bodies.Add(lines);
            }
            while (row < rows.Count);

            // compose the pages
            var count = bodies.Count;
            var titleText = options.IsMultiLineTitle ? string.Join(" ", options.MultiLineTitle) : options.Title ?? string.Empty;
            var margin = new string(' ', page.MarginLeft);
            var pages = new PlainTextReportPage[count];

            for (int i = 0; i < count; i++)
            {
                var number = i + 1;
                var lines = new List<string>(page.Height);

                lines.AddRange(Enumerable.Repeat(string.Empty, page.MarginTop));

                if (!header.IsEmpty)
                {
                    lines.AddRange(GetBandLines(header, page.PrintableWidth, number, count, date, titleText, culture));
                    if (header.ShowSeparator) lines.Add(new string(header.SeparatorChar, page.PrintableWidth));
                }

                lines.AddRange(bodies[i]);
                lines.AddRange(Enumerable.Repeat(string.Empty, capacity - bodies[i].Count));

                if (!footer.IsEmpty)
                {
                    if (footer.ShowSeparator) lines.Add(new string(footer.SeparatorChar, page.PrintableWidth));
                    lines.AddRange(GetBandLines(footer, page.PrintableWidth, number, count, date, titleText, culture));
                }

                lines.AddRange(Enumerable.Repeat(string.Empty, page.MarginBottom));

                var pageLines = margin.Length == 0
                    ? lines.ToArray()
                    : lines.Select(line => line.Length == 0 ? line : margin + line).ToArray();
                pages[i] = new PlainTextReportPage(number, count, pageLines, newLine);
            }

            return pages;
        }

        /// <summary>
        /// Returns the number of lines of a band, separator included
        /// </summary>
        private static int GetBandHeight(PlainTextReportPageBand band)
        {
            if (band.IsEmpty)
            {
                return 0;
            }
            var lines = Math.Max(SplitLines(band.Left).Length, Math.Max(SplitLines(band.Center).Length, SplitLines(band.Right).Length));
            return lines + (band.ShowSeparator ? 1 : 0);
        }

        /// <summary>
        /// Returns the lines of a band, with the placeholders replaced
        /// </summary>
        private static List<string> GetBandLines(PlainTextReportPageBand band, int width, int number, int count, DateTime date, string title, CultureInfo culture)
        {
            var left = SplitLines(band.Left);
            var center = SplitLines(band.Center);
            var right = SplitLines(band.Right);
            var height = Math.Max(left.Length, Math.Max(center.Length, right.Length));

            var lines = new List<string>(height);
            for (int i = 0; i < height; i++)
            {
                var buffer = Enumerable.Repeat(' ', width).ToArray();
                var c = Format(i < center.Length ? center[i] : null, number, count, date, title, culture);
                var l = Format(i < left.Length ? left[i] : null, number, count, date, title, culture);
                var r = Format(i < right.Length ? right[i] : null, number, count, date, title, culture);
                Put(buffer, c, (width - Math.Min(c.Length, width)) / 2);
                Put(buffer, l, 0);
                Put(buffer, r, width - Math.Min(r.Length, width));
                lines.Add(new string(buffer).TrimEnd());
            }
            return lines;
        }

        /// <summary>
        /// Writes the text in the buffer, from start, cutting what exceeds
        /// </summary>
        private static void Put(char[] buffer, string text, int start)
        {
            for (int i = 0; i < text.Length && start + i < buffer.Length; i++)
            {
                buffer[start + i] = text[i];
            }
        }

        /// <summary>
        /// Splits a band text in lines
        /// </summary>
        private static string[] SplitLines(string text)
        {
            return string.IsNullOrEmpty(text) ? new string[0] : text.Split(NewLines, StringSplitOptions.None);
        }

        /// <summary>
        /// Replaces the placeholders {page}, {pages}, {title} and {date}, with an optional format
        /// </summary>
        internal static string Format(string text, int number, int count, DateTime date, string title, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var result = Placeholder.Replace(text, match =>
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value : null;
                switch (match.Groups[1].Value.ToLowerInvariant())
                {
                    case "page": return number.ToString(format, culture);
                    case "pages": return count.ToString(format, culture);
                    case "date": return date.ToString(string.IsNullOrEmpty(format) ? DefaultDateFormat : format, culture);
                    default: return title;
                }
            });
            return Clean(result);
        }

        #endregion

        #region Helpers

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
        /// Turns every line break in '\n' and the tabs in spaces
        /// </summary>
        private static string NormalizeLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            if (text.IndexOfAny(LayoutBreakers) < 0)
            {
                return text;
            }
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ');
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
        private static Func<T, object> GetGetter(PlainTextReportColumn column)
        {
            if (column.Getter is Func<T, object> getter)
            {
                return getter;
            }
            throw new InvalidOperationException($"The column '{column.ColumnName}' cannot read the values of '{typeof(T).Name}'.");
        }

        #endregion
    }
}
