using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class CaptionTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m },
        };

        private static string[] Render(Action<PlainTextReportOptions<Product>> columns)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                columns(options);
            }).Bind(Products).GetTextLinesReport();
        }

        [Fact]
        public void Caption_is_null_by_default()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.AddColumn(m => m.Price, style => { });

            Assert.Null(options.Columns.Single().Style.Caption);
            Assert.Equal("Price", options.Columns.Single().Caption);
        }

        [Fact]
        public void Caption_replaces_the_property_name()
        {
            var lines = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => style.Caption = "Codice");
                o.Columns.AddColumn(m => m.Price, style => style.Caption = "Prezzo", columnFormat: "N2");
            });

            Assert.Equal(new[]
            {
                "Codice | Prezzo",
                "-------+-------",
                "     1 |   1.50",
            }, lines);
        }

        [Fact]
        public void Caption_wins_over_the_column_name()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.AddColumn("Name", m => m.Name);
            options.Columns.Single().Style.Caption = "Descrizione";

            Assert.Equal("Descrizione", options.Columns.Single().Caption);
            Assert.Equal("Name", options.Columns.Single().ColumnName);
        }

        [Fact]
        public void Caption_works_with_width_and_alignment()
        {
            var lines = Render(o => o.Columns.AddColumn(m => m.Name, style =>
            {
                style.Caption = "Prodotto";
                style.Width = 10;
                style.HeaderAlignment = PlainTextReportAlignment.Center;
            }));

            Assert.Equal(new[] { " Prodotto", "----------", "Apple" }, lines);
        }

        [Fact]
        public void A_long_caption_widens_the_column()
        {
            var lines = Render(o => o.Columns.AddColumn(m => m.Id, style => style.Caption = "Identificativo"));

            Assert.Equal("Identificativo", lines[0]);
            Assert.Equal("             1", lines[2]);
        }

        [Fact]
        public void A_caption_longer_than_the_width_is_truncated()
        {
            var lines = Render(o => o.Columns.AddColumn(m => m.Name, style => { style.Caption = "Descrizione"; style.Width = 6; }));

            Assert.Equal("Descr…", lines[0]);
        }

        [Fact]
        public void Line_breaks_in_the_caption_do_not_break_the_layout()
        {
            var lines = Render(o => o.Columns.AddColumn(m => m.Name, style => style.Caption = "Nome\r\nprodotto"));

            Assert.Equal("Nome prodotto", lines[0]);
            Assert.Equal(new string('-', 13), lines[1]);
        }

        [Fact]
        public void An_empty_caption_prints_an_empty_header()
        {
            var lines = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => style.Caption = string.Empty);
                o.Columns.AddColumn(m => m.Name, style => { });
            });

            Assert.Equal("  | Name", lines[0]);
            Assert.Equal("1 | Apple", lines[2]);
        }
    }
}
