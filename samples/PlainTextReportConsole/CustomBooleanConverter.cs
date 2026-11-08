using Mafesoft.PlainTextReport.Conversions;

public class CustomBooleanConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is bool boolValue && boolValue ? "Yes" : "No";
}
