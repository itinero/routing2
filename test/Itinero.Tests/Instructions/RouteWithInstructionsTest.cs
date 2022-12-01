using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Itinero.Instructions;
using Itinero.Instructions.Types;
using Itinero.Network.Attributes;
using Itinero.Tests.Network.Enumerators.Edges;
using Xunit;

namespace Itinero.Tests.Instructions;

internal static class AttributeExtension
{
    public static void AssertTag(this IEnumerable<(string key, string value)> attributes, string key,
        string expectedValue)
    {
        if (!attributes.TryGetValue(key, out var v))
        {
            throw new KeyNotFoundException($"The expected key {key} was not found");
        }

        Assert.Equal(expectedValue, v);
    }
}

public class RouteWithInstructionsTest
{
    private static readonly (double, double)[] klaverstraat = {
            (3.2168054580688477, 51.214451435318814), (3.2174867391586304, 51.214683296138816),
            (3.218269944190979, 51.21494203799424), (3.2188761234283447, 51.215093250093716),
            (3.2195842266082764, 51.21530158595063), (3.2203352451324463, 51.21547631852308),
            (3.2210916280746456, 51.21563088909212)
        };

    [Fact]
    public void MergeInstructionsAndShapeMeta_ShapeMustBeBroken_MergedNeatly()
    {
        var route = RouteScaffolding.GenerateRoute((RouteScaffolding.G(klaverstraat),
                new List<(string, string)> {
                        ("highway", "residential"),
                        ("name", "klaverstraat")
                }
            ));

        var settings = RouteInstructionGeneratorSettings.FromStream(
            TextToStream("{\"generators\":[],  \"languages\":{ \"en\": {\"*\":\"$turnDegrees\" } }}"));
        var routeAndInstructions = new RouteAndBaseInstructions(route, new List<BaseInstruction> {
                new(null, 0, 1, 0), // TurnDegrees is used to exfiltrate the instruction number
                new(null, 1, 3, 1),
                new(null, 3, 7, 2)
            }, settings.Languages).ForLanguage();

        var result = routeAndInstructions.AugmentRoute(_ => "instruction");
        var newMetas = result.Route.ShapeMeta;

        Assert.NotEmpty(newMetas);
        Assert.Equal(3, newMetas.Count);
        Assert.Equal(1, newMetas[0].Shape);
        Assert.Equal(3, newMetas[1].Shape);
        Assert.Equal(6, newMetas[2].Shape);

        for (int i = 0; i < 3; i++)
        {
            var a = newMetas[i].Attributes;
            a.AssertTag("name", "klaverstraat");
            a.AssertTag("highway", "residential");
            a.AssertTag("instruction", "" + i);
        }
        Assert.Equal(route.TotalDistance, newMetas.Select(m => m.Distance).Sum());

    }

    [Fact]
    public void MergeInstructionsAndShapeMeta_InstructionMustBeBroken_MergedNeatly()
    {
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.G(klaverstraat.ToList().GetRange(0, 3).ToArray()),
                new List<(string, string)> {
                        ("highway", "residential"),
                        ("name", "klaverstraat")
                }
            ),
            (RouteScaffolding.G(klaverstraat.ToList().GetRange(3, 4).ToArray()),
                new List<(string, string)> {
                        ("highway", "residential"),
                        ("name", "Elf-Julistraat")
                }
            )
        );


        var settings = RouteInstructionGeneratorSettings.FromStream(
            TextToStream("{\"generators\":[],  \"languages\":{ \"en\": {\"*\":\"$turnDegrees\" } }}"));
        var routeAndInstructions = new RouteAndBaseInstructions(route, new List<BaseInstruction> {
                new(null, 0, 7, 0), // TurnDegrees is used to exfiltrate the instruction number
            }, settings.Languages).ForLanguage();

        var result = routeAndInstructions.AugmentRoute(_ => "instruction");
        var newMetas = result.Route.ShapeMeta;

        Assert.NotEmpty(newMetas);
        Assert.Equal(2, newMetas.Count);

        var m0 = newMetas[0];
        Assert.Equal(3, m0.Shape);
        var a0 = m0.Attributes;
        a0.AssertTag("highway", "residential");
        a0.AssertTag("name", "klaverstraat");
        a0.AssertTag("instruction", "0");

        var m1 = newMetas[1];
        Assert.Equal(6, m1.Shape);
        var a1 = m1.Attributes;
        a1.AssertTag("highway", "residential");
        a1.AssertTag("name", "Elf-Julistraat");
        // a1.AssertTag("instruction", "0");
        Assert.Equal(route.TotalDistance, newMetas.Select(m => m.Distance).Sum());
    }

    private static Stream TextToStream(string text)
    {
        var memoryStream = new MemoryStream();
        memoryStream.Write(Encoding.UTF8.GetBytes(text));
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
