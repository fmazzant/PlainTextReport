using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PlainTextReport;
using PlainTextReportBenchmark;

_ = BenchmarkRunner.Run<PlainTextReportGenerationBenchmark>();

namespace PlainTextReportBenchmark
{
    public class Model
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool Enabled { get; set; }
    }

    [MemoryDiagnoser]
    public class PlainTextReportGenerationBenchmark
    {
        private Model[] models = Array.Empty<Model>();

        private readonly PlainTextReport<Model> report = new(options =>
        {
            // Options
            options.ShowTitle = true;
            options.Title = "Benchmark report";
            options.ShowHeader = true;
            options.ShowHeaderSeparator = true;
            options.ShowColumnSeparator = true;
            options.Width = 80;

            // Columns
            options.Columns.AddColumn(m => m.Id, style => { });
            options.Columns.AddColumn(m => m.Name, style => { });
            options.Columns.AddColumn(m => m.Price, style => { });
            options.Columns.AddColumn(m => m.CreatedOn, style => { }, columnFormat: "dd/MM/yyyy");
            options.Columns.AddColumn(m => m.Enabled, style => { });
        });

        [Params(100, 1000, 10000, 100000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var createdOn = new DateTime(2024, 1, 1);
            models = Enumerable.Range(1, N)
                .Select(i => new Model
                {
                    Id = i,
                    Name = $"Name {i}",
                    Price = i * 1.12m,
                    CreatedOn = createdOn.AddDays(i % 365),
                    Enabled = i % 2 == 0
                })
                .ToArray();
        }

        [Benchmark]
        public string GetTextReport() => report.Bind(models).GetTextReport();

        [Benchmark]
        public string[] GetTextLinesReport() => report.Bind(models).GetTextLinesReport();
    }
}
