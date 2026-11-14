using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class ComputedColumnTests
    {
        private static readonly Product[] Products =
        {
            new Product { Id = 2, Name = "Apple", Price = 1.5m },
            new Product { Id = 3, Name = null, Price = 10m },
        };

        private static string[] Render(Action<PlainTextReportOptions<Product>> configure)
        {
            return new PlainTextReport<Product>(options =>
            {
                options.ShowHeader = true;
                options.ShowHeaderSeparator = true;
                options.ShowColumnSeparator = true;
                configure(options);
            }).Bind(Products).GetTextLinesReport();
        }

        [Fact]
        public void A_numeric_expression_is_right_aligned_and_formatted()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add("Amount", m => m.Price * m.Id, column => column.Format("N2"));
            });

            Assert.Equal(new[]
            {
                "Id | Amount",
                "---+-------",
                " 2 |   3.00",
                " 3 |  30.00",
            }, lines);
        }

        [Fact]
        public void A_text_expression_is_left_aligned()
        {
            var lines = Render(o =>
            {
                o.Columns.Add("Label", m => m.Id + " " + m.Name);
                o.Columns.Add(m => m.Id);
            });

            Assert.Equal("2 Apple |  2", lines[2]);
        }

        [Fact]
        public void The_type_of_the_column_is_the_result_type()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.Add("Amount", m => m.Price * m.Id);

            Assert.Equal(typeof(decimal), options.Columns.Single().ColumnType);
            Assert.Equal("Amount", options.Columns.Single().ColumnName);
        }

        [Fact]
        public void A_NullReferenceException_is_an_empty_cell_or_the_null_text()
        {
            var lines = Render(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add("Upper", m => m.Name!.ToUpperInvariant(), column => column.NullText("-"));
            });

            Assert.Equal(" 2 | APPLE", lines[2]);
            Assert.Equal(" 3 | -", lines[3]);
        }

        [Fact]
        public void A_null_nested_value_in_an_expression_is_an_empty_cell()
        {
            var orders = new[] { new Order { Id = 1, Customer = new Customer { Name = "Rossi" } }, new Order { Id = 2 } };
            var lines = new PlainTextReport<Order>(o =>
            {
                o.Columns.Add(m => m.Id);
                o.Columns.Add("Customer", m => m.Customer!.Name + " (" + m.Id + ")");
            }).Bind(orders).GetTextLinesReport();

            Assert.Equal(new[] { "1 Rossi (1)", "2" }, lines);
        }

        [Fact]
        public void Other_exceptions_say_the_row_and_the_column()
        {
            var report = new PlainTextReport<Product>(o =>
            {
                o.Columns.Add("Ratio", m => 10 / (m.Id - 3));
            }).Bind(Products);

            var ex = Assert.Throws<PlainTextReportException>(() => report.GetTextLinesReport());
            Assert.Equal(2, ex.RowNumber);
            Assert.Equal("Ratio", ex.ColumnName);
            Assert.IsType<DivideByZeroException>(ex.InnerException);
        }

        [Fact]
        public void A_computed_value_without_a_name_throws()
        {
            var options = new PlainTextReportOptions<Product>();

            var ex = Assert.Throws<ArgumentException>(() => options.Columns.Add(m => m.Price * m.Id));
            Assert.Contains("needs a name", ex.Message);
            Assert.Throws<ArgumentException>(() => options.Columns.Add(null, m => m.Price * m.Id));
            Assert.Equal(0, options.Columns.Count);
        }

        [Fact]
        public void The_totals_row_uses_the_result_type()
        {
            var lines = Render(o =>
            {
                o.ShowTotals = true;
                o.Columns.Add(m => m.Name);
                o.Columns.Add("Amount", m => m.Price * m.Id, column => column.Format("N2").Total(PlainTextReportTotal.Sum));
            });

            Assert.Equal("Total |  33.00", lines[^1]);
        }

        [Fact]
        public void The_obsolete_overloads_with_a_name_accept_a_computed_value()
        {
            var lines = Render(o => o.Columns.AddColumn("Amount", m => m.Price * m.Id, columnFormat: "N2"));

            Assert.Equal(new[] { "Amount", "------", "  3.00", " 30.00" }, lines);
        }

        [Fact]
        public void A_member_path_keeps_its_getter()
        {
            var options = new PlainTextReportOptions<Order>();
            options.Columns.Add(m => m.Customer!.Name);

            Assert.Equal("Customer.Name", options.Columns.Single().ColumnName);
        }
    }
}
