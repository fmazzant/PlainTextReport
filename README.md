# PlainTextReport

PlainTextReport is a .NET library to generate a plain text report from a collection of objects.

> **Status:** work in progress (`1.0.0-rc1`). The public API (options, columns, converters) is defined, but the rendering of the report (`GetTextReport` / `GetTextLinesReport`) is not implemented yet and currently returns an empty result.

To use it in your project, add the Mafesoft.PlainTextReport NuGet package to your project.

[![Nuget](https://img.shields.io/nuget/v/Mafesoft.PlainTextReport?style=flat-square)](https://www.nuget.org/packages/Mafesoft.PlainTextReport)
![Nuget](https://img.shields.io/nuget/dt/Mafesoft.PlainTextReport?style=flat-square)

Supported target frameworks: .NET 5, 6, 7, 8 and .NET Framework 4.5.2, 4.6.2, 4.7.2, 4.8.

## Define the model

Define the model that you want to print, like this:

```c#
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool Enabled { get; set; }
}
```

## Define the report

Create a `PlainTextReport<T>` and configure its options and columns:

```c#
using PlainTextReport;

var report = new PlainTextReport<Model>(options =>
{
    // Options
    options.ShowTitle = true;
    options.Title = "My first plain text report";
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;
    options.Width = 80;

    // Columns
    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => { });
    options.Columns.AddColumn(m => m.Price, style => { });
    options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.Enabled, style => { }, new CustomBooleanConverter());
});
```

### Options

| Option | Description |
| --- | --- |
| `ShowTitle` | Prints the title at the top of the report. |
| `Title` | The report title. |
| `MultiLineTitle` | A title split on more lines (`IsMultiLineTitle` is `true` when it is set). |
| `ShowHeader` | Prints the header row with the column names. |
| `ShowHeaderSeparator` | Prints a separator line below the header. |
| `ShowColumnSeparator` | Prints a separator between the columns. |
| `Width` | The total width of the report, in characters. |

### Columns

A column is defined by a property of the model. `AddColumn` has several overloads, so a column can also specify:

- a name (`columnName`) or a position (`columnIndex`);
- a format string (`columnFormat`), for example `"dd/MM/yyyy"` for dates;
- an `IFormatProvider`;
- a custom `IValueConverter`;
- a style (`PlainTextReportColumnStyle`).

## Custom value converter

To control how a value is printed, implement `IValueConverter`:

```c#
using PlainTextReport.Conversions;

public class CustomBooleanConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is bool b && b ? "Yes" : "No";

    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        => value == "Yes";
}
```

Built-in converters are available for the common types (`int`, `long`, `decimal`, `double`, `bool`, `DateTime`, `DateOnly`, `TimeOnly`, `Guid`, `Uri`, enums, ...).

## Generate the report

Bind the collection and get the report as a single string or as an array of lines:

```c#
var text = report
    .Bind(models)
    .GetTextReport();

string[] lines = report
    .Bind(models)
    .GetTextLinesReport();
```

A complete example is available in [samples/PlainTextReportConsole](samples/PlainTextReportConsole/Program.cs).

## Benchmark

The [benchmark](benchmark/PlainTextReportBenchmark) project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure the report generation on 100 to 100,000 rows. Run it in Release mode:

```
dotnet run -c Release --project benchmark/PlainTextReportBenchmark
```

## License

[MIT](LICENSE)
