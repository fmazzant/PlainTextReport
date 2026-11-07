using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class ColumnWidthTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m, CreatedOn = new DateTime(2024, 1, 2) },
            new Product { Id = 10, Name = "Banana", Price = 12.25m, CreatedOn = new DateTime(2024, 12, 31) },
        };

        private static string Dashes(params int[] widths) => string.Join("-+-", widths.Select(w => new string('-', w)));

        private static string[] Render(int width, ColumnWidth? name, ColumnWidth? createdOn = null)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Width = width;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => style.Width = name);
                if (createdOn.HasValue)
                {
                    options.Columns.AddColumn(m => m.CreatedOn, style => style.Width = createdOn, columnFormat: "yyyy-MM-dd");
                }
                else
                {
                    options.Columns.AddColumn(m => m.Price, style => { });
                }
            }).Bind(Products).GetTextLinesReport();
        }

        [Theory]
        [InlineData("20", 20, false)]
        [InlineData("50%", 50, true)]
        [InlineData(" 12.5 % ", 12.5, true)]
        [InlineData("100%", 100, true)]
        public void Parses_characters_and_percentages(string text, double value, bool isPercent)
        {
            ColumnWidth width = text;

            Assert.Equal(value, width.Value);
            Assert.Equal(isPercent, width.IsPercent);
        }

        [Fact]
        public void Converts_from_int()
        {
            ColumnWidth width = 15;

            Assert.Equal(ColumnWidth.Characters(15), width);
            Assert.Equal("15", width.ToString());
            Assert.Equal("50%", ColumnWidth.Percent(50).ToString());
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("%")]
        [InlineData("10.5")]
        public void Rejects_invalid_text(string text)
        {
            Assert.Throws<FormatException>(() => ColumnWidth.Parse(text));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-3")]
        [InlineData("0%")]
        [InlineData("-5%")]
        public void Rejects_widths_out_of_range(string text)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ColumnWidth.Parse(text));
        }

        [Fact]
        public void Column_at_100_percent_takes_all_the_free_space()
        {
            // 40 - 2 separators (6) - Id (2) - Price (5) = 27
            var lines = Render(40, "100%");

            Assert.Equal(Dashes(2, 27, 5), lines[1]);
            Assert.Equal(40, lines[1].Length);
            Assert.Equal(" 1 | Apple                       |   1.5", lines[2]);
        }

        [Fact]
        public void Column_at_50_percent_takes_half_of_the_free_space()
        {
            var lines = Render(40, "50%");

            Assert.Equal(Dashes(2, 13, 5), lines[1]);
        }

        [Fact]
        public void Two_columns_at_100_percent_share_the_free_space()
        {
            // 41 - 6 - 2 = 33 free characters: 17 + 16, the rounding goes to the first column
            var full = Render(41, "100%", "100%");
            var half = Render(41, "50%", "50%");

            Assert.Equal(Dashes(2, 17, 16), full[1]);
            Assert.Equal(full, half);
        }

        [Fact]
        public void Percentages_are_proportional()
        {
            // 33 free characters: 75% and 25%
            var lines = Render(41, "75%", "25%");

            Assert.Equal(Dashes(2, 25, 8), lines[1]);
        }

        [Fact]
        public void Percent_column_is_automatic_without_a_report_width()
        {
            var lines = Render(0, "100%");

            Assert.Equal(new[]
            {
                "Id | Name   | Price",
                "---+--------+------",
                " 1 | Apple  |   1.5",
                "10 | Banana | 12.25",
            }, lines);
        }

        [Fact]
        public void Percent_column_fills_the_page_width()
        {
            var lines = new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Page = new PlainTextReportPageSettings(PaperSize.A4, PageOrientation.Landscape);
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => style.Width = "100%");
            }).Bind(Products).GetPagesReport()[0].Lines;

            Assert.Equal(116, lines[1].Length);
            Assert.Equal(Dashes(2, 111), lines[1]);
        }

        [Fact]
        public void Percent_column_without_free_space_is_one_character()
        {
            var lines = Render(10, "100%");

            Assert.All(lines, line => Assert.True(line.Length <= 10, line));
            Assert.Equal(new string('-', 10).Length, lines[1].Length);
        }
    }
}
