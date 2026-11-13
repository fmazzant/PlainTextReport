using Mafesoft.PlainTextReport;
using Mafesoft.PlainTextReport.Conversions;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class NullTextTests
    {
        private static readonly Order[] Orders =
        {
            new Order { Id = 1, Customer = new Customer { Name = "Mario" }, ShippedOn = new DateTime(2026, 3, 4) },
            new Order { Id = 2, Customer = new Customer() },
            new Order { Id = 3 },
        };

        private class NullConverter : IValueConverter
        {
            public string Convert(object value, object parameter, IFormatProvider provider)
                => value is int id && id == 1 ? "one" : null!;
        }

        private class NullAwareConverter : IValueConverter
        {
            public string Convert(object value, object parameter, IFormatProvider provider)
                => value == null ? "none" : value.ToString()!;
        }

        private static string[][] Render(Action<PlainTextReportOptions<Order>> configure)
        {
            var lines = new PlainTextReport<Order>(options =>
            {
                options.ShowColumnSeparator = true;
                configure(options);
            }).Bind(Orders).GetTextLinesReport();
            return lines.Select(line => line.Split('|').Select(cell => cell.Trim()).ToArray()).ToArray();
        }

        [Fact]
        public void A_null_value_is_an_empty_cell_by_default()
        {
            var rows = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Customer!.Name, style => { });
            });

            Assert.Equal(new[] { "1", "Mario" }, rows[0]);
            Assert.Equal(new[] { "2", "" }, rows[1]);
            Assert.Equal(new[] { "3", "" }, rows[2]);
        }

        [Fact]
        public void The_report_null_text_is_printed_for_null_values_and_null_intermediate_values()
        {
            var rows = Render(o =>
            {
                o.NullText = "-";
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Customer!.Name, style => { });
                o.Columns.AddColumn("Year", m => m.ShippedOn!.Value.Year);
            });

            Assert.Equal(new[] { "1", "Mario", "2026" }, rows[0]);
            Assert.Equal(new[] { "2", "-", "-" }, rows[1]);   // Name is null
            Assert.Equal(new[] { "3", "-", "-" }, rows[2]);   // Customer is null
        }

        [Fact]
        public void The_column_null_text_wins_over_the_report()
        {
            var rows = Render(o =>
            {
                o.NullText = "-";
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Customer!.Name, style => style.NullText = "n/a");
                o.Columns.AddColumn(m => m.ShippedOn, style => style.NullText = "");
            });

            Assert.Equal(new[] { "2", "n/a", "" }, rows[1]);
            Assert.Equal(new[] { "3", "n/a", "" }, rows[2]);
        }

        [Fact]
        public void A_converter_that_returns_null_prints_the_null_text()
        {
            var rows = Render(o =>
            {
                o.NullText = "-";
                o.Columns.AddColumn(m => m.Id, style => { }, new NullConverter());
            });

            Assert.Equal(new[] { "one", "-", "-" }, rows.Select(r => r[0]));
        }

        [Fact]
        public void A_custom_converter_can_print_the_null_values()
        {
            var rows = Render(o =>
            {
                o.NullText = "-";
                o.Columns.AddColumn(m => m.Customer!.Name, style => { }, new NullAwareConverter());
            });

            Assert.Equal(new[] { "Mario", "none", "none" }, rows.Select(r => r[0]));
        }

        [Fact]
        public void The_null_text_is_used_for_the_column_width()
        {
            var lines = new PlainTextReport<Order>(options =>
            {
                options.NullText = "(no customer)";
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Customer!.Name, style => { });
                options.Columns.AddColumn(m => m.Id, style => { });
            }).Bind(Orders).GetTextLinesReport();

            Assert.Equal(new[] { "Mario         | 1", "(no customer) | 2", "(no customer) | 3" }, lines);
        }

        [Fact]
        public void A_null_byte_array_prints_the_null_text()
        {
            var lines = new PlainTextReport<Attachment>(options =>
            {
                options.NullText = "-";
                options.Columns.AddColumn(m => m.Content, style => { });
            }).Bind(new[] { new Attachment { Content = new byte[] { 1, 2 } }, new Attachment() }).GetTextLinesReport();

            Assert.Equal(new[] { "AQI=", "-" }, lines);
        }
    }
}
