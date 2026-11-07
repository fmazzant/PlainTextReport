# PlainTextReport

PlainTextReport is a .NET library to generate a plain text report from a collection of objects.

> **Status:** release candidate (`1.0.0-rc1`).

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
using Mafesoft.PlainTextReport;

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
    options.Columns.AddColumn(m => m.Name, style => style.Width = 20);
    options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
    options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.Enabled, style => style.Alignment = PlainTextReportAlignment.Center, new CustomBooleanConverter());
});
```

The result looks like this:

```
               My first plain text report

Id | Name                 | Price | CreatedOn  | Enabled
---+----------------------+-------+------------+--------
 1 | Product 1            |  1.40 | 06/11/2026 |   No
 2 | Product 2            |  2.80 | 05/11/2026 |   No
 3 | Product 3            |  4.20 | 04/11/2026 |   Yes
```

### Options

| Option | Default | Description |
| --- | --- | --- |
| `ShowTitle` | `false` | Prints the title at the top of the report, centered, followed by an empty line. |
| `Title` | | The report title. |
| `MultiLineTitle` | | A title on more lines; when set (and not empty) it is used instead of `Title`. |
| `ShowHeader` | `false` | Prints the header row with the column names. |
| `ShowHeaderSeparator` | `false` | Prints a separator line below the header. |
| `ShowColumnSeparator` | `false` | Prints a separator between the columns; otherwise the columns are separated by a space. |
| `ColumnSeparator` | `'\|'` | The character used to separate the columns. |
| `HeaderSeparator` | `'-'` | The character used to draw the line below the header. |
| `Width` | `0` | The maximum width of the report, in characters. `0` means no limit. |

### Columns

A column is defined by a property of the model. `AddColumn` has several overloads, so a column can also specify:

- a name (`columnName`) or a position (`columnIndex`); the columns are printed in `columnIndex` order;
- a format string (`columnFormat`), for example `"dd/MM/yyyy"` for dates or `"N2"` for numbers;
- an `IFormatProvider`;
- a custom `IValueConverter`;
- a style (`PlainTextReportColumnStyle`).

Values are formatted with the invariant culture, unless an `IFormatProvider` is given.

### Column style

| Property | Default | Description |
| --- | --- | --- |
| `Width` | `null` | Fixed width of the column. When `null` the width fits the header and the longest value. |
| `Alignment` | `null` | `Left`, `Right` or `Center`. When `null` numbers are right aligned and everything else is left aligned. |
| `HeaderAlignment` | `null` | Alignment of the header. When `null` it is the same as `Alignment`. |

### Width and truncation

When `Width` is set and the columns don't fit, the report shrinks the automatic text columns first, then the automatic numeric columns, and the fixed-width columns last. A value that doesn't fit its column ends with `…`, so a truncated value is always visible.

Line breaks and tabs in the values are replaced by spaces, and trailing spaces are removed from every line.

## Custom value converter

To control how a value is printed, implement `IValueConverter`:

```c#
using Mafesoft.PlainTextReport.Conversions;

public class CustomBooleanConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is bool b && b ? "Yes" : "No";
}
```

Without a custom converter a value is printed with the column's format and format provider; `byte[]` values are printed as Base64.

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

## Tests

```
dotnet test src/PlainTextReport.sln
```

## Benchmark

The [benchmark](benchmark/PlainTextReportBenchmark) project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure the report generation on 100 to 100,000 rows. Run it in Release mode:

```
dotnet run -c Release --project benchmark/PlainTextReportBenchmark
```

## License

[MIT](LICENSE)
