using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Itinero.Instructions;
using Itinero.Instructions.Configuration;
using Itinero.Instructions.Generators;
using Itinero.Instructions.ToText;
using Itinero.Instructions.Types.Generators;
using Itinero.IO.Json.GeoJson;
using Xunit;

namespace Itinero.Tests.Instructions;

public class RealRouteTests
{
    // here, a few real (but still short) routes are tested against a full generator, generated from JSON


    public static string baseInstructionToLeftRight =
        "\"base\":{" +
        "\"$.name\": {" +
        "   \"$turnDegrees<=-135\": \"Turn sharply right onto $.name\"," +
        "  \"$turnDegrees>-135&$turnDegrees<=-65\": \"Turn right onto $.name\"," +
        "  \"$turnDegrees>-65&$turnDegrees<=-25\": \"Turn slightly right onto $.name\"," +
        "  \"$turnDegrees>-25&$turnDegrees<25\": \"Continue onto $.name\"," +
        "  \"$turnDegrees>=135\": \"Turn sharply left onto $.name\"," +
        "  \"$turnDegrees<135&$turnDegrees>=65\": \"Turn left onto $.name\"," +
        "  \"$turnDegrees<65&$turnDegrees>=25\": \"Turn slightly left onto $.name\"" +
        "}," +
        "\"*\":\"NAME NOT FOUND\"}," +
        "\"start\":\"Start towards $startDegrees°\"," +
        "\"turn\": {\"$+name\": \"Turn $turnDegrees onto $+name\" ,\"*\": \"Turn $turnDegrees\"}," +
        "\"*\": \"Fallback: $type $turndegrees\"";


    public static string roundaboutGenerator =
        "\"roundabout\": \"Taking the ${exitNumber}th exit\"";

    private static readonly IInstructionToText SimpleToText =
        ConfigurationParser.ParseInstructionToText(JsonDocument.Parse("{" + baseInstructionToLeftRight + "," + roundaboutGenerator + "}").RootElement);

    private static readonly LinearInstructionListGenerator gen = new(new List<IInstructionGenerator>() {
            new EndInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new TurnGenerator(),
            new BaseInstructionGenerator() }
    );

    [Fact]
    public void GenerateInstructions_SimpleRoute_TurnRight()
    {
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                    (3.2200763, 51.215923, null)
                ), new List<(string, string)> {
                        ("name", "Elf-Julistraat"),
                        ("highway", "residential")
                }
            ),
            (RouteScaffolding.P(
                (3.2203252, 51.215485, null),
                (3.2195995, 51.215298, null),
                (3.2191286, 51.21517, null)
            ), new List<(string, string)> {
                    ("name", "Klaverstraat")
            })
        );


        var instructions = gen.GenerateInstructions(route);
        var text = instructions.Select(i => SimpleToText.ToText(i)).ToList();
        Assert.Equal("Start towards 160°", text[0]);
        Assert.Equal("Turn 87 onto Klaverstraat", text[1]);
        Assert.Equal("Fallback: end 0", text[2]);
    }

}
