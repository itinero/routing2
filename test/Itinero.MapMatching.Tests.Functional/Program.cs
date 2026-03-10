using System;
using System.IO;
using System.Threading.Tasks;
using Itinero.MapMatching.Tests.Functional.Domain;
using Newtonsoft.Json;
using Serilog;

namespace Itinero.MapMatching.Tests.Functional;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Fatal()
#if DEBUG
            .MinimumLevel.Fatal()
#endif
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        // all tests.
        var tests = new[]
        {
            Path.Combine("data", "bicycle", "test1.json"),
            Path.Combine("data", "bicycle", "test2.json"),
            Path.Combine("data", "bicycle", "test3.json"),
            Path.Combine("data", "bicycle", "test4.json"),
            Path.Combine("data", "bicycle", "test5.json"),
            Path.Combine("data", "bicycle", "test6.json"),
            Path.Combine("data", "bicycle", "test7.json"),
            Path.Combine("data", "bicycle", "test8.json"),
            Path.Combine("data", "bicycle", "test9.json"),
            Path.Combine("data", "bicycle", "test10.json"),
            Path.Combine("data", "car", "test1.json"),
            Path.Combine("data", "car", "test2.json"),
            Path.Combine("data", "car", "test3.json"),
            Path.Combine("data", "car", "test4.json"),
            Path.Combine("data", "car", "test5.json")
        };

        // run all for them.
        var failed = false;
        foreach (var test in tests)
        {
            // read event template.
            var testData = JsonConvert.DeserializeObject<TestData>(
                await File.ReadAllTextAsync(test));

            Console.Write($"Running {test}");

            // run test
            var result = await testData.RunAsync();
            if (!result.success)
            {
                Console.WriteLine($"...FAIL: {result.message}");
                failed = true;
                continue;
            }
            else
            {
                Console.WriteLine("...OK");
            }
        }

        if (failed) return -1;

        return 0;
    }
}
