using BenchmarkDotNet.Attributes;
using Mafesoft.PlainTextReport;

namespace PlainTextReportBenchmark
{
    [MemoryDiagnoser]
    public class PlainTextReportGenerationBenchmark
    {
        private BenchmarkRow[] rows = Array.Empty<BenchmarkRow>();

        private readonly PlainTextReport<BenchmarkRow> report = new(CreateOptions());

        [Params(100, 1000, 10000, 100000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var createdOn = new DateTime(2024, 1, 1);
            rows = Enumerable.Range(1, N)
                .Select(i => new BenchmarkRow(i, $"Name {i}", i * 1.12m, createdOn.AddDays(i % 365), i % 2 == 0))
                .ToArray();
        }

        [Benchmark]
        public string GetTextReport() => report.Bind(rows).GetTextReport();

        [Benchmark]
        public string[] GetTextLinesReport() => report.Bind(rows).GetTextLinesReport();

        private static PlainTextReportOptions<BenchmarkRow> CreateOptions()
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
            options.Columns.AddColumn(m => m.Id, style => { });
            options.Columns.AddColumn(m => m.Name, style => { });
            options.Columns.AddColumn(m => m.Price, style => { });
            options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
            options.Columns.AddColumn(m => m.Enabled, style => { });
            return options;
        }
    }
}
