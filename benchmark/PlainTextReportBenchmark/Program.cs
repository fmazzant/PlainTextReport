using BenchmarkDotNet.Running;

// all the benchmarks, or the ones selected by the arguments, for example: -- --filter *Streaming*
_ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args.Length > 0 ? args : new[] { "--filter", "*" });
