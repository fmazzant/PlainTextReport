using System.Globalization;
using Mafesoft.PlainTextReport;
using Mafesoft.PlainTextReport.Conversions;
using Mafesoft.PlainTextReport.Factory;
using Xunit;

namespace Mafesoft.PlainTextReport.Tests
{
    public class ConverterTests
    {
        [Fact]
        public void ByteArrayConverter_returns_base64()
        {
            Assert.Equal("AQID", new ByteArrayConverter().Convert(new byte[] { 1, 2, 3 }, null!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ByteArrayConverter_returns_null_for_other_values()
        {
            Assert.Null(new ByteArrayConverter().Convert(null!, null!, CultureInfo.InvariantCulture));
            Assert.Null(new ByteArrayConverter().Convert(42, null!, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("en-US", "1.5")]
        [InlineData("it-IT", "1,5")]
        public void DefaultValueConverter_uses_the_format_provider(string culture, string expected)
        {
            Assert.Equal(expected, new DefaultValueConverter().Convert(1.5m, null!, new CultureInfo(culture)));
        }

        [Fact]
        public void DefaultValueConverter_returns_empty_for_null()
        {
            Assert.Equal(string.Empty, new DefaultValueConverter().Convert(null!, null!, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(typeof(byte[]), typeof(ByteArrayConverter))]
        [InlineData(typeof(int), typeof(DefaultValueConverter))]
        [InlineData(typeof(string), typeof(DefaultValueConverter))]
        [InlineData(typeof(DateTime?), typeof(DefaultValueConverter))]
        public void Factory_returns_the_converter_of_the_type(Type valueType, Type converterType)
        {
            Assert.IsType(converterType, ValueConverterFactory.CreateValueConverterByType(valueType));
        }

        [Fact]
        public void Byte_array_columns_are_printed_as_base64()
        {
            var lines = new PlainTextReport<Attachment>(options =>
            {
                options.ShowHeader = true;
                options.Columns.AddColumn(m => m.FileName, style => { });
                options.Columns.AddColumn(m => m.Content, style => { });
            }).Bind(new[]
            {
                new Attachment { FileName = "a.bin", Content = new byte[] { 1, 2, 3 } },
                new Attachment { FileName = "b.bin", Content = null },
            }).GetTextLinesReport();

            Assert.Equal(new[] { "FileName Content", "a.bin    AQID", "b.bin" }, lines);
        }
    }
}
