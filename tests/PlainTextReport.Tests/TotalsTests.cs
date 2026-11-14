using System.Globalization;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class TotalsTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.5m, CreatedOn = new DateTime(2024, 3, 4), Enabled = false },
            new Product { Id = 10, Name = "Banana", Price = 12.25m, CreatedOn = new DateTime(2024, 1, 2), Enabled = true },
        };

        private static string[] Render(IEnumerable<Product> products, Action<PlainTextReportOptions<Product>> configure)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.ShowTotals = true;
                configure(options);
            }).Bind(products).GetTextLinesReport();
        }

        [Fact]
        public void Sum_with_the_format_of_the_column()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Format("N2").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(new[]
            {
                "Name   | Price",
                "-------+------",
                "Apple  |  1.50",
                "Banana | 12.25",
                "-------+------",
                "Total  | 13.75",
            }, lines);
        }

        [Fact]
        public void The_caption_is_printed_in_the_first_column_without_an_aggregate()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Count));
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
                o.Columns.Add(m => m.Enabled);
            });

            // the last cell is empty, like in a row with an empty last value
            Assert.Equal(" 2 | Total  | 13.75 |", lines[^1]);
        }

        [Fact]
        public void Without_a_column_without_aggregate_there_is_no_caption()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Max));
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("10 | 13.75", lines[^1]);
        }

        [Fact]
        public void TotalsCaption_changes_the_caption()
        {
            var lines = Render(Products, o =>
            {
                o.TotalsCaption = "Totale";
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("Totale | 13.75", lines[^1]);
        }

        [Fact]
        public void The_caption_widens_an_automatic_column()
        {
            var lines = Render(Products, o =>
            {
                o.TotalsCaption = "Grand total";
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(new[]
            {
                "         Id | Price",
                "------------+------",
                "          1 |   1.5",
                "         10 | 12.25",
                "------------+------",
                "Grand total | 13.75",
            }, lines);
        }

        [Fact]
        public void The_total_widens_an_automatic_column()
        {
            var many = Enumerable.Range(1, 3).Select(i => new Product { Id = i, Price = 900 }).ToArray();
            var lines = Render(many, o =>
            {
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Caption("P").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("Total | 2700", lines[^1]);
            Assert.Equal("      |  900", lines[2]);
        }

        [Fact]
        public void Average_of_an_integer_column_has_decimals()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Id, column => column.Format("N1").Total(PlainTextReportTotal.Average));
            });

            Assert.Equal("Total  |  5.5", lines[^1]);
        }

        [Fact]
        public void Min_and_max_of_dates_and_texts()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.CreatedOn, column => column.Format("yyyy-MM-dd").Total(PlainTextReportTotal.Min));
                o.Columns.Add(m => m.Name, column => column.Total(PlainTextReportTotal.Max));
            });

            Assert.Equal("2024-01-02 | Banana", lines[^1]);
        }

        [Fact]
        public void Min_and_max_use_the_converter_of_the_column()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Enabled, column => column.Converter(new YesNoConverter()).Total(PlainTextReportTotal.Max));
            });

            Assert.Equal("Total  | Yes", lines[^1]);
        }

        [Fact]
        public void Null_values_are_skipped()
        {
            var orders = new[]
            {
                new Order { Id = 1, ShippedOn = new DateTime(2024, 5, 1) },
                new Order { Id = 2, ShippedOn = null },
                new Order { Id = 3, ShippedOn = new DateTime(2024, 2, 1) },
            };

            var lines = new PlainTextReport<Order>(o =>
            {
                o.ShowTotals = true;
                o.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Sum));
                o.Columns.Add(m => m.ShippedOn, column => column.Format("yyyy-MM-dd").Total(PlainTextReportTotal.Count));
                o.Columns.Add(m => m.Customer!.Name, column => column.Total(PlainTextReportTotal.Count));
            }).Bind(orders).GetTextLinesReport();

            Assert.Equal("6 2          0", lines[^1]);
        }

        [Fact]
        public void Without_rows_the_sum_is_zero_and_the_average_is_the_null_text()
        {
            var lines = Render(Array.Empty<Product>(), o =>
            {
                o.NullText = "-";
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
                o.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Average));
                o.Columns.Add(m => m.CreatedOn, column => column.Total(PlainTextReportTotal.Min));
            });

            Assert.Equal(new[]
            {
                "Name  | Price | Id | CreatedOn",
                "------+-------+----+----------",
                "------+-------+----+----------",
                "Total |     0 |  - | -",
            }, lines);
        }

        [Fact]
        public void Without_the_header_separator_there_is_no_separator_above_the_totals()
        {
            var lines = Render(Products, o =>
            {
                o.ShowHeaderSeparator = false;
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(new[]
            {
                "Name   | Price",
                "Apple  |   1.5",
                "Banana | 12.25",
                "Total  | 13.75",
            }, lines);
        }

        [Fact]
        public void Without_ShowTotals_the_aggregates_are_not_printed()
        {
            var lines = Render(Products, o =>
            {
                o.ShowTotals = false;
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(4, lines.Length);
        }

        [Fact]
        public void The_sum_uses_the_culture_of_the_report()
        {
            var lines = Render(new[] { new Product { Name = "A", Price = 1000m }, new Product { Name = "B", Price = 234.5m } }, o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Format("N2").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("Total | 1.234,50", lines[^1]);
        }

        [Fact]
        public void Fixed_widths_read_the_rows_once_and_still_compute_the_totals()
        {
            var reads = 0;
            IEnumerable<Product> Products()
            {
                for (int i = 1; i <= 3; i++)
                {
                    reads++;
                    yield return new Product { Id = i, Price = i };
                }
            }

            var lines = Render(Products(), o =>
            {
                o.Columns.Add(m => m.Id, column => column.Width(5));
                o.Columns.Add(m => m.Price, column => column.Width(5).Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(3, reads);
            Assert.Equal("Total |     6", lines[^1]);
        }

        [Fact]
        public void A_double_column_sums_as_double()
        {
            var lines = new PlainTextReport<Measure>(o =>
            {
                o.ShowTotals = true;
                o.Columns.Add("Sum", m => m.Value, column => column.Total(PlainTextReportTotal.Sum));
                o.Columns.Add("Average", m => m.Value, column => column.Total(PlainTextReportTotal.Average));
            }).Bind(new[] { new Measure { Value = 0.1 }, new Measure { Value = 0.2 }, new Measure { Value = 1e300 } }).GetTextLinesReport();

            Assert.Equal((0.1 + 0.2 + 1e300).ToString(CultureInfo.InvariantCulture), lines[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);
            Assert.Equal(((0.1 + 0.2 + 1e300) / 3).ToString(CultureInfo.InvariantCulture), lines[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        }

        public class Measure
        {
            public double Value { get; set; }
        }

        [Fact]
        public void Sum_of_a_text_column_throws()
        {
            var report = new PlainTextReport<Product>(o =>
            {
                o.ShowTotals = true;
                o.Columns.Add(m => m.Name, column => column.Total(PlainTextReportTotal.Sum));
            }).Bind(Products);

            var ex = Assert.Throws<InvalidOperationException>(() => report.GetTextLinesReport());
            Assert.Contains("'Name'", ex.Message);
        }

        [Fact]
        public void An_overflow_says_the_row_and_the_column()
        {
            var report = new PlainTextReport<Product>(o =>
            {
                o.ShowTotals = true;
                o.Columns.Add(m => m.Price, column => column.Total(PlainTextReportTotal.Sum));
            }).Bind(new[] { new Product { Price = decimal.MaxValue }, new Product { Price = decimal.MaxValue } });

            var ex = Assert.Throws<PlainTextReportException>(() => report.GetTextLinesReport());
            Assert.Equal(2, ex.RowNumber);
            Assert.Equal("Price", ex.ColumnName);
            Assert.IsType<OverflowException>(ex.InnerException);
        }

        [Fact]
        public void The_obsolete_style_lambda_sets_the_total()
        {
            var lines = Render(Products, o =>
            {
                o.Columns.AddColumn(m => m.Name, style => { });
                o.Columns.AddColumn(m => m.Price, style => style.Total = PlainTextReportTotal.Sum, columnFormat: "N2");
            });

            Assert.Equal("Total  | 13.75", lines[^1]);
        }

        // 2 x 3 inches: portrait at 10 cpi / 4 lpi is 20 characters x 12 lines
        private static PlainTextReportPage[] RenderPages(int count)
        {
            return new PlainTextReport<Item>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                options.ShowTotals = true;
                options.Page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
                {
                    CharactersPerInch = 10,
                    LinesPerInch = 4,
                };
                options.Columns.Add(m => m.Name);
                options.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Count));
            }).Bind(Enumerable.Range(1, count).Select(i => new Item { Id = i, Name = $"N{i}" })).GetPagesReport();
        }

        [Fact]
        public void With_pages_the_totals_are_at_the_end_of_the_last_page()
        {
            var pages = RenderPages(8);

            Assert.Single(pages);
            Assert.Equal("------+---", pages[0].Lines[10]);
            Assert.Equal("Total |  8", pages[0].Lines[11]);
        }

        [Fact]
        public void With_pages_the_totals_row_is_not_split_from_its_separator()
        {
            var pages = RenderPages(9);

            Assert.Equal(2, pages.Length);
            Assert.Equal("N9    |  9", pages[0].Lines[10]);
            Assert.Equal(string.Empty, pages[0].Lines[11]);
            Assert.Equal("Name  | Id", pages[1].Lines[0]);   // RepeatColumnHeader
            Assert.Equal("------+---", pages[1].Lines[2]);
            Assert.Equal("Total |  9", pages[1].Lines[3]);
        }
    }
}
