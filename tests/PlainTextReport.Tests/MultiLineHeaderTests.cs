using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class MultiLineHeaderTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m, CreatedOn = new DateTime(2024, 1, 2) },
        };

        private static string[] Render(Action<PlainTextReportOptions<Product>> configure, bool multiLineHeader = true)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.MultiLineHeader = multiLineHeader;
                configure(options);
            }).Bind(Products).GetTextLinesReport();
        }

        [Fact]
        public void A_line_break_in_the_caption_starts_a_new_line()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.CreatedOn, column => column.Caption("Created\non").Width(10).Format("dd/MM/yyyy"));
                o.Columns.Add(m => m.Price, column => column.Format("N2"));
            });

            Assert.Equal(new[]
            {
                "Id | Created    | Price",
                "   | on         |",
                "---+------------+------",
                " 1 | 02/01/2024 |  1.50",
            }, lines);
        }

        [Fact]
        public void Automatic_columns_are_sized_on_the_longest_word()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id, column => column.Caption("Product code"));
                o.Columns.Add(m => m.Price, column => column.Caption("Unit price"));
            });

            Assert.Equal(new[]
            {
                "Product |  Unit",
                "   code | price",
                "--------+------",
                "      1 |   1.5",
            }, lines);
        }

        [Fact]
        public void A_long_caption_wraps_in_a_fixed_width_column()
        {
            var lines = Render(o => o.Columns.Add(m => m.Name, column => column.Caption("Quantity ordered").Width(5)));

            Assert.Equal(new[] { "Quant", "ity", "order", "ed", "-----", "Apple" }, lines);
        }

        [Fact]
        public void The_header_alignment_is_used_on_every_line()
        {
            var lines = Render(o => o.Columns.Add(m => m.Name, column => column.Caption("Long\nname").Width(8).HeaderAlign(PlainTextReportAlignment.Center)));

            Assert.Equal("  Long", lines[0]);
            Assert.Equal("  name", lines[1]);
        }

        [Fact]
        public void Without_MultiLineHeader_the_caption_stays_on_one_line()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id, column => column.Caption("Created\non"));
            }, multiLineHeader: false);

            Assert.Equal(new[] { "Created on", "----------", "         1" }, lines);
        }

        [Fact]
        public void A_short_caption_is_one_line()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal(new[] { "Id | Name", "---+------", " 1 | Apple" }, lines);
        }

        // 2 x 3 inches: portrait at 10 cpi / 4 lpi is 20 characters x 12 lines
        [Fact]
        public void With_pages_the_header_height_is_counted_and_repeated()
        {
            var items = Enumerable.Range(1, 20).Select(i => new Item { Id = i, Name = $"N{i}" });
            var pages = new PlainTextReport<Item>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.MultiLineHeader = true;
                options.Page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
                {
                    CharactersPerInch = 10,
                    LinesPerInch = 4,
                    RepeatColumnHeader = true,
                };
                options.Columns.Add(m => m.Id, column => column.Caption("Item\ncode"));
                options.Columns.Add(m => m.Name);
            }).Bind(items).GetPagesReport();

            // 3 header lines and 9 rows on every page: 9 + 9 + 2
            Assert.Equal(3, pages.Length);
            Assert.Equal(pages[0].Lines.Take(3), pages[1].Lines.Take(3));
            Assert.Equal(new[] { "Item Name", "code", "---- ----" }, pages[2].Lines.Take(3));
            Assert.Equal("  19 N19", pages[2].Lines[3]);
            Assert.Equal("  20 N20", pages[2].Lines[4]);
        }
    }
}
