# PlainTextReport

[![NuGet](https://img.shields.io/nuget/v/Mafesoft.PlainTextReport?style=flat-square&label=nuget)](https://www.nuget.org/packages/Mafesoft.PlainTextReport)
[![Downloads](https://img.shields.io/nuget/dt/Mafesoft.PlainTextReport?style=flat-square&label=downloads)](https://www.nuget.org/packages/Mafesoft.PlainTextReport)
[![Build](https://img.shields.io/github/actions/workflow/status/fmazzant/PlainTextReport/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/fmazzant/PlainTextReport/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/fmazzant/PlainTextReport/blob/main/LICENSE)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/fmazzant/PlainTextReport/codeql.yml?branch=main&style=flat-square&label=codeql)](https://github.com/fmazzant/PlainTextReport/actions/workflows/codeql.yml)
[![.NET](https://img.shields.io/badge/.NET-10%20%7C%208%20%7C%20Standard%202.0%20%7C%20Framework%204.6.2%2B-512BD4?style=flat-square)](#)

PlainTextReport is a .NET library to generate a plain text report from a collection of objects. The columns are defined with lambda expressions, as in [TinyCsv](https://github.com/fmazzant/TinyCsv), and the report can be split in pages ready to print, with a header and a footer on every page.

## Why PlainTextReport

| Feature | Description |
|---|---|
| Columns from lambda expressions | `m => m.Price`, also nested (`m => m.Customer.Address.City`) and null safe. See [Columns](#columns) |
| Column caption, width and alignment | The header text, a width in characters or as a percentage of the free space, left, right or center. See [Column style](#column-style) |
| Formats and culture | Format strings (`"N2"`, `"dd/MM/yyyy"`), `IFormatProvider` and the `Culture` option. See [Culture](#culture) |
| Custom converters | `IValueConverter` to print any value as you like. See [Custom value converter](#custom-value-converter) |
| Long values | Truncated with `…` to fit the report width, or wrapped on more lines. See [Width and truncation](#width-and-truncation) and [Multi-line rows](#multi-line-rows) |
| Pages | ISO A paper sizes or custom sizes, portrait or landscape, margins, header and footer with placeholders. See [Pages](#pages) |
| Output | A string, the lines, the pages, a file, a file for every page, a `Stream` or a `TextWriter`, also asynchronously. See [Generate the report](#generate-the-report) |

## Requirements

- .NET 8 or 10 (dedicated builds)
- .NET Framework 4.6.2 or later (4.7.2 and 4.8 use the 4.6.2 build)
- any other platform that supports .NET Standard 2.0: .NET Core 2.0+, .NET 5, 6, 7 and 9, Mono, Xamarin, Unity

.NET Framework 4.5.2 – 4.6.1 is no longer supported since 1.1.0 (out of support since April 2022): use 1.0.0.1.

## Installation

```shell
dotnet add package Mafesoft.PlainTextReport
```

## Usage

Define the model that you want to print:

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

string text = report.Bind(models).GetTextReport();
```

The result looks like this:

```text
               My first plain text report

Id | Name                 | Price | CreatedOn  | Enabled
---+----------------------+-------+------------+--------
 1 | Product 1            |  1.40 | 06/11/2026 |   No
 2 | Product 2            |  2.80 | 05/11/2026 |   No
 3 | Product 3            |  4.20 | 04/11/2026 |   Yes
```

## Options

| Option | Default | Description |
|---|---|---|
| `ShowTitle` | `false` | Prints the title at the top of the report, centered, followed by an empty line. |
| `Title` | | The report title. |
| `MultiLineTitle` | | A title on more lines; when set (and not empty) it is used instead of `Title`. |
| `ShowHeader` | `false` | Prints the header row with the column captions. |
| `ShowHeaderSeparator` | `false` | Prints a separator line below the header. |
| `ShowColumnSeparator` | `false` | Prints a separator between the columns; otherwise the columns are separated by a space. |
| `ColumnSeparator` | `'\|'` | The character used to separate the columns. |
| `HeaderSeparator` | `'-'` | The character used to draw the line below the header. |
| `Width` | `0` | The maximum width of the report, in characters. `0` means no limit. |
| `MultiLineRows` | `false` | Wraps the values longer than their column on more lines, instead of truncating them. See [Multi-line rows](#multi-line-rows). |
| `Culture` | `null` | The culture of the numbers, the dates, the column formats and the `{date}` placeholder. `null` means the invariant culture. See [Culture](#culture). |
| `Page` | `null` | The page settings. `null` means a report without pages. See [Pages](#pages). |
| `NullText` | `null` | The text printed for a `null` value, for example `"-"` or `"n/a"`. `null` means an empty cell. See [Null values](#null-values). |
| `NewLine` | `null` | The line separator, for example `"\r\n"` for a printer or `"\n"` for the Linux tools. `null` means `Environment.NewLine`, so the same report differs on Windows and Linux. Used by `GetTextReport`, `WriteTo`, `WriteToAsync`, `SaveToFile`, `SavePagesToFiles` and `PlainTextReportPage.ToString()`. |

## Columns

A column is defined by a property or a field of the model. `AddColumn` has several overloads, so a column can also specify:

- a name (`columnName`) or a position (`columnIndex`); the columns are printed in `columnIndex` order;
- a format string (`columnFormat`), for example `"dd/MM/yyyy"` for dates or `"N2"` for numbers;
- an `IFormatProvider`;
- a custom `IValueConverter`;
- a style (`PlainTextReportColumnStyle`).

```c#
options.Columns.AddColumn(m => m.Id, style => { });
options.Columns.AddColumn("Product", m => m.Name);
options.Columns.AddColumn(0, "Code", m => m.Id, new CultureInfo("it-IT"));
options.Columns.AddColumn(m => m.Enabled, style => { }, new CustomBooleanConverter());
```

Two columns can have the same `columnIndex`: they are not an error, and they are printed in the order they were added in. The overloads without `columnIndex` use the number of columns already added as index, so mixing them with explicit indexes can give duplicates:

```c#
options.Columns.AddColumn(1, m => m.Name);     // index 1
options.Columns.AddColumn("Code", m => m.Id);  // index 1 too (one column already added): printed after Name
options.Columns.AddColumn(0, m => m.Price);    // index 0: printed first
```

Only members can be selected: `m => m.Price * 2` or `m => m.Name.ToUpper()` throw an `ArgumentException`, use an [`IValueConverter`](#custom-value-converter) instead.

### Nested properties

A column can read a property of a property, at any depth:

```c#
options.Columns.AddColumn(m => m.Customer.Address.City, style => { });
```

- When an intermediate value is `null` (an order without a customer) the cell is empty (or the [null text](#null-values)), without a `NullReferenceException`.
- The default name of a nested column is its path, `Customer.Address.City`. Give it a `columnName` or a [caption](#column-style) to print something else.
- `Nullable<T>` values work too: `m => m.ShippedOn.Value.Year` is empty when `ShippedOn` is `null`.

### Column style

| Property | Default | Description |
|---|---|---|
| `Caption` | `null` | The text printed in the header. When `null` it is the column name (`columnName`, or the name of the property). |
| `Width` | `null` | Width of the column, in characters (`20`) or as percentage of the free space (`"50%"`). When `null` the width fits the header and the longest value. |
| `Alignment` | `null` | `Left`, `Right` or `Center`. When `null` numbers are right aligned and everything else is left aligned. |
| `HeaderAlignment` | `null` | Alignment of the header. When `null` it is the same as `Alignment`. |
| `MultiLine` | `null` | `true` wraps the values of the column on more lines, `false` keeps them on one line. When `null` it is the same as `options.MultiLineRows`. |
| `NullText` | `null` | The text printed for a `null` value of the column. When `null` it is the same as `options.NullText`. See [Null values](#null-values). |

```c#
options.Columns.AddColumn(m => m.Price, style =>
{
    style.Caption = "Prezzo";
    style.Width = 10;
    style.Alignment = PlainTextReportAlignment.Right;
}, columnFormat: "N2");
```

The caption changes only the header: the column name is still used to identify the column.

### Null values

A `null` value is an empty cell. Set `NullText` to print a placeholder instead, for the whole report or for a single column:

```c#
options.NullText = "-";                                                    // whole report
options.Columns.AddColumn(m => m.Name, style => style.NullText = "n/a");  // single column, wins over the report
```

- It is also used when an intermediate value of a nested property is `null` (`m => m.Customer.Name` with `Customer == null`).
- A custom converter still receives the `null` values, so it can print them its own way; when a converter returns `null`, `NullText` is used.
- `style.NullText = ""` keeps the cells of a column empty even when the report has a `NullText`.

### Column width in characters or percentage

`Width` is a `ColumnWidth`: it can be set with an `int` (characters) or with a string (`"20"` characters, `"50%"` percentage). `ColumnWidth.Characters(20)` and `ColumnWidth.Percent(50)` do the same.

```c#
options.Columns.AddColumn(m => m.Id, style => style.Width = 6);          // 6 characters
options.Columns.AddColumn(m => m.Name, style => style.Width = "100%");   // all the free space
options.Columns.AddColumn(m => m.Notes, style => style.Width = "50%");   // half of the free space
```

A percentage is a share of the **free space**: the report width (`options.Width`, or the printable width of the page) minus the column separators and the columns with a fixed or automatic width.

- `100%` takes all the free space, so the column grows until the table fills the report width.
- When the percentages add up to more than 100% they are scaled down proportionally: two columns at `100%` get 50% each, `100%` and `50%` get two thirds and one third.
- Below 100% the rest of the free space stays empty: one column at `50%` takes half of it.
- Without a report width (no `options.Width` and no page settings) there is no free space to share, so a percentage column is sized like an automatic column.

### Width and truncation

When `Width` is set and the columns don't fit, the report shrinks the automatic text columns first, then the automatic numeric columns, and the fixed-width columns last. A value that doesn't fit its column ends with `…`, so a truncated value is always visible.

Line breaks and tabs in the values are replaced by spaces, and trailing spaces are removed from every line.

### Multi-line rows

With `options.MultiLineRows = true` a value longer than its column is not truncated: it wraps on more lines, breaking between the words (a word longer than the column is split). The line breaks of a value start a new line, and the row is as tall as its tallest cell. A single column can turn it on or off with `style.MultiLine`.

```c#
options.MultiLineRows = true;
options.Columns.AddColumn(m => m.Id, style => { });
options.Columns.AddColumn(m => m.Name, style => style.Width = 16);
options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");
```

```text
Id | Name             | Price
---+------------------+------
 1 | Red apples from  |  2.50
   | the Italian Alps |
 2 | Bananas          |  1.20
   | imported         |
```

With pages, the lines of a row are kept on the same page; only a row taller than a whole page continues on the next one.

### Culture

Values are formatted with the invariant culture, so a report is the same on every machine, unless `options.Culture` is set or the column has its own `IFormatProvider`:

```c#
options.Culture = new CultureInfo("it-IT");
options.Columns.AddColumn(m => m.Price, style => { }, columnFormat: "N2");                // 1.234,50
options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd MMMM yyyy");  // 02 gennaio 2024
options.Columns.AddColumn(m => m.UpdatedOn, new CultureInfo("en-US"));                    // the column's provider wins
```

The culture is also used by the `{date}` placeholder of header and footer, and it is passed to the custom converters as `provider`.

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

Without a custom converter a value is printed with the column's format and format provider; `byte[]` values are printed as base64.

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
    options.Columns.AddColumn(m => m.Name, style => style.Width = "100%");
});
```

Every page has exactly the height of the paper, so the footer is always at the bottom:

```text
    Products                                                        07/11/2026
    --------------------------------------------------------------------------
     Id | Name
    ----+---------------------------------------------------------------------
      1 | Product 1
      2 | Product 2
    ...
    --------------------------------------------------------------------------
                                   Page 1 of 3
```

### Paper size and orientation

A plain text page is measured in characters and lines, so the paper size is converted with the printer pitch: `CharactersPerInch` (default `10`) and `LinesPerInch` (default `6`).

The whole ISO 216 A series is available: `PaperSize.A0` ... `PaperSize.A10`, or `PaperSize.A(n)`. The B series is available with `PaperSize.B(n)` (`B0` ... `B10`), and the common `PaperSize.B4` and `PaperSize.B5` as properties. The US sizes are `PaperSize.Letter`, `PaperSize.Legal`, `PaperSize.Tabloid` and `PaperSize.Executive`, and `PaperSize.ContinuousForm` is the 240 mm x 12" fanfold paper of the dot-matrix printers. `PaperSize.Custom(name, width, height)` creates any other size in millimeters; when `name` is `null` or empty the paper is named `Custom`. Each paper can be used in `Portrait` or `Landscape`.

| Paper | Size (mm) | Portrait (chars x lines) | Landscape (chars x lines) |
|---|---|---|---|
| A3 | 297 x 420 | 116 x 99 | 165 x 70 |
| A4 | 210 x 297 | 82 x 70 | 116 x 49 |
| A5 | 148 x 210 | 58 x 49 | 82 x 34 |
| B4 | 250 x 353 | 98 x 83 | 138 x 59 |
| B5 | 176 x 250 | 69 x 59 | 98 x 41 |
| Letter | 215.9 x 279.4 | 85 x 66 | 110 x 51 |
| Legal | 215.9 x 355.6 | 85 x 84 | 140 x 51 |
| Tabloid | 279.4 x 431.8 | 110 x 102 | 170 x 66 |
| Executive | 184.15 x 266.7 | 72 x 63 | 105 x 43 |
| ContinuousForm | 240 x 304.8 | 94 x 72 | 120 x 56 |

Values at 10 characters and 6 lines per inch, margins included. `Width`, `Height`, `PrintableWidth` and `PrintableHeight` of the page settings return the actual values.

### Page settings

| Property | Default | Description |
|---|---|---|
| `PaperSize` | `A4` | The paper size. |
| `Orientation` | `Portrait` | `Portrait` or `Landscape`. |
| `CharactersPerInch` | `10` | Pitch: 10 is pica, 12 is elite, 17 is condensed. |
| `LinesPerInch` | `6` | Line spacing: 6 is the common value, 8 is compressed. |
| `MarginLeft`, `MarginRight` | `0` | Margins, in characters. |
| `MarginTop`, `MarginBottom` | `0` | Margins, in lines. |
| `Header`, `Footer` | empty | The bands printed on every page. |
| `RepeatColumnHeader` | `true` | Prints the column header on every page, not only on the first one. |
| `PageBreak` | `"\f"` | Text put at the beginning of every page after the first one by `GetTextReport` (form feed, so a printer starts a new sheet). Set `""` to have no break character. |
| `Date` | now | Date used by the `{date}` placeholder. When `null` the current date and time is read once per report, so the pages and the file names of `SavePagesToFiles` have the same date. |

The table is fitted in the printable width (or in `options.Width`, if it is smaller). The title is printed only on the first page.

### Header and footer

`Header` and `Footer` have three texts, `Left`, `Center` and `Right`, aligned on the printable width. A text can span more lines with `\n`. `ShowSeparator` draws a line (`SeparatorChar`, default `-`) between the band and the body.

The texts can contain these placeholders, with an optional format after `:`:

| Placeholder | Value | Example |
|---|---|---|
| `{page}` | Page number | `{page}`, `{page:000}` |
| `{pages}` | Number of pages | `Page {page} of {pages}` |
| `{title}` | Report title | `{title}` |
| `{date}` | `Date` of the page settings, `yyyy-MM-dd` by default | `{date:dd/MM/yyyy HH:mm}` |

## Generate the report

Bind the collection and get the report as a single string, as an array of lines or as an array of pages:

```c#
// the whole report; with pages, every page after the first one starts with PageBreak
string text = report.Bind(models).GetTextReport();

// the lines of all the pages
string[] lines = report.Bind(models).GetTextLinesReport();

// the pages: Number, Count and Lines of each page
PlainTextReportPage[] pages = report.Bind(models).GetPagesReport();
```

Without page settings `GetPagesReport` returns a single page with the whole report.

### Save to file

Save the whole report in one file, or every page in its own file:

```c#
// one file; with pages, the pages are separated by PageBreak (form feed by default)
report.Bind(models).SaveToFile("report.txt");

// an absolute path: the directories are created when missing; the absolute path of the file is returned
string file = report.Bind(models).SaveToFile(@"C:\Reports\2026\report.txt");

// a file for every page: page-001.txt, page-002.txt, ...
string[] files = report.Bind(models).SavePagesToFiles("pages");
string[] absoluteFiles = report.Bind(models).SavePagesToFiles(@"C:\Reports\2026\pages");

// a custom file name, with the placeholders {page}, {pages}, {title} and {date}
report.Bind(models).SavePagesToFiles("pages", "{title}-{date:yyyyMMdd}-{page:000}.txt");

// asynchronously, with an optional CancellationToken
await report.Bind(models).SaveToFileAsync("report.txt", cancellationToken: token);
await report.Bind(models).SavePagesToFilesAsync("pages", cancellationToken: token);
```

- The files are written in UTF-8 without BOM, unless an `Encoding` is given (for example `Encoding.GetEncoding(850)` for an old printer; on .NET and .NET Core, not on .NET Framework, register `CodePagesEncodingProvider.Instance` first). With a code page that has no `…`, truncated values end with `?`.
- Paths can be absolute or relative to the current directory; both methods return absolute paths.
- Existing files are overwritten, and missing directories are created.
- The filename pattern must contain `{page}` when there is more than one page; characters that are not valid in a filename are replaced by `_`.
- Without page settings `SavePagesToFiles` writes a single file.

### Write to a stream or a writer

The report can be written to any `Stream` or `TextWriter`, for example the body of an HTTP response, without a file:

```c#
// ASP.NET Core
app.MapGet("/report.txt", async (HttpResponse response) =>
{
    response.ContentType = "text/plain; charset=utf-8";
    await report.Bind(models).WriteToAsync(response.Body);
});

// a MemoryStream, with a different encoding
using var stream = new MemoryStream();
report.Bind(models).WriteTo(stream, Encoding.GetEncoding(850));

// a TextWriter
report.Bind(models).WriteTo(Console.Out);
```

- The text is the same as `GetTextReport()`, followed by a line break, the same as `SaveToFile`.
- The stream and the writer are flushed but not closed.
- The BOM of the encoding, if any, is written only at the beginning of a seekable stream.
- `WriteToAsync` takes an optional `CancellationToken`.

## Sample app

[`samples/PlainTextReportConsole`](samples/PlainTextReportConsole/Program.cs) prints a report without pages and a paged report, with header and footer, on the console:

```shell
dotnet run --project samples/PlainTextReportConsole
```

## Benchmark

The [benchmark](benchmark/PlainTextReportBenchmark) project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure the report generation on 100 to 100,000 rows. Run it in Release mode:

```shell
dotnet run -c Release --project benchmark/PlainTextReportBenchmark
```

## Tests

The tests use [xUnit](https://xunit.net/) and run on .NET 8 and .NET 10:

```shell
dotnet test src/PlainTextReport.sln
```

## License

[MIT](LICENSE)
