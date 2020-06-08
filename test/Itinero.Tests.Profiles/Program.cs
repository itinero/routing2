using System;
using System.Collections.Generic;

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
            "/home/pietervdvn/werk/routing-profiles/",
            "/home/pietervdvn/werk/routing-profiles/testbench/src/Profiles.Tests/data/",
            ""
        };

        static void Main(string[] args)
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
                Console.WriteLine($"{argNames[i]}: {argValues[i]}");
            }
        }
    }
}