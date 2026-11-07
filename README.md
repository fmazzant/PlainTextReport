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

## Pages

Set `options.Page` to split the report in pages ready to print, with a header and a footer on every page:

```c#
var report = new PlainTextReport<Model>(options =>
{
    options.Title = "Products";
    options.ShowHeader = true;
    options.ShowHeaderSeparator = true;
    options.ShowColumnSeparator = true;

    options.Page = new PlainTextReportPageSettings(PaperSize.A4, PageOrientation.Portrait)
    {
        MarginLeft = 4,
        MarginRight = 4,
        MarginTop = 2,
        MarginBottom = 2,
    };
    options.Page.Header.Left = "{title}";
    options.Page.Header.Right = "{date:dd/MM/yyyy}";
    options.Page.Header.ShowSeparator = true;
    options.Page.Footer.Center = "Page {page} of {pages}";
    options.Page.Footer.ShowSeparator = true;

    options.Columns.AddColumn(m => m.Id, style => { });
    options.Columns.AddColumn(m => m.Name, style => { });
});
```

Every page has exactly the height of the paper, so the footer is always at the bottom:

```
    Products                                                        07/11/2026
    --------------------------------------------------------------------------
     Id | Name
    ----+-------------
      1 | Product 1
      2 | Product 2
    ...
    --------------------------------------------------------------------------
                                   Page 1 of 3
```

### Paper size and orientation

A plain text page is measured in characters and lines, so the paper size is converted with the printer pitch: `CharactersPerInch` (default `10`) and `LinesPerInch` (default `6`).

The whole ISO 216 A series is available: `PaperSize.A0` ... `PaperSize.A10`, or `PaperSize.A(n)`. `PaperSize.Custom(name, width, height)` creates any other size in millimeters. Each paper can be used in `Portrait` or `Landscape`.

| Paper | Size (mm) | Portrait (chars x lines) | Landscape (chars x lines) |
| --- | --- | --- | --- |
| A3 | 297 x 420 | 116 x 99 | 165 x 70 |
| A4 | 210 x 297 | 82 x 70 | 116 x 49 |
| A5 | 148 x 210 | 58 x 49 | 82 x 34 |

Values at 10 characters and 6 lines per inch, margins included. `Width`, `Height`, `PrintableWidth` and `PrintableHeight` of the page settings return the actual values.

### Page settings

| Property | Default | Description |
| --- | --- | --- |
| `PaperSize` | `A4` | The paper size. |
| `Orientation` | `Portrait` | `Portrait` or `Landscape`. |
| `CharactersPerInch` | `10` | Pitch: 10 is pica, 12 is elite, 17 is condensed. |
| `LinesPerInch` | `6` | Line spacing: 6 is the common value, 8 is compressed. |
| `MarginLeft`, `MarginRight` | `0` | Margins, in characters. |
| `MarginTop`, `MarginBottom` | `0` | Margins, in lines. |
| `Header`, `Footer` | empty | The bands printed on every page. |
| `RepeatColumnHeader` | `true` | Prints the column header on every page, not only on the first one. |
| `PageBreak` | `"\f"` | Text put at the beginning of every page after the first one by `GetTextReport` (form feed, so a printer starts a new sheet). Set `""` to have no break character. |
| `Date` | now | Date used by the `{date}` placeholder. |

The table is fitted in the printable width (or in `options.Width`, if it is smaller). The title is printed only on the first page.

### Header and footer

`Header` and `Footer` have three texts, `Left`, `Center` and `Right`, aligned on the printable width. A text can span more lines with `\n`. `ShowSeparator` draws a line (`SeparatorChar`, default `-`) between the band and the body.

The texts can contain these placeholders, with an optional format after `:`:

| Placeholder | Value | Example |
| --- | --- | --- |
| `{page}` | Page number | `{page}`, `{page:000}` |
| `{pages}` | Number of pages | `Page {page} of {pages}` |
| `{title}` | Report title | `{title}` |
| `{date}` | `Date` of the page settings, `yyyy-MM-dd` by default | `{date:dd/MM/yyyy HH:mm}` |

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

Bind the collection and get the report as a single string, as an array of lines or as an array of pages:

```c#
// the whole report; with pages, every page after the first one starts with PageBreak
var text = report
    .Bind(models)
    .GetTextReport();

// the lines of all the pages
string[] lines = report
    .Bind(models)
    .GetTextLinesReport();

// the pages: Number, Count and Lines of each page
PlainTextReportPage[] pages = report
    .Bind(models)
    .GetPagesReport();
```

Without page settings `GetPagesReport` returns a single page with the whole report.

A complete example, with and without pages, is available in [samples/PlainTextReportConsole](samples/PlainTextReportConsole/Program.cs).

## Tests

```
dotnet test src/PlainTextReport.sln
```

## Release

The package is published on NuGet by the [nuget workflow](.github/workflows/nuget.yml) when a tag `v*` is pushed. The package version is the tag without the `v`:

```
git tag v1.0.0-rc2
git push origin v1.0.0-rc2
```

## Benchmark

The [benchmark](benchmark/PlainTextReportBenchmark) project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure the report generation on 100 to 100,000 rows. Run it in Release mode:

```
dotnet run -c Release --project benchmark/PlainTextReportBenchmark
```

## License

[MIT](LICENSE)
