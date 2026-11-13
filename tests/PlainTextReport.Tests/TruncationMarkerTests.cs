using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class TruncationMarkerTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m },
            new Product { Id = 10, Name = "Banana", Price = 12.25m },
        };

        private static string[] Render(string? marker, int width, string? title = null)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowTitle = title != null;
                options.Title = title;
                options.ShowColumnSeparator = true;
                options.Width = width;
                if (marker != "default") options.TruncationMarker = marker!;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(Products).GetTextLinesReport();
        }

        [Theory]
        [InlineData("default", "10 | Ban…")]
        [InlineData(null, "10 | Ban…")]
        [InlineData("~", "10 | Ban~")]
        [InlineData("...", "10 | B...")]
        [InlineData("", "10 | Bana")]
        public void Cells_end_with_the_marker(string? marker, string expected)
        {
            var lines = Render(marker, 9);

            Assert.Equal(expected, lines[1]);
        }

        [Fact]
        public void The_default_marker_is_the_ellipsis()
        {
            Assert.Equal("…", new PlainTextReportOptions<Product>().TruncationMarker);
        }

        [Fact]
        public void A_column_narrower_than_the_marker_is_cut_without_it()
        {
            // the Name column is 2 characters wide
            var lines = Render("...", 7);

            Assert.Equal("10 | Ba", lines[1]);
        }

        [Fact]
        public void A_column_as_wide_as_the_marker_shows_only_the_marker()
        {
            // the Name column is 3 characters wide
            var lines = Render("...", 8);

            Assert.Equal("10 | ...", lines[1]);
        }

        [Theory]
        [InlineData("default", "Fruit an…")]
        [InlineData("~", "Fruit an~")]
        [InlineData("", "Fruit and")]
        public void The_title_ends_with_the_marker(string? marker, string expected)
        {
            var lines = Render(marker, 9, title: "Fruit and vegetables");

            Assert.Equal(expected, lines[0]);
        }

        [Theory]
        [InlineData("default", "A very long hea…")]
        [InlineData("...", "A very long h...")]
        [InlineData("", "A very long head")]
        public void Header_and_footer_texts_end_with_the_marker(string? marker, string expected)
        {
            // 20 characters x 12 lines, 2 characters of margin on each side: the bands are 16 characters wide
            var page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
                MarginLeft = 2,
                MarginRight = 2,
            };
            page.Header.Left = "A very long header text";
            page.Footer.Right = "A very long header text";

            var report = new PlainTextReport<Product>(options =>
            {
                options.Page = page;
                if (marker != "default") options.TruncationMarker = marker!;
                options.Columns.AddColumn(m => m.Id, style => { });
            }).Bind(Products);
            var lines = report.GetPagesReport()[0].Lines;

            Assert.Equal("  " + expected, lines[0]);
            Assert.Equal("  " + expected, lines[lines.Length - 1]);
        }
    }
}
