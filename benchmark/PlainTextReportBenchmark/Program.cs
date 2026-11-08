using BenchmarkDotNet.Running;
using PlainTextReportBenchmark;

_ = BenchmarkRunner.Run<PlainTextReportGenerationBenchmark>();
