using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Mafesoft.PlainTextReport;

namespace PlainTextReportBenchmark
{
    /// <summary>
    /// Writes a report of one million rows to a stream that discards the bytes, to measure the allocations:
    /// with automatic widths only the cells are kept in memory, with fixed widths the rows are read while they are written.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 3)]
    public class PlainTextReportStreamingBenchmark
    {
        private const int Rows = 1_000_000;

        private readonly PlainTextReport<BenchmarkRow> automaticWidths = new(CreateOptions(fixedWidths: false, paged: false));
        private readonly PlainTextReport<BenchmarkRow> fixedWidths = new(CreateOptions(fixedWidths: true, paged: false));
        private readonly PlainTextReport<BenchmarkRow> fixedWidthsPaged = new(CreateOptions(fixedWidths: true, paged: true));

        [Benchmark(Baseline = true)]
        public string GetTextReport() => automaticWidths.Bind(CreateRows()).GetTextReport();

        [Benchmark]
        public void WriteTo_AutomaticWidths() => automaticWidths.Bind(CreateRows()).WriteTo(Stream.Null);

        [Benchmark]
        public void WriteTo_FixedWidths() => fixedWidths.Bind(CreateRows()).WriteTo(Stream.Null);

        [Benchmark]
        public void WriteTo_FixedWidths_Paged() => fixedWidthsPaged.Bind(CreateRows()).WriteTo(Stream.Null);

        /// <summary>
        /// The rows are created while they are read, as from a database reader
        /// </summary>
        private static IEnumerable<BenchmarkRow> CreateRows()
        {
            var createdOn = new DateTime(2024, 1, 1);
            for (int i = 1; i <= Rows; i++)
            {
                yield return new BenchmarkRow(i, $"Name {i}", i * 1.12m, createdOn.AddDays(i % 365), i % 2 == 0);
            }
        }

        private static PlainTextReportOptions<BenchmarkRow> CreateOptions(bool fixedWidths, bool paged)
        {
            var options = new PlainTextReportOptions<BenchmarkRow>
            {
                ShowTitle = true,
                Title = "Benchmark report",
                ShowHeader = true,
                ShowHeaderSeparator = true,
                ShowColumnSeparator = true,
                Width = 80,
            };
            if (paged)
            {
                options.Page = new PlainTextReportPageSettings(PaperSize.A4)
                {
                    RepeatColumnHeader = true,
                    Date = new DateTime(2026, 1, 1),
                };
                options.Page.Footer.Center = "{page}";
            }
            options.Columns.Add(m => m.Id, column => column.Style(style => { if (fixedWidths) style.Width = 8; }));
            options.Columns.Add(m => m.Name, column => column.Style(style => { if (fixedWidths) style.Width = "100%"; }));
            options.Columns.Add(m => m.Price, column => column.Style(style => { if (fixedWidths) style.Width = 12; }));
            options.Columns.Add(m => m.CreatedOn, column => column.Format("dd/MM/yyyy").Style(style => { if (fixedWidths) style.Width = 10; }));
            options.Columns.Add(m => m.Enabled, column => column.Style(style => { if (fixedWidths) style.Width = 7; }));
            return options;
        }
    }
}
