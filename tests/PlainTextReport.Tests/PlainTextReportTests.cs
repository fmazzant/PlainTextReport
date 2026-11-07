using Mafesoft.PlainTextReport;
using Mafesoft.PlainTextReport.Conversions;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool Enabled { get; set; }
    }

    public class YesNoConverter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider)
            => value is bool b && b ? "Yes" : "No";
    }

    public class PlainTextReportTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m, CreatedOn = new DateTime(2024, 1, 2), Enabled = true },
            new Product { Id = 10, Name = "Banana", Price = 12.25m, CreatedOn = new DateTime(2024, 12, 31), Enabled = false },
        };

        private static PlainTextReport<Product> CreateReport(Action<PlainTextReportOptions<Product>>? configure = null)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
                options.Columns.AddColumn(m => m.Price, style => { });
                configure?.Invoke(options);
            });
        }

        [Fact]
        public void Renders_header_separator_and_rows()
        {
            var lines = CreateReport().Bind(Products).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id | Name   | Price",
                "---+--------+------",
                " 1 | Apple  |   1.5",
                "10 | Banana | 12.25",
            }, lines);
        }

        [Fact]
        public void GetTextReport_joins_the_lines()
        {
            var report = CreateReport().Bind(Products);

            Assert.Equal(string.Join(Environment.NewLine, report.GetTextLinesReport()), report.GetTextReport());
        }

        [Fact]
        public void Renders_without_column_separator()
        {
            var lines = CreateReport(o => o.ShowColumnSeparator = false).Bind(Products).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id Name   Price",
                "-- ------ -----",
                " 1 Apple    1.5",
                "10 Banana 12.25",
            }, lines);
        }

        [Fact]
        public void Renders_custom_separator_characters()
        {
            var lines = CreateReport(o =>
            {
                o.ColumnSeparator = ':';
                o.HeaderSeparator = '=';
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("Id : Name   : Price", lines[0]);
            Assert.Equal("===+========+======", lines[1]);
        }

        [Fact]
        public void Renders_the_title_centered()
        {
            var lines = CreateReport(o =>
            {
                o.ShowTitle = true;
                o.Title = "Report";
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("      Report", lines[0]);
            Assert.Equal(string.Empty, lines[1]);
            Assert.Equal("Id | Name   | Price", lines[2]);
        }

        [Fact]
        public void Renders_the_multi_line_title()
        {
            var lines = CreateReport(o =>
            {
                o.ShowTitle = true;
                o.Title = "ignored";
                o.MultiLineTitle = new[] { "First", "Second" };
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("       First", lines[0]);
            Assert.Equal("      Second", lines[1]);
            Assert.Equal(string.Empty, lines[2]);
        }

        [Fact]
        public void Empty_multi_line_title_falls_back_to_title()
        {
            var lines = CreateReport(o =>
            {
                o.ShowTitle = true;
                o.Title = "Report";
                o.MultiLineTitle = Array.Empty<string>();
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("      Report", lines[0]);
        }

        [Fact]
        public void Title_is_hidden_when_ShowTitle_is_false()
        {
            var lines = CreateReport(o => o.Title = "Report").Bind(Products).GetTextLinesReport();

            Assert.Equal("Id | Name   | Price", lines[0]);
        }

        [Fact]
        public void Shrinks_the_columns_to_fit_the_width()
        {
            var lines = CreateReport(o => o.Width = 15).Bind(Products).GetTextLinesReport();

            Assert.All(lines, line => Assert.True(line.Length <= 15, line));
            // the text column is shrunk first, so the numbers are not truncated
            Assert.Equal(new[]
            {
                "Id | N… | Price",
                "---+----+------",
                " 1 | A… |   1.5",
                "10 | B… | 12.25",
            }, lines);
        }

        [Fact]
        public void Truncates_numeric_columns_only_when_needed()
        {
            var lines = CreateReport(o => o.Width = 12).Bind(Products).GetTextLinesReport();

            Assert.All(lines, line => Assert.True(line.Length <= 12, line));
            Assert.Equal("10 | … | 12…", lines[3]);
        }

        [Fact]
        public void Applies_fixed_width_and_alignment()
        {
            var lines = new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.Name, style =>
                {
                    style.Width = 8;
                    style.Alignment = PlainTextReportAlignment.Center;
                    style.HeaderAlignment = PlainTextReportAlignment.Right;
                });
                options.Columns.AddColumn(m => m.Id, style => style.Alignment = PlainTextReportAlignment.Left);
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "    Name | Id",
                " Apple   | 1",
                " Banana  | 10",
            }, lines);
        }

        [Fact]
        public void Applies_column_format_and_custom_converter()
        {
            var lines = new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = false;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "yyyy-MM-dd");
                options.Columns.AddColumn(m => m.Enabled, style => { }, new YesNoConverter());
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal(new[]
            {
                "2024-01-02 | Yes",
                "2024-12-31 | No",
            }, lines);
        }

        [Fact]
        public void Orders_the_columns_by_index_and_uses_custom_names()
        {
            var lines = new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowColumnSeparator = true;
                options.Columns.AddColumn(2, m => m.Name);
                options.Columns.AddColumn("Code", m => m.Id);
            }).Bind(Products).GetTextLinesReport();

            Assert.Equal("Code | Name", lines[0]);
            Assert.Equal("   1 | Apple", lines[1]);
        }

        [Fact]
        public void Null_values_and_line_breaks_do_not_break_the_layout()
        {
            var products = new[]
            {
                new Product { Id = 1, Name = null, Price = 1m },
                new Product { Id = 2, Name = "a\r\nb\tc", Price = 2m },
            };

            var lines = CreateReport().Bind(products).GetTextLinesReport();

            Assert.Equal(" 1 |       |     1", lines[2]);
            Assert.Equal(" 2 | a b c |     2", lines[3]);
        }

        [Fact]
        public void Renders_only_the_header_when_nothing_is_bound()
        {
            var lines = CreateReport().GetTextLinesReport();

            Assert.Equal(new[]
            {
                "Id | Name | Price",
                "---+------+------",
            }, lines);
        }
    }
}
