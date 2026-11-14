# Changelog

All notable changes to the `Mafesoft.PlainTextReport` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `options.Columns.Add(m => m.Price, column => column.Caption("Price").Format("N2").Width(10)...)`: a fluent column
  builder with every setting of a column in one place (`Caption`, `Index`, `Format`, `FormatProvider`, `Converter`,
  `Width`, `Align`, `HeaderAlign`, `MultiLine`, `NullText`, `Style`), so every combination is possible, for example
  an index with a style or a format with a converter. `Add("Name", m => m.Property, ...)` sets the column name.
  ([#6](https://github.com/fmazzant/PlainTextReport/issues/6))
- `options.ShowTotals` and `options.TotalsCaption`: a totals row at the end of the report, with the aggregate of every
  column set by `column.Total(...)` or `style.Total`: `Sum`, `Count`, `Average`, `Min` or `Max`. The aggregates use the
  values before they are converted to text, `null` values are skipped, and the result is formatted with the format of
  the column in the culture of the report. With pages the totals row is printed at the end of the last page.
  ([#2](https://github.com/fmazzant/PlainTextReport/issues/2))
- Computed columns: `options.Columns.Add("Amount", m => m.Price * m.Quantity)` prints any expression of the model, not only
  a property. The column needs a name; a `NullReferenceException` inside the expression gives an empty cell (or the
  null text), and the alignment and the totals row use the result type of the expression. The obsolete `AddColumn`
  overloads with a column name accept a computed value too. ([#17](https://github.com/fmazzant/PlainTextReport/issues/17))
- `options.MultiLineHeader`: a caption longer than its column wraps on more lines, and a line break in a caption
  (`"Created\non"`) starts a new line. The automatic columns are sized on the longest word of the caption, and with
  pages the header height is counted, also with `RepeatColumnHeader`. ([#4](https://github.com/fmazzant/PlainTextReport/issues/4))

### Changed

- `IPlainTextReportOptions` has the new `ShowTotals`, `TotalsCaption` and `MultiLineHeader` properties: a custom
  implementation of the interface must add them. ([#2](https://github.com/fmazzant/PlainTextReport/issues/2),
  [#4](https://github.com/fmazzant/PlainTextReport/issues/4))
- When a column has both a format and a `CultureInfo` as format provider, the format is applied in that culture;
  before, the format was ignored. ([#6](https://github.com/fmazzant/PlainTextReport/issues/6))
- README, sample app and benchmarks use `Columns.Add`.

### Deprecated

- The `AddColumn` overloads are `[Obsolete]`: they still work, and they will be removed in 2.0
  ([#37](https://github.com/fmazzant/PlainTextReport/issues/37)). Use `Columns.Add` instead.
  ([#6](https://github.com/fmazzant/PlainTextReport/issues/6))

## [1.2.1] - 2026-11-14

### Changed

- `WriteTo`, `WriteToAsync`, `SaveToFile` and `SaveToFileAsync` stream the report instead of building every page in
  memory first: the lines are written while they are composed, one page at a time, and the collection is enumerated
  once. When every column has a fixed width and `{pages}` is not used, the rows are read one at a time while they
  are written, so a report of millions of rows needs little memory. ([#20](https://github.com/fmazzant/PlainTextReport/issues/20))
- README: Support section with a PayPal donation link.

## [1.2.0] - 2026-11-13

### Added

- `options.NullText` and `style.NullText`: the text printed for a `null` value, for the whole report or for a single
  column (the column wins). Also used for a `null` intermediate value of a nested property and when a converter
  returns `null`. ([#3](https://github.com/fmazzant/PlainTextReport/issues/3))
- Paper sizes: the US sizes `PaperSize.Letter`, `Legal`, `Tabloid` and `Executive`, the ISO B series
  (`PaperSize.B(n)`, `B4`, `B5`) and `PaperSize.ContinuousForm` (240 mm x 12", dot-matrix printers).
  ([#5](https://github.com/fmazzant/PlainTextReport/issues/5))
- `options.NewLine`: the line separator, for example `"\r\n"` for a printer or `"\n"` for the Linux tools. `null`
  means `Environment.NewLine`. ([#8](https://github.com/fmazzant/PlainTextReport/issues/8))
- `options.TruncationMarker`: the text at the end of a truncated value, `…` by default; `"~"` or `"..."` for the
  printer code pages without `…`, `""` for no marker. ([#16](https://github.com/fmazzant/PlainTextReport/issues/16))
- `options.UnicodeWidth` (default `true`): see the display width below. `false` counts the UTF-16 code units, as
  before. ([#15](https://github.com/fmazzant/PlainTextReport/issues/15))
- `PlainTextReportException` with `RowNumber`, `ColumnName` and `Value`, thrown when a property getter or a converter
  fails, with the original exception as `InnerException`.
  ([#18](https://github.com/fmazzant/PlainTextReport/issues/18))

### Changed

- Widths, padding, truncation and wrapping use the display width of the text, not `string.Length`: Chinese,
  Japanese and Korean characters and most emoji take 2 columns, combining marks 0, and a character is never cut in
  half. ([#15](https://github.com/fmazzant/PlainTextReport/issues/15))
- The title and the header and footer texts are truncated with the truncation marker too, like the cells.
  ([#16](https://github.com/fmazzant/PlainTextReport/issues/16))
- An exception of a property getter or a converter is wrapped in a `PlainTextReportException` that says the row and
  the column; setting the `Style` of a column to `null` throws an `ArgumentNullException`.
  ([#18](https://github.com/fmazzant/PlainTextReport/issues/18))
- `PaperSize.Custom` with a `null` or empty name is named `Custom`.
  ([#10](https://github.com/fmazzant/PlainTextReport/issues/10))
- `IPlainTextReportOptions` has the new `NewLine`, `NullText`, `TruncationMarker` and `UnicodeWidth` properties: a
  custom implementation of the interface must add them.
- Documentation: two columns with the same index are printed in the order they were added in.
  ([#9](https://github.com/fmazzant/PlainTextReport/issues/9))

### Fixed

- When `Page.Date` is not set, the date is read once per report, so the `{date}` of the pages and the file names of
  `SavePagesToFiles` are the same, also around midnight.
  ([#19](https://github.com/fmazzant/PlainTextReport/issues/19))

## [1.1.0] - 2026-11-12

### Changed

- Target frameworks: `netstandard2.0`, `net462`, `net8.0` and `net10.0`. .NET Framework 4.5.2 – 4.6.1 is no longer
  supported: use 1.0.0.1. ([#21](https://github.com/fmazzant/PlainTextReport/issues/21))
- Badges and workflows aligned to the standard. ([#22](https://github.com/fmazzant/PlainTextReport/issues/22))

## [1.0.0.1] - 2026-11-08

### Changed

- README with improved docs and usage examples, new package icon.

## [1.0.0] - 2026-11-08

- First release.

[1.2.1]: https://github.com/fmazzant/PlainTextReport/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/fmazzant/PlainTextReport/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/fmazzant/PlainTextReport/compare/v1.0.0.1...v1.1.0
[1.0.0.1]: https://github.com/fmazzant/PlainTextReport/compare/v1.0.0...v1.0.0.1
[1.0.0]: https://github.com/fmazzant/PlainTextReport/releases/tag/v1.0.0
