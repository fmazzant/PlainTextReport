using System.Globalization;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class ColumnBuilderTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m, CreatedOn = new DateTime(2024, 1, 2), Enabled = true },
            new Product { Id = 10, Name = null, Price = 1234.25m, CreatedOn = new DateTime(2024, 3, 4), Enabled = false },
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
        public void Add_without_settings_uses_the_property()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Price);

            var column = options.Columns.Single();
            Assert.Equal("Price", column.ColumnName);
            Assert.Equal("Price", column.Caption);
            Assert.Equal(0, column.ColumnIndex);
            Assert.Equal(typeof(decimal), column.ColumnType);
            Assert.Null(column.ColumnFormat);
        }

        [Fact]
        public void Add_uses_the_path_of_a_nested_property()
        {
            var options = new PlainTextReportOptions<Order>();
            options.Columns.Add(m => m.Customer!.Name);

            Assert.Equal("Customer.Name", options.Columns.Single().ColumnName);
        }

        [Fact]
        public void Add_with_a_name()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add("Product", m => m.Name);
            options.Columns.Add(null, m => m.Id);

            Assert.Equal(new[] { "Product", "Id" }, options.Columns.Select(c => c.ColumnName));
        }

        [Fact]
        public void Every_setting_in_one_place()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id, column => column.Index(1));
                o.Columns.Add(m => m.Price, column => column
                    .Caption("Prezzo")
                    .Format("N2")
                    .FormatProvider(new CultureInfo("it-IT"))
                    .Width(10)
                    .Align(PlainTextReportAlignment.Right)
                    .HeaderAlign(PlainTextReportAlignment.Center)
                    .Index(0));
            });

            Assert.Equal(new[]
            {
                "  Prezzo   | Id",
                "-----------+---",
                "      1,50 |  1",
                "  1.234,25 | 10",
            }, lines);
        }

        [Fact]
        public void Format_uses_the_culture_of_the_report()
        {
            var lines = new PlainTextReport<Product>(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.Add(m => m.Price, column => column.Format("N2"));
                o.Columns.Add(m => m.CreatedOn, column => column.Format("dd MMMM yyyy"));
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("    1,50 02 gennaio 2024", lines[0]);
        }

        [Fact]
        public void Format_and_converter_together()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Price, column => column.Format("N2").Converter(new YesNoConverter()));

            var column = options.Columns.Single();
            Assert.Equal("N2", column.ColumnFormat);
            Assert.IsType<YesNoConverter>(column.Converter);
        }

        [Fact]
        public void Converter_converts_the_values()
        {
            var lines = Render(o => o.Columns.Add(m => m.Enabled, column => column.Caption("On").Converter(new YesNoConverter())));

            Assert.Equal(new[] { "On", "---", "Yes", "No" }, lines);
        }

        [Fact]
        public void A_null_converter_restores_the_default_one()
        {
            var lines = Render(o => o.Columns.Add(m => m.Enabled, column => column.Converter(new YesNoConverter()).Converter(null)));

            Assert.Equal(new[] { "Enabled", "-------", "True", "False" }, lines);
        }

        [Fact]
        public void Width_in_characters_and_percentage()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Id, column => column.Width(6));
            options.Columns.Add(m => m.Name, column => column.Width("50%"));

            Assert.Equal(ColumnWidth.Characters(6), options.Columns.First().Style.Width);
            Assert.Equal(ColumnWidth.Percent(50), options.Columns.Last().Style.Width);
        }

        [Fact]
        public void NullText_and_MultiLine()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.Name, column => column.NullText("n/a").MultiLine().Width(3));
            });

            Assert.Equal(new[]
            {
                "Id | Na…",
                "---+----",
                " 1 | App",
                "   | le",
                "10 | n/a",
            }, lines);
        }

        [Fact]
        public void MultiLine_false_wins_over_the_report()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Name, column => column.MultiLine(false));

            Assert.False(options.Columns.Single().Style.MultiLine);
        }

        [Fact]
        public void Style_sets_the_style_directly()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Name, column => column.Style(style => style.Caption = "Prodotto").Width(5));

            var style = options.Columns.Single().Style;
            Assert.Equal("Prodotto", style.Caption);
            Assert.Equal(ColumnWidth.Characters(5), style.Width);
        }

        [Fact]
        public void The_last_setting_wins()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Name, column => column.Caption("A").Caption("B"));

            Assert.Equal("B", options.Columns.Single().Caption);
        }

        [Fact]
        public void Index_orders_the_columns()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Name, column => column.Index(1));
                o.Columns.Add(m => m.Id, column => column.Index(0));
            });

            Assert.Equal("Id | Name", lines[0]);
        }

        [Fact]
        public void Without_an_index_the_column_is_added_at_the_end()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add(m => m.Id);
            options.Columns.Add(m => m.Name);

            Assert.Equal(new[] { 0, 1 }, options.Columns.Select(c => c.ColumnIndex));
        }

        [Fact]
        public void A_format_provider_that_is_not_a_culture_is_used_as_it_is()
        {
            var options = new PlainTextReportOptions<Product>();
            var provider = new NumberFormatInfo();
            options.Columns.Add(m => m.Price, column => column.Format("N2").FormatProvider(provider));

            Assert.Same(provider, options.Columns.Single().ColumnFormatProvider);
        }

        [Fact]
        public void A_negative_index_throws()
        {
            var options = new PlainTextReportOptions<Product>();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.Columns.Add(m => m.Id, column => column.Index(-1)));
            Assert.Equal(0, options.Columns.Count);
        }

        [Fact]
        public void A_null_expression_throws()
        {
            var options = new PlainTextReportOptions<Product>();

            Assert.Throws<ArgumentNullException>(() => options.Columns.Add<int>(null!));
            Assert.Throws<ArgumentNullException>(() => options.Columns.Add<int>("Id", null!));
        }

        [Fact]
        public void An_expression_that_is_not_a_member_throws()
        {
            var options = new PlainTextReportOptions<Product>();

            Assert.Throws<ArgumentException>(() => options.Columns.Add(m => m.Price * 2));
        }

        [Fact]
        public void Same_output_as_the_obsolete_overloads()
        {
            var builder = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.Name, column => column.Width(8));
                o.Columns.Add(m => m.Price, column => column.Format("N2"));
                o.Columns.Add(m => m.Enabled, column => column.Align(PlainTextReportAlignment.Center).Converter(new YesNoConverter()));
            });

            var obsolete = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Name, style => style.Width = 8);
                o.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
                o.Columns.AddColumn(m => m.Enabled, style => style.Alignment = PlainTextReportAlignment.Center, new YesNoConverter());
            });

            Assert.Equal(obsolete, builder);
        }
    }
}
