using Mafesoft.PlainTextReport;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class NestedPropertyTests
    {
        private static readonly Order[] Orders =
        {
            new Order { Id = 1, Code = "A1", Customer = new Customer { Name = "Mario", Address = new Address { City = "Roma", ZipCode = 100 } }, ShippedOn = new DateTime(2026, 3, 4) },
            new Order { Id = 2, Code = "B2", Customer = new Customer { Name = "Anna" } },
            new Order { Id = 3, Code = "C3" },
        };

        private static string[] Render(Action<PlainTextReportOptions<Order>> columns)
        {
            return new PlainTextReport<Order>(options =>
            {
                options.ShowHeader = true;
                options.ShowColumnSeparator = true;
                columns(options);
            }).Bind(Orders).GetTextLinesReport();
        }

        [Fact]
        public void Reads_a_nested_property()
        {
            var lines = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Customer!.Name, style => { });
            });

            Assert.Equal(new[] { "Id | Customer.Name", " 1 | Mario", " 2 | Anna", " 3 |" }, lines);
        }

        [Fact]
        public void A_null_intermediate_value_is_an_empty_cell()
        {
            var lines = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn(m => m.Customer!.Address!.City, style => { });
                o.Columns.AddColumn(m => m.Customer!.Address!.ZipCode, style => { });
            });

            string[] Cells(string line) => line.Split('|').Select(cell => cell.Trim()).ToArray();

            Assert.Equal(new[] { "Id", "Customer.Address.City", "Customer.Address.ZipCode" }, Cells(lines[0]));
            Assert.Equal(new[] { "1", "Roma", "100" }, Cells(lines[1]));
            Assert.Equal(new[] { "2", "", "" }, Cells(lines[2]));
            Assert.Equal(new[] { "3", "", "" }, Cells(lines[3]));
        }

        [Fact]
        public void Reads_through_a_nullable_value()
        {
            var lines = Render(o =>
            {
                o.Columns.AddColumn(m => m.Id, style => { });
                o.Columns.AddColumn("Year", m => m.ShippedOn!.Value.Year);
            });

            Assert.Equal(new[] { "Id | Year", " 1 | 2026", " 2 |", " 3 |" }, lines);
        }

        [Fact]
        public void Reads_a_field()
        {
            var lines = Render(o => o.Columns.AddColumn(m => m.Code, style => { }));

            Assert.Equal(new[] { "Code", "A1", "B2", "C3" }, lines);
        }

        [Fact]
        public void The_column_name_can_be_changed()
        {
            var lines = Render(o => o.Columns.AddColumn("City", m => m.Customer!.Address!.City));

            Assert.Equal(new[] { "City", "Roma" }, lines.Take(2));
        }

        [Fact]
        public void Rejects_an_expression_that_is_not_a_member()
        {
            var options = new PlainTextReportOptions<Order>();

            Assert.Throws<ArgumentException>(() => options.Columns.AddColumn(m => m.Id + 1, style => { }));
            Assert.Throws<ArgumentException>(() => options.Columns.AddColumn(m => m.Customer!.Name!.ToUpper(), style => { }));
            Assert.Throws<ArgumentException>(() => options.Columns.AddColumn(m => Orders[0].Id, style => { }));
        }

        [Fact]
        public void Rejects_a_null_expression()
        {
            var options = new PlainTextReportOptions<Order>();

            Assert.Throws<ArgumentNullException>(() => options.Columns.AddColumn<int>(null!, style => { }));
        }

        [Fact]
        public void A_boxed_member_is_read()
        {
            var lines = Render(o => o.Columns.AddColumn(m => (object)m.Id, style => { }));

            Assert.Equal(new[] { "Id", "1", "2", "3" }, lines);
        }
    }
}
