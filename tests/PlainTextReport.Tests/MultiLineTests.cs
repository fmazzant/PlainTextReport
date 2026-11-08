using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class MultiLineTests
    {
        private static IPlainTextReport<Product> CreateReport(string? name, Action<PlainTextReportOptions<Product>>? configure = null, Action<PlainTextReportColumnStyle>? nameStyle = null)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => nameStyle?.Invoke(style));
                options.Columns.AddColumn(m => m.Price, style => { });
                configure?.Invoke(options);
            }).Bind(new[] { new Product { Id = 1, Name = name, Price = 1.5m } });
        }

        [Fact]
        public void MultiLineRows_is_off_by_default()
        {
            var options = new PlainTextReportOptions<Product>();

            Assert.False(options.MultiLineRows);
            Assert.Null(new PlainTextReportColumnStyle().MultiLine);
        }

        [Fact]
        public void Wraps_a_long_value_on_word_boundaries()
        {
            var lines = CreateReport("Red apple from Italy", o => o.MultiLineRows = true, s => s.Width = 10).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id | Name       | Price",
                "---+------------+------",
                " 1 | Red apple  |   1.5",
                "   | from Italy |",
            }, lines);
        }

        [Fact]
        public void Without_multi_line_a_long_value_is_truncated()
        {
            var lines = CreateReport("Red apple from Italy", nameStyle: s => s.Width = 10).GetTextLinesReport();

            Assert.Equal(" 1 | Red apple… |   1.5", lines[2]);
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void Line_breaks_of_the_value_start_a_new_line()
        {
            var lines = CreateReport("a\r\nbb\nccc", o => o.MultiLineRows = true).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id | Name | Price",
                "---+------+------",
                " 1 | a    |   1.5",
                "   | bb   |",
                "   | ccc  |",
            }, lines);
        }

        [Fact]
        public void Keeps_the_empty_lines_of_the_value()
        {
            var lines = CreateReport("a\n\nb", o => o.MultiLineRows = true).GetTextLinesReport();

            Assert.Equal(new[] { " 1 | a    |   1.5", "   |      |", "   | b    |" }, lines.Skip(2));
        }

        [Fact]
        public void The_width_is_the_longest_line()
        {
            var lines = CreateReport("ab\nabcdefg\nabc", o => o.MultiLineRows = true).GetTextLinesReport();

            Assert.Equal("Id | Name    | Price", lines[0]);
            Assert.Equal("   | abcdefg |", lines[3]);
        }

        [Fact]
        public void Splits_a_word_longer_than_the_column()
        {
            var lines = CreateReport("abcdefghij", o => o.MultiLineRows = true, s => s.Width = 4).GetTextLinesReport();

            Assert.Equal(new[] { " 1 | abcd |   1.5", "   | efgh |", "   | ij   |" }, lines.Skip(2));
        }

        [Fact]
        public void Wraps_the_columns_shrunk_to_the_report_width()
        {
            var lines = CreateReport("Red apple from Italy", o => { o.MultiLineRows = true; o.Width = 20; }).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id | Name    | Price",
                "---+---------+------",
                " 1 | Red     |   1.5",
                "   | apple   |",
                "   | from    |",
                "   | Italy   |",
            }, lines);
            Assert.All(lines, line => Assert.True(line.Length <= 20));
        }

        [Fact]
        public void Applies_the_alignment_to_every_line()
        {
            var lines = CreateReport("a\nbbb", o => o.MultiLineRows = true, s => { s.Width = 5; s.Alignment = PlainTextReportAlignment.Right; }).GetTextLinesReport();

            Assert.Equal(" 1 |     a |   1.5", lines[2]);
            Assert.Equal("   |   bbb |", lines[3]);
        }

        [Fact]
        public void Tabs_become_spaces()
        {
            var lines = CreateReport("a\tb\nc", o => o.MultiLineRows = true).GetTextLinesReport();

            Assert.Equal(" 1 | a b  |   1.5", lines[2]);
            Assert.Equal("   | c    |", lines[3]);
        }

        [Fact]
        public void Null_value_is_one_empty_line()
        {
            var lines = CreateReport(null, o => o.MultiLineRows = true).GetTextLinesReport();

            Assert.Equal(new[] { " 1 |      |   1.5" }, lines.Skip(2));
        }

        [Fact]
        public void Column_style_can_disable_multi_line()
        {
            var lines = CreateReport("a\nb", o => o.MultiLineRows = true, s => s.MultiLine = false).GetTextLinesReport();

            Assert.Equal(new[] { " 1 | a b  |   1.5" }, lines.Skip(2));
        }

        [Fact]
        public void Column_style_can_enable_multi_line()
        {
            var lines = CreateReport("a\nb", nameStyle: s => s.MultiLine = true).GetTextLinesReport();

            Assert.Equal(new[] { " 1 | a    |   1.5", "   | b    |" }, lines.Skip(2));
        }

        [Fact]
        public void Every_row_has_its_own_height()
        {
            var report = new PlainTextReport<Product>(options =>
            {
                options.MultiLineRows = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(new[]
            {
                new Product { Id = 1, Name = "a\nb" },
                new Product { Id = 2, Name = "c" },
                new Product { Id = 3, Name = "d\ne\nf" },
            });

            Assert.Equal(new[] { "1 | a", "  | b", "2 | c", "3 | d", "  | e", "  | f" }, report.GetTextLinesReport());
        }

        // 20 characters x 12 lines, no header and footer
        private static PlainTextReportPageSettings SmallPage() =>
            new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3)) { CharactersPerInch = 10, LinesPerInch = 4 };

        private static PlainTextReport<Item> CreatePagedReport(PlainTextReportPageSettings page)
        {
            return new PlainTextReport<Item>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.MultiLineRows = true;
                options.Page = page;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            });
        }

        [Fact]
        public void A_multi_line_row_is_not_split_between_two_pages()
        {
            var items = Enumerable.Range(1, 9).Select(i => new Item { Id = i, Name = $"N{i}" })
                .Append(new Item { Id = 10, Name = "L1\nL2\nL3" });

            var pages = CreatePagedReport(SmallPage()).Bind(items).GetPagesReport();

            Assert.Equal(2, pages.Length);
            Assert.Equal(" 9 | N9", pages[0].Lines[10]);
            Assert.Equal(string.Empty, pages[0].Lines[11]);
            Assert.Equal(new[] { "Id | Name", "---+-----", "10 | L1", "   | L2", "   | L3" }, pages[1].Lines.Take(5));
            Assert.All(pages, page => Assert.Equal(12, page.Lines.Length));
        }

        [Fact]
        public void A_row_longer_than_a_page_continues_on_the_next_page()
        {
            var name = string.Join("\n", Enumerable.Range(1, 15).Select(i => $"L{i}"));

            var pages = CreatePagedReport(SmallPage()).Bind(new[] { new Item { Id = 1, Name = name } }).GetPagesReport();

            Assert.Equal(2, pages.Length);
            Assert.Equal(" 1 | L1", pages[0].Lines[2]);
            Assert.Equal("   | L10", pages[0].Lines[11]);
            Assert.Equal(new[] { "Id | Name", "---+-----", "   | L11", "   | L12", "   | L13", "   | L14", "   | L15" }, pages[1].Lines.Take(7));
        }

        [Fact]
        public void Single_line_rows_fill_the_pages_as_before()
        {
            var items = Enumerable.Range(1, 25).Select(i => new Item { Id = i, Name = $"N{i}" });

            var pages = CreatePagedReport(SmallPage()).Bind(items).GetPagesReport();

            // 10 rows per page after the 2 lines of the column header
            Assert.Equal(3, pages.Length);
            Assert.Equal("10 | N10", pages[0].Lines[11]);
            Assert.Equal("20 | N20", pages[1].Lines[11]);
            Assert.Equal("25 | N25", pages[2].Lines[6]);
        }
    }
}
