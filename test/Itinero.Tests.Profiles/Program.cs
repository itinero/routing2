using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Tests.Profiles.TestCase;
using Newtonsoft.Json;

namespace Itinero.Tests.Profiles
{
    class Program
    {
        private static List<string> argNames = new List<string>
        {
            "Routing profiles location",
            "Testbench data location",
            "Testfile name must contain"
        };

        private static List<string> argDefaults = new List<string>
        {
            "/home/pietervdvn/werk/AspectedRouting/output/itinero2/",
            "/home/pietervdvn/werk/routing-profiles/testbench/src/Profiles.Tests/data/",
            ""
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Itinero 2.0 Profile Testing Suite");
            Console.WriteLine("=================================\n");

            var argValues = new List<string>();
            argValues.AddRange(argDefaults);
            for (var i = 0; i < args.Length; i++)
            {
                argValues[i] = args[i];
            }

            var routingProfilesDir = argValues[0];
            var testLocationDir = argValues[1];
            var mustContain = argValues[2];

            for (int i = 0; i < argNames.Count; i++)
            {
                var msg = argValues[i];
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "<empty string/no filter given>";
                }

                Console.WriteLine($"{argNames[i]}: {msg}");
            }

            var testcases = FindTestCasePaths(testLocationDir, mustContain);


            var successCount = 0;
            var total = 0;
            var testbench = new TestBench(routingProfilesDir, testLocationDir);
            foreach (var testcase in testcases)
            {
                if (!testcase.test.Profile.Name.StartsWith("bicycle"))
                {
                    // TODO port the other profiles
                    Console.WriteLine("WARNING: Omitting test with profile.behaviour == " + testcase.test.Profile.Name);
                    continue;
                }

                total++;

                if (testcase.test.OsmDataFile.StartsWith("data/"))
                {
                    // Legacy workaround
                    testcase.test.OsmDataFile = testcase.test.OsmDataFile.Substring("data/".Length);
                }

                var (success, message) = await testbench.Run(testcase);
                if (!success)
                {
                    Console.WriteLine("[FAIL] Behaviour: " + testcase.test.Profile.Name +
                                      "\n       " + testcase.test.Description + " \n       " + message);
                }
                else
                {
                    successCount++;
                    Console.WriteLine($"[OK] {successCount}/{total}");
                }
            }

            Console.WriteLine($"{successCount}/{total} tests successful");
        }


        private static IEnumerable<(TestData test, string path)> FindTestCasePaths(string sourceDir, string filter)
        {
            var tests = Directory.EnumerateFiles(sourceDir, "*.json", SearchOption.AllDirectories)
                .ToList();
            tests.Sort();

            var testCountNoFilter = tests.Count;
            if (!string.IsNullOrEmpty(filter))
            {
                Console.WriteLine($"Applying filter with word {filter}");
                tests = tests.Where(w => w.ToLower().Contains(filter)).ToList();
                Console.WriteLine($"Found {tests.Count} matching testcases out of {testCountNoFilter} tests");
            }
            else
            {
                Console.WriteLine($"Found {tests.Count} test cases");
            }

            return tests.Select(path =>
                (JsonConvert.DeserializeObject<TestData>(
                    File.ReadAllText(path)), path));
        }
    }
}