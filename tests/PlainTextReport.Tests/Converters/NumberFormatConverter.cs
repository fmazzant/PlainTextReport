using Mafesoft.PlainTextReport.Conversions;

namespace Mafesoft.PlainTextReport.Tests
{
    /// <summary>
    /// A converter that formats the numbers itself, with the format provider it receives
    /// </summary>
    public class NumberFormatConverter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider)
            => value is decimal number ? "€ " + number.ToString("N2", provider) : string.Empty;
    }
}
