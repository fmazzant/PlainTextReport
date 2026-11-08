using System.Globalization;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class CultureTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1234.5m, CreatedOn = new DateTime(2024, 1, 2) },
        };

        private static string Cell(Action<PlainTextReportOptions<Product>> configure)
        {
            return new PlainTextReport<Product>(configure).Bind(Products).GetTextLinesReport().Single();
        }

        [Fact]
        public void The_invariant_culture_is_the_default()
        {
            Assert.Null(new PlainTextReportOptions<Product>().Culture);
            Assert.Equal("1,234.50", Cell(o => o.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2")));
            Assert.Equal("1234.5", Cell(o => o.Columns.AddColumn(m => m.Price, style => { })));
        }

        [Fact]
        public void Formats_the_numbers_with_the_culture()
        {
            Assert.Equal("1.234,50", Cell(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
            }));
        }

        [Fact]
        public void Formats_the_values_without_format_with_the_culture()
        {
            Assert.Equal("1234,5", Cell(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.Price, style => { });
            }));
        }

        [Fact]
        public void Formats_the_dates_with_the_culture()
        {
            Assert.Equal("02 gennaio 2024", Cell(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd MMMM yyyy");
            }));
        }

        [Fact]
        public void The_format_provider_of_the_column_wins()
        {
            // it-IT would print 1234,5
            Assert.Equal("1234.5", Cell(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.Price, new CultureInfo("en-US"));
            }));
        }

        [Fact]
        public void A_custom_converter_receives_the_culture()
        {
            Assert.Equal("€ 1.234,50", Cell(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.Price, style => { }, new NumberFormatConverter());
            }));
        }

        [Fact]
        public void The_provider_of_a_formatted_column_gives_the_formats_of_the_culture()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Culture = new CultureInfo("it-IT");
            options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N0");
            var provider = options.Columns.Single().GetFormatProvider(options.Culture);

            Assert.Equal("1.234,50", 1234.5m.ToString("N2", provider));
            Assert.Equal("1.235", string.Format(provider, "{0}", 1234.5m));
        }

        [Fact]
        public void The_public_format_provider_of_the_column_is_invariant()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");

            Assert.Equal("1,234.50", string.Format(options.Columns.Single().ColumnFormatProvider, "{0}", 1234.5m));
        }

        [Fact]
        public void The_date_placeholder_uses_the_culture()
        {
            var page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
            {
                CharactersPerInch = 10,
                LinesPerInch = 4,
                Date = new DateTime(2026, 11, 7),
            };
            page.Header.Left = "{date:dd MMMM}";

            var pages = new PlainTextReport<Product>(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Page = page;
                o.Columns.AddColumn(m => m.Id, style => { });
            }).Bind(Products).GetPagesReport();

            Assert.Equal("07 novembre", pages[0].Lines[0]);
        }
    }
}
