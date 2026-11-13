using Mafesoft.PlainTextReport;
using Mafesoft.PlainTextReport.Conversions;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class ErrorTests
    {
        private class FailingConverter : IValueConverter
        {
            public string Convert(object value, object parameter, IFormatProvider provider)
                => value is decimal price && price < 0 ? throw new FormatException("Input string was not in a correct format.") : value?.ToString() ?? "";
        }

        private class Faulty
        {
            public int Id { get; set; }
            public string Name => Id == 3 ? throw new InvalidOperationException("Broken name") : "N" + Id;
        }

        [Fact]
        public void A_converter_that_throws_says_the_row_and_the_column()
        {
            var products = Enumerable.Range(1, 5).Select(i => new Product { Id = i, Price = i == 4 ? -1m : i }).ToArray();
            var report = new PlainTextReport<Product>(options =>
            {
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(0, "Price", m => m.Price, new FailingConverter());
            }).Bind(products);

            var ex = Assert.Throws<PlainTextReportException>(() => report.GetTextReport());

            Assert.Equal("Cannot convert the value of column 'Price' at row 4: Input string was not in a correct format.", ex.Message);
            Assert.Equal(4, ex.RowNumber);
            Assert.Equal("Price", ex.ColumnName);
            Assert.Equal(-1m, ex.Value);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        [Fact]
        public void A_getter_that_throws_says_the_row_and_the_column()
        {
            var items = Enumerable.Range(1, 5).Select(i => new Faulty { Id = i }).ToArray();
            var report = new PlainTextReport<Faulty>(options =>
            {
                options.Columns.AddColumn(m => m.Id, style => { });
                options.Columns.AddColumn(m => m.Name, style => { });
            }).Bind(items);

            var ex = Assert.Throws<PlainTextReportException>(() => report.GetTextLinesReport());

            Assert.Equal("Cannot read the value of column 'Name' at row 3: Broken name", ex.Message);
            Assert.Equal(3, ex.RowNumber);
            Assert.Equal("Name", ex.ColumnName);
            Assert.Null(ex.Value);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public void The_writing_methods_throw_the_same_exception()
        {
            var report = new PlainTextReport<Faulty>(options => options.Columns.AddColumn(m => m.Name, style => { }))
                .Bind(new[] { new Faulty { Id = 3 } });

            Assert.Throws<PlainTextReportException>(() => report.WriteTo(new StringWriter()));
            Assert.Throws<PlainTextReportException>(() => report.GetPagesReport());
        }

        [Fact]
        public void The_style_of_a_column_cannot_be_null()
        {
            var options = new PlainTextReportOptions<Product>();
            options.Columns.AddColumn(m => m.Price, style => { });
            var column = options.Columns.Single();

            var ex = Assert.Throws<ArgumentNullException>(() => column.Style = null!);

            Assert.Contains("'Price'", ex.Message);
            Assert.NotNull(column.Style);
        }
    }
}
