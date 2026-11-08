using Mafesoft.PlainTextReport.Conversions;

namespace Mafesoft.PlainTextReport.Tests
{
    public class YesNoConverter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider)
            => value is bool b && b ? "Yes" : "No";
    }
}
