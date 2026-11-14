using System.Globalization;
using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class GroupingTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 1, Name = "Apple", Price = 1.50m, Category = "Fruit" },
            new Product { Id = 10, Name = "Banana", Price = 12.25m, Category = "Fruit" },
            new Product { Id = 4, Name = "Carrot", Price = 0.80m, Category = "Vegetables" },
        };

        private static string[] Render(IEnumerable<Product> products, Action<PlainTextReportOptions<Product>> configure)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                configure(options);
            }).Bind(products).GetTextLinesReport();
        }

        [Fact]
        public void Groups_have_a_header_and_subtotals_and_the_report_ends_with_the_totals()
        {
            var lines = Render(Products, o =>
            {
                o.ShowTotals = true;
                o.GroupBy(m => m.Category, group =>
                {
                    group.Header = "Category: {key}";
                    group.ShowSubtotals = true;
                });
                o.Columns.Add(m => m.Id);
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Format("N2").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal(new[]
            {
                "Category: Fruit",
                "    1 | Apple  |  1.50",
                "   10 | Banana | 12.25",
                "      |        | 13.75",
                "",
                "Category: Vegetables",
                "    4 | Carrot |  0.80",
                "      |        |  0.80",
                "------+--------+------",
                "Total |        | 14.55",
            }, lines);
        }

        [Fact]
        public void The_column_header_is_printed_once_and_the_default_header_is_the_key()
        {
            var lines = Render(Products, o =>
            {
                o.ShowHeader = true;
                o.GroupBy(m => m.Category);
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Format("N2"));
            });

            Assert.Equal(new[]
            {
                "Name   | Price",
                "-------+------",
                "Fruit",
                "Apple  |  1.50",
                "Banana | 12.25",
                "",
                "Vegetables",
                "Carrot |  0.80",
            }, lines);
        }

        [Fact]
        public void The_groups_follow_the_order_of_the_collection_without_sorting_it()
        {
            var products = new[]
            {
                new Product { Name = "Banana", Category = "Fruit" },
                new Product { Name = "Carrot", Category = "Vegetables" },
                new Product { Name = "Apple", Category = "Fruit" },
            };

            var lines = Render(products, o =>
            {
                o.GroupBy(m => m.Category);
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal(new[] { "Fruit", "Banana", "", "Vegetables", "Carrot", "", "Fruit", "Apple" }, lines);
        }

        [Fact]
        public void The_key_can_be_formatted_in_the_culture_of_the_report()
        {
            var products = new[]
            {
                new Product { Name = "Apple", CreatedOn = new DateTime(2026, 11, 3) },
                new Product { Name = "Banana", CreatedOn = new DateTime(2026, 12, 1) },
            };

            var lines = Render(products, o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.GroupBy(m => m.CreatedOn.Month, group => group.Header = "Month {KEY:00}");
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal(new[] { "Month 11", "Apple", "", "Month 12", "Banana" }, lines);
        }

        [Fact]
        public void A_date_key_with_a_format()
        {
            var products = new[] { new Product { Name = "Apple", CreatedOn = new DateTime(2026, 11, 3) } };

            var lines = Render(products, o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.GroupBy(m => m.CreatedOn, group => group.Header = "{key:MMMM yyyy}");
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal("novembre 2026", lines[0]);
        }

        [Fact]
        public void A_null_key_is_printed_as_the_null_text()
        {
            var products = new[] { new Product { Name = "Apple" } };

            var lines = Render(products, o =>
            {
                o.NullText = "n/a";
                o.GroupBy(m => m.Category, group => group.Header = "Category: {key}");
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal(new[] { "Category: n/a", "Apple" }, lines);
        }

        [Fact]
        public void Subtotals_without_the_totals_row_use_the_aggregates_of_the_columns()
        {
            var lines = Render(Products, o =>
            {
                o.GroupBy(m => m.Category, group => group.ShowSubtotals = true);
                o.Columns.Add(m => m.Name, column => column.Total(PlainTextReportTotal.Count));
                o.Columns.Add(m => m.Price, column => column.Format("N2").Total(PlainTextReportTotal.Max));
            });

            Assert.Equal(new[]
            {
                "Fruit",
                "Apple  |  1.50",
                "Banana | 12.25",
                "2      | 12.25",
                "",
                "Vegetables",
                "Carrot |  0.80",
                "1      |  0.80",
            }, lines);
        }

        [Fact]
        public void A_subtotal_widens_an_automatic_column()
        {
            var products = Enumerable.Range(1, 3).Select(i => new Product { Id = i, Price = 900, Category = "A" }).ToArray();

            var lines = Render(products, o =>
            {
                o.ShowHeader = true;
                o.GroupBy(m => m.Category, group => group.ShowSubtotals = true);
                o.Columns.Add(m => m.Name);
                o.Columns.Add(m => m.Price, column => column.Caption("P").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("     |  900", lines[3]);
            Assert.Equal("     | 2700", lines[^1]);
        }

        [Fact]
        public void A_group_header_longer_than_the_report_width_is_truncated()
        {
            var lines = Render(Products, o =>
            {
                o.Width = 10;
                o.GroupBy(m => m.Category, group => group.Header = "Category: {key}");
                o.Columns.Add(m => m.Name);
            });

            Assert.Contains("Category:…", lines);
        }

        [Fact]
        public void GroupBy_stores_the_settings_and_a_new_call_replaces_them()
        {
            var options = new PlainTextReportOptions<Product>();
            Assert.Null(options.Group);

            options.GroupBy(m => m.Category, group => group.ShowSubtotals = true);
            options.GroupBy(m => m.Id, group => group.Header = "Id {key}");

            var group = Assert.IsType<PlainTextReportGroup>(options.Group);
            Assert.Equal("Id {key}", group.Header);
            Assert.False(group.ShowSubtotals);
            Assert.False(group.NewPage);
        }

        [Fact]
        public void GroupBy_without_a_key_throws()
        {
            var options = new PlainTextReportOptions<Product>();

            Assert.Throws<ArgumentNullException>(() => options.GroupBy<string>(null!));
        }

        [Fact]
        public void Subtotals_check_the_aggregates_of_the_columns()
        {
            Assert.Throws<InvalidOperationException>(() => Render(Products, o =>
            {
                o.GroupBy(m => m.Category, group => group.ShowSubtotals = true);
                o.Columns.Add(m => m.Name, column => column.Total(PlainTextReportTotal.Sum));
            }));
        }

        [Fact]
        public void A_key_that_throws_gives_the_row()
        {
            var ex = Assert.Throws<PlainTextReportException>(() => Render(Products, o =>
            {
                o.GroupBy(m => m.Id == 10 ? throw new InvalidOperationException("bad key") : m.Category);
                o.Columns.Add(m => m.Name);
            }));

            Assert.Equal(2, ex.RowNumber);
            Assert.Contains("group key", ex.Message);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        // 2 x 3 inches: portrait at 10 cpi / 4 lpi is 20 characters x 12 lines
        private static PlainTextReportPage[] RenderPages(int countA, int countB, Action<PlainTextReportOptions<Item>>? configure = null)
        {
            var items = Enumerable.Range(1, countA).Select(i => new Item { Id = i, Name = "A" })
                .Concat(Enumerable.Range(countA + 1, countB).Select(i => new Item { Id = i, Name = "B" }));

            return new PlainTextReport<Item>(options =>
            {
                options.ShowColumnSeparator = true;
                options.Page = new PlainTextReportPageSettings(PaperSize.Custom("Test", 25.4 * 2, 25.4 * 3))
                {
                    CharactersPerInch = 10,
                    LinesPerInch = 4,
                };
                options.GroupBy(m => m.Name, group => group.Header = "Group {key}");
                configure?.Invoke(options);
                if (!options.Columns.Any())
                {
                    options.Columns.Add(m => m.Id);
                    options.Columns.Add(m => m.Name);
                }
            }).Bind(items).GetPagesReport();
        }

        [Fact]
        public void With_pages_the_empty_line_and_the_header_of_a_group_are_kept_when_its_first_row_fits()
        {
            var pages = RenderPages(8, 1);

            Assert.Single(pages);
            Assert.Equal("8 | A", pages[0].Lines[8]);
            Assert.Equal(string.Empty, pages[0].Lines[9]);
            Assert.Equal("Group B", pages[0].Lines[10]);
            Assert.Equal("9 | B", pages[0].Lines[11]);
        }

        [Fact]
        public void With_pages_a_group_header_is_not_left_alone_at_the_bottom_of_a_page()
        {
            var pages = RenderPages(9, 1);

            Assert.Equal(2, pages.Length);
            Assert.Equal(" 9 | A", pages[0].Lines[9]);
            Assert.Equal(string.Empty, pages[0].Lines[10]);
            Assert.Equal(string.Empty, pages[0].Lines[11]);
            // the empty line before the group is dropped at the top of the page
            Assert.Equal("Group B", pages[1].Lines[0]);
            Assert.Equal("10 | B", pages[1].Lines[1]);
        }

        [Fact]
        public void With_pages_the_subtotals_and_the_column_header_are_paginated_with_the_groups()
        {
            var pages = RenderPages(9, 2, o =>
            {
                o.ShowHeader = true;
                o.ShowHeaderSeparator = true;
                o.GroupBy(m => m.Name, group =>
                {
                    group.Header = "Group {key}";
                    group.ShowSubtotals = true;
                });
                o.Columns.Add(m => m.Id, column => column.Total(PlainTextReportTotal.Count));
                o.Columns.Add(m => m.Name);
            });

            Assert.Equal(2, pages.Length);
            Assert.Equal(" 9 | A", pages[0].Lines[11]);
            Assert.Equal(new[] { "Id | Name", "---+-----", " 9 |", string.Empty, "Group B", "10 | B", "11 | B", " 2 |" },
                pages[1].Lines.Take(8));
        }

        [Fact]
        public void With_NewPage_every_group_after_the_first_starts_a_new_page()
        {
            var pages = RenderPages(2, 1, o => o.GroupBy(m => m.Name, group =>
            {
                group.Header = "Group {key}";
                group.NewPage = true;
            }));

            Assert.Equal(2, pages.Length);
            Assert.Equal("Group A", pages[0].Lines[0]);
            Assert.Equal("2 | A", pages[0].Lines[2]);
            Assert.Equal(string.Empty, pages[0].Lines[3]);
            Assert.Equal("Group B", pages[1].Lines[0]);
            Assert.Equal("3 | B", pages[1].Lines[1]);
        }

        [Fact]
        public void With_pages_the_page_count_includes_the_groups()
        {
            var pages = RenderPages(9, 1, o => o.Page.Footer.Right = "{page}/{pages}");

            Assert.Equal(2, pages.Length);
            Assert.EndsWith("1/2", pages[0].Lines[^1]);
            Assert.EndsWith("2/2", pages[1].Lines[^1]);
            Assert.Equal("Group B", pages[1].Lines[0]);
        }
    }
}
