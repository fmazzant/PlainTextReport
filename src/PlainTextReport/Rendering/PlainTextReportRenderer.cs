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
        /// <summary>
        /// The default truncation marker, the ellipsis
        /// </summary>
        internal const string DefaultTruncationMarker = "…";
        private const string DefaultDateFormat = "yyyy-MM-dd";
        private static readonly char[] LayoutBreakers = new[] { '\r', '\n', '\t' };
        private static readonly string[] NewLines = new[] { "\r\n", "\n" };
        private static readonly Regex Placeholder = new Regex(@"\{(page|pages|title|date)(?::([^}]*))?\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex KeyPlaceholder = new Regex(@"\{key(?::([^}]*))?\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly PlainTextReportOptions<T> options;
        private readonly PlainTextReportColumn[] columns;
        private readonly Func<T, object>[] getters;
        private readonly bool[] multiLine;
        private readonly IFormatProvider[] formatProviders;
        private readonly string[] nullTexts;
        private readonly CultureInfo culture;
        private readonly string newLine;
        private readonly string truncationMarker;
        private readonly TextWidth textWidth;
        private readonly PlainTextReportGroup group;
        private readonly Func<T, object> groupKey;

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
            this.truncationMarker = Clean(options.TruncationMarker ?? DefaultTruncationMarker);
            this.textWidth = options.UnicodeWidth ? TextWidth.Unicode : TextWidth.Simple;
            this.multiLine = columns.Select(c => c.Style.MultiLine ?? options.MultiLineRows).ToArray();
            this.formatProviders = columns.Select(c => c.GetFormatProvider(culture)).ToArray();
            this.nullTexts = columns.Select(c => c.Style.NullText ?? options.NullText).ToArray();
            this.groupKey = options.GroupKey;
            this.group = groupKey == null ? null : options.Group ?? new PlainTextReportGroup();
            if (options.ShowTotals || (group != null && group.ShowSubtotals))
            {
                foreach (var column in columns)
                {
                    ValidateTotal(column);
                }
            }
        }

        /// <summary>
        /// Checks that the aggregate of the column can be computed on its type
        /// </summary>
        private static void ValidateTotal(PlainTextReportColumn column)
        {
            var total = column.Style.Total;
            var type = Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType;
            if ((total == PlainTextReportTotal.Sum || total == PlainTextReportTotal.Average) && !IsNumeric(type))
            {
                throw new InvalidOperationException($"The total {total} of column '{column.ColumnName}' needs a numeric column, not {type.Name}.");
            }
            if ((total == PlainTextReportTotal.Min || total == PlainTextReportTotal.Max) && !typeof(IComparable).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"The total {total} of column '{column.ColumnName}' needs values that can be compared, not {type.Name}.");
            }
            if (!Enum.IsDefined(typeof(PlainTextReportTotal), total))
            {
                throw new InvalidOperationException($"The total {total} of column '{column.ColumnName}' is not valid.");
            }
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
            var layout = GetLayout(values);
            if (layout.Page == null)
            {
                return new[] { new PlainTextReportPage(1, 1, GetLines(layout).ToArray(), newLine) };
            }

            var pages = Paginate(layout, date).ToList();
            return pages.Select((lines, i) => new PlainTextReportPage(i + 1, pages.Count, lines, newLine)).ToArray();
        }

        /// <summary>
        /// Renders the values as the text of the whole report, a piece at a time: the lines, the line breaks
        /// and the page breaks, followed by a final line break. The text is the same as the pages joined by
        /// the line break and the page break. Only the cells needed to compute the column widths or the
        /// number of pages are kept in memory: when every column has a fixed width and the page bands don't
        /// use {pages}, the values are read one at a time while the text is written.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public IEnumerable<string> RenderText(IEnumerable<T> values)
        {
            var layout = GetLayout(values);
            var first = true;
            if (layout.Page == null)
            {
                foreach (var line in GetLines(layout))
                {
                    if (!first) yield return newLine;
                    first = false;
                    yield return line;
                }
            }
            else
            {
                var pageBreak = layout.Page.PageBreak ?? string.Empty;
                foreach (var page in Paginate(layout, GetDate(options)))
                {
                    for (int k = 0; k < page.Length; k++)
                    {
                        if (!first)
                        {
                            yield return newLine;
                            if (k == 0 && pageBreak.Length > 0) yield return pageBreak;
                        }
                        first = false;
                        yield return page[k];
                    }
                }
            }
            yield return newLine;
        }

        /// <summary>
        /// The layout of the report: the column widths, the title and the column header, and the cells of the rows
        /// </summary>
        private sealed class Layout
        {
            public PlainTextReportPageSettings Page;
            public int[] Widths;
            public string Separator;
            public List<string> Title;
            public List<string> ColumnHeader;

            /// <summary>
            /// The rows, with the group headers and the subtotals: a list when they are needed more than once,
            /// otherwise read while the report is written
            /// </summary>
            public IEnumerable<RowItem> Rows;

            /// <summary>
            /// The aggregates of the columns, computed while the rows are read; null without the totals row
            /// </summary>
            public ColumnTotal[] Totals;
        }

        /// <summary>
        /// The kind of a <see cref="RowItem"/>
        /// </summary>
        private enum RowKind
        {
            Values,
            GroupHeader,
            Subtotals,
        }

        /// <summary>
        /// An item of the body: a row of values, the header of a group or the subtotals row of a group
        /// </summary>
        private sealed class RowItem
        {
            public RowKind Kind;

            /// <summary>
            /// The cells of a row of values or of a subtotals row
            /// </summary>
            public string[] Cells;

            /// <summary>
            /// The text of a group header
            /// </summary>
            public string Text;

            /// <summary>
            /// True for the header of the first group
            /// </summary>
            public bool FirstGroup;
        }

        /// <summary>
        /// The lines that stay together on a page: a row, a group header with the first row of its group,
        /// a subtotals row or the totals row
        /// </summary>
        private sealed class Block
        {
            public string[] Lines;

            /// <summary>
            /// An empty line before the block, dropped at the top of a page
            /// </summary>
            public bool Gap;

            /// <summary>
            /// The block starts a new page
            /// </summary>
            public bool NewPage;
        }

        /// <summary>
        /// Validates the settings and computes the layout. The values are read now only when the widths
        /// or the number of pages depend on them; otherwise they are read while the rows are rendered.
        /// </summary>
        private Layout GetLayout(IEnumerable<T> values)
        {
            var page = options.Page;
            var maxWidth = options.Width;
            if (page != null)
            {
                ValidatePage(page);
                maxWidth = maxWidth > 0 ? Math.Min(maxWidth, page.PrintableWidth) : page.PrintableWidth;
            }

            var totals = options.ShowTotals ? CreateTotals() : null;
            IEnumerable<RowItem> rows = GetRowItems(values ?? Enumerable.Empty<T>(), totals, maxWidth);
            if (columns.Any(column => NeedsValues(column, maxWidth)) || (page != null && UsesPageCount(page)))
            {
                rows = rows.ToList();
            }

            var widths = GetColumnWidths(rows, maxWidth, totals);
            var separator = options.ShowColumnSeparator ? $" {options.ColumnSeparator} " : " ";
            var tableWidth = columns.Length == 0 ? 0 : widths.Sum() + separator.Length * (columns.Length - 1);

            return new Layout
            {
                Page = page,
                Widths = widths,
                Separator = separator,
                Title = GetTitleLines(tableWidth, maxWidth),
                ColumnHeader = GetColumnHeaderLines(widths, separator),
                Rows = rows,
                Totals = totals,
            };
        }

        /// <summary>
        /// Returns true when the width of the column depends on its values
        /// </summary>
        private static bool NeedsValues(PlainTextReportColumn column, int maxWidth)
        {
            // a percentage column without a report width is automatic
            return !(column.Style.Width is ColumnWidth width) || (width.IsPercent && maxWidth <= 0);
        }

        /// <summary>
        /// Returns true when the header or the footer prints the number of pages
        /// </summary>
        private static bool UsesPageCount(PlainTextReportPageSettings page)
        {
            return new[] { page.Header, page.Footer }
                .Where(band => band != null)
                .SelectMany(band => new[] { band.Left, band.Center, band.Right })
                .Any(text => !string.IsNullOrEmpty(text) && Placeholder.Matches(text).Cast<Match>()
                    .Any(match => string.Equals(match.Groups[1].Value, "pages", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Returns the lines of the report without pages
        /// </summary>
        private IEnumerable<string> GetLines(Layout layout)
        {
            foreach (var line in layout.Title) yield return line;
            foreach (var line in layout.ColumnHeader) yield return line;
            foreach (var block in GetBlocks(layout, false))
            {
                if (block.Gap) yield return string.Empty;
                foreach (var line in block.Lines) yield return line;
            }
        }

        /// <summary>
        /// Returns the aggregates of the columns, empty; null without columns
        /// </summary>
        private ColumnTotal[] CreateTotals()
        {
            return columns.Length > 0
                ? columns.Select(column => new ColumnTotal(column.Style.Total, column.ColumnType)).ToArray()
                : null;
        }

        /// <summary>
        /// Returns the items of the body while the values are read: the rows and, when the report is grouped,
        /// the header of every group and its subtotals row. A group starts every time the key changes.
        /// </summary>
        /// <param name="values">The items</param>
        /// <param name="totals">The aggregates of the totals row; null without the totals row</param>
        /// <param name="maxWidth">The report width, 0 for no limit: a longer group header is truncated</param>
        private IEnumerable<RowItem> GetRowItems(IEnumerable<T> values, ColumnTotal[] totals, int maxWidth)
        {
            var index = 0;
            if (group == null)
            {
                foreach (var model in values)
                {
                    yield return new RowItem { Kind = RowKind.Values, Cells = GetCells(model, index++, totals, null) };
                }
                yield break;
            }

            ColumnTotal[] subtotals = null;
            object key = null;
            var first = true;
            foreach (var model in values)
            {
                var next = GetGroupKey(model, index);
                if (first || !Equals(key, next))
                {
                    if (subtotals != null)
                    {
                        yield return new RowItem { Kind = RowKind.Subtotals, Cells = GetTotalsCells(subtotals, null) };
                    }
                    yield return new RowItem { Kind = RowKind.GroupHeader, Text = GetGroupHeader(next, maxWidth), FirstGroup = first };
                    subtotals = group.ShowSubtotals ? CreateTotals() : null;
                    key = next;
                    first = false;
                }
                yield return new RowItem { Kind = RowKind.Values, Cells = GetCells(model, index++, totals, subtotals) };
            }
            if (subtotals != null)
            {
                yield return new RowItem { Kind = RowKind.Subtotals, Cells = GetTotalsCells(subtotals, null) };
            }
        }

        /// <summary>
        /// Returns the key of the group of an item; a key selector that throws gives a <see cref="PlainTextReportException"/> with the row
        /// </summary>
        private object GetGroupKey(T model, int index)
        {
            try
            {
                return model == null ? null : groupKey(model);
            }
            catch (Exception ex)
            {
                throw new PlainTextReportException(
                    $"Cannot read the group key at row {index + 1}: {ex.Message}",
                    index + 1, null, null, ex);
            }
        }

        /// <summary>
        /// Returns the header of a group: <see cref="PlainTextReportGroup.Header"/> with the key in place of {key} or {key:format},
        /// on one line and truncated to the report width
        /// </summary>
        private string GetGroupHeader(object key, int maxWidth)
        {
            var text = KeyPlaceholder.Replace(group.Header ?? "{key}", match =>
            {
                if (key == null)
                {
                    return options.NullText ?? string.Empty;
                }
                var format = match.Groups[1].Success ? match.Groups[1].Value : null;
                return key is IFormattable formattable
                    ? formattable.ToString(format, culture)
                    : Convert.ToString(key, culture) ?? string.Empty;
            });
            text = Clean(text);
            return maxWidth > 0 ? Truncate(text, maxWidth) : text;
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
                if (maxWidth > 0)
                {
                    text = Truncate(text, maxWidth);
                }
                var padding = Math.Max(0, (width - textWidth.GetWidth(text)) / 2);
                lines.Add((new string(' ', padding) + text).TrimEnd());
            }
            lines.Add(string.Empty);
            return lines;
        }

        /// <summary>
        /// Returns the column header, on one line or more with <see cref="PlainTextReportOptions.MultiLineHeader"/>,
        /// and its separator line
        /// </summary>
        private List<string> GetColumnHeaderLines(int[] widths, string separator)
        {
            var lines = new List<string>(2);
            if (columns.Length == 0 || !options.ShowHeader)
            {
                return lines;
            }

            var captions = new string[columns.Length][];
            var height = 1;
            for (int i = 0; i < columns.Length; i++)
            {
                captions[i] = options.MultiLineHeader
                    ? Wrap(NormalizeLineBreaks(columns[i].Caption), widths[i], textWidth)
                    : new[] { Clean(columns[i].Caption) };
                height = Math.Max(height, captions[i].Length);
            }

            var builder = new StringBuilder();
            for (int k = 0; k < height; k++)
            {
                builder.Clear();
                for (int i = 0; i < columns.Length; i++)
                {
                    if (i > 0) builder.Append(separator);
                    AppendCell(builder, k < captions[i].Length ? captions[i][k] : string.Empty, widths[i], GetHeaderAlignment(columns[i]));
                }
                lines.Add(builder.ToString().TrimEnd());
            }

            if (options.ShowHeaderSeparator)
            {
                lines.Add(GetSeparatorLine(widths));
            }
            return lines;
        }

        /// <summary>
        /// Returns the separator line below the header and above the totals row
        /// </summary>
        private string GetSeparatorLine(int[] widths)
        {
            var join = options.ShowColumnSeparator ? $"{options.HeaderSeparator}+{options.HeaderSeparator}" : " ";
            return string.Join(join, widths.Select(w => new string(options.HeaderSeparator, w)));
        }

        /// <summary>
        /// Returns the blocks of the body, while the rows are read: every row is one line, or more when a multi-line
        /// value wraps; a group header is in the same block of the first row of its group, so it is never left alone
        /// at the bottom of a page. The totals row, with its separator, is the last block, after the values have been read.
        /// </summary>
        /// <param name="layout">The layout</param>
        /// <param name="measure">True to count the lines without composing them: the lines are null</param>
        private IEnumerable<Block> GetBlocks(Layout layout, bool measure)
        {
            if (columns.Length == 0)
            {
                yield break;
            }

            var widths = layout.Widths;
            var separator = layout.Separator;
            var alignments = columns.Select(GetAlignment).ToArray();
            var builder = new StringBuilder();
            var cellLines = new string[columns.Length][];
            RowItem header = null;
            foreach (var item in layout.Rows)
            {
                string[] lines;
                switch (item.Kind)
                {
                    case RowKind.GroupHeader:
                        header = item;
                        continue;
                    case RowKind.Subtotals:
                        lines = measure ? new string[1] : new[] { GetLine(builder, item.Cells, widths, separator, alignments) };
                        break;
                    default:
                        lines = measure
                            ? new string[GetRowHeight(item.Cells, widths)]
                            : GetRowLines(builder, cellLines, item.Cells, widths, separator, alignments);
                        break;
                }

                if (header == null)
                {
                    yield return new Block { Lines = lines };
                    continue;
                }

                var withHeader = new string[lines.Length + 1];
                withHeader[0] = header.Text;
                Array.Copy(lines, 0, withHeader, 1, lines.Length);
                yield return new Block { Lines = withHeader, Gap = !header.FirstGroup, NewPage = !header.FirstGroup && group.NewPage };
                header = null;
            }

            if (layout.Totals != null)
            {
                yield return new Block
                {
                    Lines = measure ? new string[GetTotalsHeight()] : GetTotalsLines(builder, layout.Totals, widths, separator, alignments),
                };
            }
        }

        /// <summary>
        /// Returns the totals row, preceded by the separator line when <see cref="PlainTextReportOptions.ShowHeaderSeparator"/> is true
        /// </summary>
        private string[] GetTotalsLines(StringBuilder builder, ColumnTotal[] totals, int[] widths, string separator, PlainTextReportAlignment[] alignments)
        {
            var row = GetLine(builder, GetTotalsCells(totals, options.TotalsCaption), widths, separator, alignments);
            return options.ShowHeaderSeparator ? new[] { GetSeparatorLine(widths), row } : new[] { row };
        }

        /// <summary>
        /// Returns the line of cells that are on one line, like the totals and the subtotals
        /// </summary>
        private string GetLine(StringBuilder builder, string[] cells, int[] widths, string separator, PlainTextReportAlignment[] alignments)
        {
            builder.Clear();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) builder.Append(separator);
                AppendCell(builder, cells[i], widths[i], alignments[i]);
            }
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Returns the number of lines of the totals row, separator included
        /// </summary>
        private int GetTotalsHeight()
        {
            return options.ShowHeaderSeparator ? 2 : 1;
        }

        /// <summary>
        /// Returns the cells of the totals or of the subtotals row: the aggregates, formatted with the format of the column
        /// in the culture of the report, and the caption in the first column without an aggregate
        /// </summary>
        /// <param name="totals">The aggregates</param>
        /// <param name="caption">The caption; null for none</param>
        private string[] GetTotalsCells(ColumnTotal[] totals, string caption)
        {
            var cells = new string[columns.Length];
            var captionPrinted = false;
            for (int i = 0; i < columns.Length; i++)
            {
                var total = totals[i];
                string text;
                switch (total.Kind)
                {
                    case PlainTextReportTotal.None:
                        text = captionPrinted ? null : caption;
                        captionPrinted = true;
                        break;
                    case PlainTextReportTotal.Count:
                        text = ((long)total.Result).ToString(formatProviders[i] as CultureInfo ?? culture);
                        break;
                    default:
                        text = GetTotalText(i, total);
                        break;
                }
                cells[i] = Clean(text);
            }
            return cells;
        }

        /// <summary>
        /// Returns the text of an aggregate: the minimum and the maximum are converted like the values of the column,
        /// the sum and the average are formatted with the format of the column
        /// </summary>
        private string GetTotalText(int i, ColumnTotal total)
        {
            var column = columns[i];
            object result;
            try
            {
                result = total.Result;
            }
            catch (Exception ex)
            {
                throw new PlainTextReportException(
                    $"Cannot compute the total {total.Kind} of column '{column.ColumnName}': {ex.Message}",
                    0, column.ColumnName, null, ex);
            }

            if (result == null)
            {
                return nullTexts[i];
            }
            if (total.Kind == PlainTextReportTotal.Min || total.Kind == PlainTextReportTotal.Max)
            {
                return column.Converter.Convert(result, null, formatProviders[i]) ?? nullTexts[i];
            }
            return string.Format(formatProviders[i], "{0}", result);
        }

        /// <summary>
        /// Returns the lines of a row: one line, or more when a multi-line value wraps
        /// </summary>
        private string[] GetRowLines(StringBuilder builder, string[][] cellLines, string[] cells, int[] widths, string separator, PlainTextReportAlignment[] alignments)
        {
            var height = 1;
            for (int i = 0; i < cells.Length; i++)
            {
                cellLines[i] = multiLine[i] ? Wrap(cells[i], widths[i], textWidth) : new[] { cells[i] };
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
            return lines;
        }

        /// <summary>
        /// Returns the number of lines of a row, without composing them
        /// </summary>
        private int GetRowHeight(string[] cells, int[] widths)
        {
            var height = 1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (multiLine[i])
                {
                    height = Math.Max(height, Wrap(cells[i], widths[i], textWidth).Length);
                }
            }
            return height;
        }

        /// <summary>
        /// Returns the cells of a row. The multi-line cells keep their line breaks, as '\n'.
        /// A getter or a converter that throws gives a <see cref="PlainTextReportException"/> with the row and the column.
        /// </summary>
        /// <param name="model">The item</param>
        /// <param name="index">The zero-based position of the item in the collection</param>
        /// <param name="totals">The aggregates of the columns, which receive the values; null without the totals row</param>
        /// <param name="subtotals">The aggregates of the group, which receive the values; null without the subtotals</param>
        private string[] GetCells(T model, int index, ColumnTotal[] totals, ColumnTotal[] subtotals)
        {
            var cells = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                object value;
                try
                {
                    value = model == null ? null : getters[i](model);
                }
                catch (Exception ex)
                {
                    throw new PlainTextReportException(
                        $"Cannot read the value of column '{column.ColumnName}' at row {index + 1}: {ex.Message}",
                        index + 1, column.ColumnName, null, ex);
                }

                AddTotal(totals, i, value, index);
                AddTotal(subtotals, i, value, index);

                // a null value is the null text, unless a custom converter prints it; a null converted text is the null text too
                string text;
                try
                {
                    text = value == null && column.HasDefaultConverter
                        ? null
                        : column.Converter.Convert(value, null, formatProviders[i]);
                }
                catch (Exception ex)
                {
                    throw new PlainTextReportException(
                        $"Cannot convert the value of column '{column.ColumnName}' at row {index + 1}: {ex.Message}",
                        index + 1, column.ColumnName, value, ex);
                }
                text = text ?? nullTexts[i];
                cells[i] = multiLine[i] ? NormalizeLineBreaks(text) : Clean(text);
            }
            return cells;
        }

        /// <summary>
        /// Adds a value to the aggregate of a column; a value that cannot be added gives a <see cref="PlainTextReportException"/>
        /// with the row and the column
        /// </summary>
        /// <param name="totals">The aggregates; null for none</param>
        /// <param name="i">The column</param>
        /// <param name="value">The value</param>
        /// <param name="index">The zero-based position of the item in the collection</param>
        private void AddTotal(ColumnTotal[] totals, int i, object value, int index)
        {
            if (totals == null)
            {
                return;
            }
            try
            {
                totals[i].Add(value);
            }
            catch (Exception ex)
            {
                var column = columns[i];
                throw new PlainTextReportException(
                    $"Cannot add the value of column '{column.ColumnName}' at row {index + 1} to the total {totals[i].Kind}: {ex.Message}",
                    index + 1, column.ColumnName, value, ex);
            }
        }

        /// <summary>
        /// Returns the width the caption needs in an automatic column: the whole caption, or its longest word
        /// with <see cref="PlainTextReportOptions.MultiLineHeader"/>
        /// </summary>
        private int GetCaptionWidth(PlainTextReportColumn column)
        {
            if (!options.MultiLineHeader)
            {
                return textWidth.GetWidth(Clean(column.Caption));
            }
            return NormalizeLineBreaks(column.Caption)
                .Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => textWidth.GetWidth(word))
                .DefaultIfEmpty(0)
                .Max();
        }

        /// <summary>
        /// Returns the width a cell needs: its longest line
        /// </summary>
        private int GetCellWidth(string cell, int column)
        {
            if (!multiLine[column] || cell.IndexOf('\n') < 0)
            {
                return textWidth.GetWidth(cell);
            }
            return cell.Split('\n').Max(line => textWidth.GetWidth(line));
        }

        /// <summary>
        /// Wraps a multi-line cell in lines of the given width: the line breaks start a new line,
        /// the words are not split unless they are longer than the width, and a text element is never split.
        /// </summary>
        internal static string[] Wrap(string text, int width, TextWidth textWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                var first = lines.Count;
                var line = paragraph.TrimEnd();
                while (textWidth.GetWidth(line) > width)
                {
                    // break at the last space that fits, otherwise split the word
                    var fit = textWidth.GetFitLength(line, width);
                    var cut = line.LastIndexOf(' ', fit);
                    if (cut <= 0)
                    {
                        // at least one text element, also when a wide character doesn't fit a narrow column
                        var take = fit > 0 ? fit : textWidth.GetElementLength(line, 0);
                        lines.Add(line.Substring(0, take));
                        line = line.Substring(take);
                    }
                    else
                    {
                        lines.Add(line.Substring(0, cut).TrimEnd());
                        line = line.Substring(cut + 1);
                    }
                    line = line.TrimStart(' ');
                }
                // a wide character split from a narrow column can leave nothing
                if (line.Length > 0 || lines.Count == first)
                {
                    lines.Add(line);
                }
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Returns the width of every column, fitted in the maximum width
        /// </summary>
        /// <param name="rows">The rows and the subtotals; they are read only for the columns that fit their values</param>
        /// <param name="maxWidth">The report width, 0 for no limit</param>
        /// <param name="totals">The aggregates of the columns, complete once the rows are read; null without the totals row</param>
        private int[] GetColumnWidths(IEnumerable<RowItem> rows, int maxWidth, ColumnTotal[] totals)
        {
            var widths = new int[columns.Length];
            var percents = new double[columns.Length];
            string[] totalsCells = null;
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

                var width = options.ShowHeader ? GetCaptionWidth(columns[i]) : 0;
                foreach (var item in rows)
                {
                    // the group headers are lines of their own, out of the columns
                    if (item.Cells != null) width = Math.Max(width, GetCellWidth(item.Cells[i], i));
                }
                if (totals != null)
                {
                    // an automatic column reads every row first, so the aggregates are complete
                    totalsCells = totalsCells ?? GetTotalsCells(totals, options.TotalsCaption);
                    width = Math.Max(width, textWidth.GetWidth(totalsCells[i]));
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
        /// Cuts a text longer than the width: it ends with the truncation marker, or it is cut without
        /// the marker when the width is smaller than the marker. The text is cut between two text elements,
        /// so the result can be one column narrower than the width when a wide character doesn't fit.
        /// </summary>
        private string Truncate(string text, int width)
        {
            if (textWidth.GetWidth(text) <= width)
            {
                return text;
            }
            var markerWidth = textWidth.GetWidth(truncationMarker);
            if (markerWidth > width)
            {
                return text.Substring(0, textWidth.GetFitLength(text, width));
            }
            return text.Substring(0, textWidth.GetFitLength(text, width - markerWidth)) + truncationMarker;
        }

        /// <summary>
        /// Appends a cell, aligned to the width. A truncated text ends with the truncation marker.
        /// </summary>
        private void AppendCell(StringBuilder builder, string text, int width, PlainTextReportAlignment alignment)
        {
            var textLength = textWidth.GetWidth(text);
            if (textLength == width)
            {
                builder.Append(text);
                return;
            }

            if (textLength > width)
            {
                // a truncated text starts at the beginning of the column, padded when a wide character didn't fit
                text = Truncate(text, width);
                textLength = textWidth.GetWidth(text);
                alignment = PlainTextReportAlignment.Left;
            }

            var padding = width - textLength;
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
        /// <remarks>
        /// The pages are composed one at a time, while the rows are read. When the header or the footer
        /// prints {pages}, the rows are measured first to count the pages.
        /// </remarks>
        private IEnumerable<string[]> Paginate(Layout layout, DateTime date)
        {
            var page = layout.Page;
            var header = page.Header ?? new PlainTextReportPageBand();
            var footer = page.Footer ?? new PlainTextReportPageBand();
            var capacity = page.PrintableHeight - GetBandHeight(header) - GetBandHeight(footer);
            if (capacity < 1)
            {
                throw new InvalidOperationException($"The page {page.PaperSize} has no room for the body: reduce the header, the footer or the margins.");
            }

            // the number of pages, only when it is printed: the rows are a list then
            var count = 0;
            if (UsesPageCount(page))
            {
                count = SplitBodies(page, capacity, layout.Title, layout.ColumnHeader, GetBlocks(layout, true)).Count();
            }

            var rows = GetBlocks(layout, false);
            var titleText = options.IsMultiLineTitle ? string.Join(" ", options.MultiLineTitle) : options.Title ?? string.Empty;
            var margin = new string(' ', page.MarginLeft);
            var number = 0;

            foreach (var body in SplitBodies(page, capacity, layout.Title, layout.ColumnHeader, rows))
            {
                number++;
                var lines = new List<string>(page.Height);

                lines.AddRange(Enumerable.Repeat(string.Empty, page.MarginTop));

                if (!header.IsEmpty)
                {
                    lines.AddRange(GetBandLines(header, page.PrintableWidth, number, count, date, titleText, culture));
                    if (header.ShowSeparator) lines.Add(new string(header.SeparatorChar, page.PrintableWidth));
                }

                lines.AddRange(body);
                lines.AddRange(Enumerable.Repeat(string.Empty, capacity - body.Count));

                if (!footer.IsEmpty)
                {
                    if (footer.ShowSeparator) lines.Add(new string(footer.SeparatorChar, page.PrintableWidth));
                    lines.AddRange(GetBandLines(footer, page.PrintableWidth, number, count, date, titleText, culture));
                }

                lines.AddRange(Enumerable.Repeat(string.Empty, page.MarginBottom));

                yield return margin.Length == 0
                    ? lines.ToArray()
                    : lines.Select(line => line.Length == 0 ? line : margin + line).ToArray();
            }
        }

        /// <summary>
        /// Splits the body in pages, while the rows are read; the lines of a block (a multi-line row, a group header
        /// with the first row of its group) stay on the same page, unless the block is longer than a whole page.
        /// The empty line before a group is dropped at the top of a page. There is always at least one page.
        /// </summary>
        /// <param name="blocks">The lines of every block; only their number is used when the pages are just counted</param>
        private static IEnumerable<List<string>> SplitBodies(PlainTextReportPageSettings page, int capacity, List<string> title, List<string> columnHeader, IEnumerable<Block> blocks)
        {
            var nextPageCapacity = capacity - (page.RepeatColumnHeader ? columnHeader.Count : 0);
            using (var enumerator = blocks.GetEnumerator())
            {
                var block = enumerator.MoveNext() ? enumerator.Current : null;
                var rowLine = 0;
                var first = true;
                do
                {
                    var lines = new List<string>(capacity);
                    if (first) lines.AddRange(title);
                    if (first || page.RepeatColumnHeader) lines.AddRange(columnHeader);

                    var start = lines.Count;
                    if (start > capacity || (!first && start == capacity && block != null))
                    {
                        throw new InvalidOperationException($"The page {page.PaperSize} is too small for the title and the column header.");
                    }

                    while (block != null && lines.Count < capacity)
                    {
                        var row = block.Lines;
                        var free = capacity - lines.Count;
                        if (rowLine == 0 && lines.Count > start)
                        {
                            if (block.NewPage)
                            {
                                break;
                            }

                            var gap = block.Gap ? 1 : 0;
                            if (row.Length + gap > free && row.Length <= nextPageCapacity)
                            {
                                // the block fits on the next page
                                break;
                            }
                            if (gap > 0)
                            {
                                lines.Add(string.Empty);
                                free--;
                            }
                        }

                        var take = Math.Min(row.Length - rowLine, free);
                        for (int k = 0; k < take; k++)
                        {
                            lines.Add(row[rowLine++]);
                        }
                        if (rowLine == row.Length)
                        {
                            block = enumerator.MoveNext() ? enumerator.Current : null;
                            rowLine = 0;
                        }
                    }

                    first = false;
                    yield return lines;
                }
                while (block != null);
            }
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
        /// Returns the lines of a band, with the placeholders replaced; a text longer than the width ends with the truncation marker
        /// </summary>
        private List<string> GetBandLines(PlainTextReportPageBand band, int width, int number, int count, DateTime date, string title, CultureInfo culture)
        {
            var left = SplitLines(band.Left);
            var center = SplitLines(band.Center);
            var right = SplitLines(band.Right);
            var height = Math.Max(left.Length, Math.Max(center.Length, right.Length));

            var lines = new List<string>(height);
            for (int i = 0; i < height; i++)
            {
                // one cell per column: null is a space, "" the second column of a wide text element
                var buffer = new string[width];
                var c = Truncate(Format(i < center.Length ? center[i] : null, number, count, date, title, culture), width);
                var l = Truncate(Format(i < left.Length ? left[i] : null, number, count, date, title, culture), width);
                var r = Truncate(Format(i < right.Length ? right[i] : null, number, count, date, title, culture), width);
                Put(buffer, c, (width - textWidth.GetWidth(c)) / 2);
                Put(buffer, l, 0);
                Put(buffer, r, width - textWidth.GetWidth(r));

                var builder = new StringBuilder(width);
                foreach (var cell in buffer)
                {
                    builder.Append(cell ?? " ");
                }
                lines.Add(builder.ToString().TrimEnd());
            }
            return lines;
        }

        /// <summary>
        /// Writes the text in the buffer, from start, cutting what exceeds; a wide text element that is
        /// partly overwritten becomes a space
        /// </summary>
        private void Put(string[] buffer, string text, int start)
        {
            var position = Math.Max(0, start);
            var index = 0;
            while (index < text.Length && position < buffer.Length)
            {
                var length = textWidth.GetElementLength(text, index);
                var element = text.Substring(index, length);
                var width = textWidth.GetWidth(element);
                index += length;

                if (width == 0)
                {
                    // an invisible character alone (a zero width space): it takes no column
                    continue;
                }
                if (position + width > buffer.Length)
                {
                    break;
                }

                for (int k = position; k < position + width; k++)
                {
                    Clear(buffer, k);
                }
                buffer[position] = element;
                for (int k = position + 1; k < position + width; k++)
                {
                    buffer[k] = string.Empty;
                }
                position += width;
            }
        }

        /// <summary>
        /// Clears a cell of the buffer; when it is part of a wide text element, the whole element is cleared
        /// </summary>
        private static void Clear(string[] buffer, int position)
        {
            // the second column of a wide element: clear its first column
            var head = position;
            while (head > 0 && buffer[head] != null && buffer[head].Length == 0)
            {
                head--;
            }
            buffer[head] = null;
            for (int k = head + 1; k < buffer.Length && buffer[k] != null && buffer[k].Length == 0; k++)
            {
                buffer[k] = null;
            }
            buffer[position] = null;
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
