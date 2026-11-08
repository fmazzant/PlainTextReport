namespace PlainTextReportBenchmark
{
    /// <summary>
    /// A row of the benchmark report
    /// </summary>
    public sealed record BenchmarkRow(int Id, string Name, decimal Price, DateTime CreatedOn, bool Enabled);
}
