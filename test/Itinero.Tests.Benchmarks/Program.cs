using BenchmarkDotNet.Running;
using Itinero.Tests.Benchmarks;

if (args.Length > 0 && args[0] == "profile-allocs")
{
    AllocationProfiler.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
