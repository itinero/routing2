using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Itinero.Instructions.Configuration;
using Itinero.Instructions.Generators;
using Itinero.Instructions.ToText;
using Itinero.Instructions.Types;
using Itinero.Instructions.Types.Generators;
using Itinero.Routes;
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
        ConfigurationParser.ParseInstructionToText(JsonDocument
            .Parse("{" + baseInstructionToLeftRight + "," + roundaboutGenerator + "}").RootElement);

    private static readonly LinearInstructionListGenerator gen = new(new List<IInstructionGenerator>
        {
            new RoundaboutInstructionGenerator(), new TurnGenerator(), new BaseInstructionGenerator()
        }
    );

    [Fact]
    public void GenerateInstructions_SimpleRoute_TurnRight()
    {
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                    (3.2200763, 51.215923, null)
                ), new List<(string, string)> { ("name", "Elf-Julistraat"), ("highway", "residential") }
            ),
            (RouteScaffolding.P(
                (3.2203252, 51.215485, null),
                (3.2195995, 51.215298, null)
            ), new List<(string, string)> { ("name", "Klaverstraat") })
        );

        var instructions = gen.GenerateInstructions(route);
        var text = instructions.Select(i => SimpleToText.ToText(i)).ToList();
        Assert.Equal("Turn right onto Elf-Julistraat",
            text[0]); // This one is a bit weird, it should be a start-instruction, but they are not in this generator, so the 'turnright' fills the gap
        Assert.Equal("Turn -87 onto Klaverstraat", text[1]);
        Assert.Equal("Fallback: end 0", text[2]);
    }


    [Fact]
    public void GenerateInstructionsWithStart_SimpleRoute_StartInstructionHasZeroIndices()
    {
        var generator = new LinearInstructionListGenerator(new List<IInstructionGenerator>
            {
                new StartInstructionGenerator(),
                new EndInstructionGenerator(),
                new RoundaboutInstructionGenerator(),
                new TurnGenerator(),
                new BaseInstructionGenerator()
            }
        );
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                    (3.2200763, 51.215923, null)
                ), new List<(string, string)> { ("name", "Elf-Julistraat"), ("highway", "residential") }
            ),
            (RouteScaffolding.P(
                (3.2203252, 51.215485, null),
                (3.2195995, 51.215298, null),
                (3.2191286, 51.21517, null)
            ), new List<(string, string)> { ("name", "Klaverstraat") })
        );


        var instructions = generator.GenerateInstructions(route);
        var startInstr = instructions[0];
        Assert.Equal("start", startInstr.Type);
        Assert.Equal(0, startInstr.ShapeIndex);
        Assert.Equal(0, startInstr.ShapeIndexEnd);
    }

    [Fact]
    public void KlaverstraatStartAndStopRoute_GenerateInstructions_InstructionsContainFollowAlong()
    {
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                    (
                        3.2203238220720323,
                        51.215487201201114, 0f
                    ),
                    (
                        3.2208590840492377,
                        51.215600349155096, 0f
                    ), (
                        3.2210915919065712,
                        51.215630089643184, 0f
                    )
                ), new List<(string, string)> { ("name", "Klaverstraat"), ("highway", "residential") }
            )
        );

        var generator = new LinearInstructionListGenerator(new List<IInstructionGenerator>
        {
            new StartInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowBendGenerator(),
            new FollowAlongGenerator(),
            new TurnGenerator(),
            new BaseInstructionGenerator()
        });
        var instructions = generator.GenerateInstructions(route);
        Assert.Equal("followalong", instructions[1].Type);
    }

    [Fact]
    public void KlaverstraatStartAndVlamingdamStopRoute_GenerateInstructions_InstructionsContainTurn()
    {
        var route = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                    (
                        3.2208590840492377,
                        51.215600349155096, 0f
                    ), (
                        3.2210915919065712,
                        51.215630089643184, 0f
                    )
                ), new List<(string, string)> { ("name", "Klaverstraat"), ("highway", "residential") }
            ),
            (RouteScaffolding.P((3.221081870955544,
                    51.215575831418306, 0f)),
                new List<(string, string)> { ("highway", "tertiary"), ("name", "Vlamingdam") })
        );

        var generator = new LinearInstructionListGenerator(new List<IInstructionGenerator>
        {
            new StartInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowBendGenerator(),
            new FollowAlongGenerator(),
            new TurnGenerator(),
            new BaseInstructionGenerator()
        });
        var instructions = generator.GenerateInstructions(route);
        Assert.Equal("turn", instructions[1].Type);
    }

    [Fact]
    public void CyclepathNextToRingroad_ConstructInstructions_FollowbendRight()
    {
        /*
         * This describes a long, gentle rightwards bend in the cyclepath here:https://www.openstreetmap.org/#map=19/51.20998/3.21397
         * The regression was that it would insert a 'turn straight' instruction, but this should be one (or two) 'follow bend'-instructions
         */
        var part0 = (RouteScaffolding.P(
                (
                    3.213835487550398,
                    51.210335796947305, 0f
                ),
                (
                    3.213832145410805,
                    51.210259375824165, 0f
                ),
                (
                    3.2137970529425957,
                    51.210155736015906, 0f
                ),
                (
                    3.213740236565144,
                    51.21006884589596, 0f
                ),
                (
                    3.213661696278564,
                    51.210029064822805, 0f
                ),
                (
                    3.2135965245518605,
                    51.21001126801539, 0f
                ),
                (
                    3.213499602496398,
                    51.21000289304513, 0f
                )),
            new List<(string, string)> { ("highway", "cycleway"), ("oneway", "yes"), ("oneway:bicycle", "yes") });
        var part1 = (RouteScaffolding.P(
                (3.2134026804409643, 51.21000498678771, 0f),
                (3.2132756791262693, 51.21005523658562, 0f)),
            new List<(string, string)> { ("highway", "cycleway") });
        var route = RouteScaffolding.GenerateRoute(part0, part1);
        var generator = new LinearInstructionListGenerator(new List<IInstructionGenerator>
        {
            new StartInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowBendGenerator(),
            new FollowAlongGenerator(),
            new TurnGenerator(),
            new BaseInstructionGenerator()
        });
        var instructions = generator.GenerateInstructions(route);
        Assert.Equal("start", instructions[0].Type);
        Assert.Equal("followbend", instructions[1].Type);
        Assert.Equal(-117, instructions[1].TurnDegrees);
        Assert.Equal("end", instructions[2].Type);
    }

    [Fact]
    public void RoundaboutRouteWithUturn_GenerateRoundaboutInstruction_CorrectExit()
    {
        // This route is situated here: https://www.openstreetmap.org/#map=19/51.22359/3.21497
        // It starts on the north road, goes on the roundabout and exists immediately, resulting in (more or less) a uturn
        // This is a bit a peculiar case
        var route = new Route
        {
            Attributes = Array.Empty<(string, string)>(),
            Profile = "car.fast",
            Shape =
                new List<(double longitude, double latitude, float? e)>
                {
                    (3.214352788301324, 51.22352149775147, null),
                    (3.214602292239011, 51.223570783616175, null),
                    (3.2148061899038463, 51.22361110572973, null),
                    (3.2148061899038463, 51.22355734291166, null),
                    (3.214833018543956, 51.223503580093585, null),
                    (3.2148813100961537, 51.22345653762777, null),
                    (3.2147257039835164, 51.223422935866466, null),
                    (3.214334005837912, 51.223362452696136, null),
                    (3.214211548984376, 51.223338376836864, null)
                },
            ShapeMeta = new List<Route.Meta>
            {
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "1"), ("oneway", "yes"),
                            ("highway", "residential"), ("surface", "concrete"), ("abutters", "commercial"),
                            ("maxspeed", "50"), ("oneway:bicycle", "yes"),
                            ("_segment_guid", "39842e6a-0000-0000-0000-000001000000"),
                            ("_segment_offset1", "0.6879224841687648"), ("_segment_offset2", "1"),
                            ("_segment_forward", "True"), ("maneuver", "Start east on Sint-Pieterszuidstraat")
                        },
                    AttributesAreForward = true,
                    Distance = 0,
                    Profile = "car.fast",
                    Shape = 0,
                    Time = 0
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "1"), ("oneway", "yes"),
                            ("highway", "residential"), ("surface", "concrete"), ("abutters", "commercial"),
                            ("maxspeed", "50"), ("oneway:bicycle", "yes"),
                            ("_segment_guid", "39842e6a-0000-0000-0000-000001000000"),
                            ("_segment_offset1", "0.6879224841687648"), ("_segment_offset2", "1"),
                            ("_segment_forward", "True"),
                            ("maneuver", "Turn around on the roundabout by taking the second exit")
                        },
                    AttributesAreForward = true,
                    Distance = 18.21912673174574,
                    Profile = "car.fast",
                    Shape = 1,
                    Time = 252.88147903663088
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "1"), ("oneway", "yes"),
                            ("highway", "residential"), ("surface", "concrete"), ("abutters", "commercial"),
                            ("maxspeed", "50"), ("oneway:bicycle", "yes"),
                            ("_segment_guid", "39842e6a-0000-0000-0100-000002000000"), ("_segment_forward", "True"),
                            ("maneuver", "Turn around on the roundabout by taking the second exit")
                        },
                    AttributesAreForward = true,
                    Distance = 14.890389034385322,
                    Profile = "car.fast",
                    Shape = 2,
                    Time = 206.67859979726825
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("bicycle", "use_sidepath"), ("highway", "secondary"), ("surface", "concrete"),
                            ("junction", "roundabout"), ("maxspeed", "50"),
                            ("_segment_guid", "078d3c09-0000-0000-0000-000003000000"), ("_segment_forward", "True"),
                            ("maneuver", "Turn around on the roundabout by taking the second exit")
                        },
                    AttributesAreForward = true,
                    Distance = 18.46013517334652,
                    Profile = "car.fast",
                    Shape = 5,
                    Time = 256.2266762060497
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "2"), ("oneway", "no"),
                            ("bicycle", "use_sidepath"), ("highway", "residential"), ("surface", "concrete"),
                            ("maxspeed", "50"), ("lanes:forward", "1"), ("lanes:backward", "1"),
                            ("_segment_guid", "39842e67-0000-0000-0000-000001000000"),
                            ("_segment_forward", "False"), ("maneuver", "Turn right on Sint-Pieterszuidstraat")
                        },
                    AttributesAreForward = false,
                    Distance = 0,
                    Profile = "car.fast",
                    Shape = 5,
                    Time = 0
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "2"), ("oneway", "no"),
                            ("bicycle", "use_sidepath"), ("highway", "residential"), ("surface", "concrete"),
                            ("maxspeed", "50"), ("lanes:forward", "1"), ("lanes:backward", "1"),
                            ("_segment_guid", "39842e67-0000-0000-0000-000001000000"),
                            ("_segment_forward", "False"), ("maneuver", "Follow along on Sint-Pieterszuidstraat")
                        },
                    AttributesAreForward = false,
                    Distance = 11.46241961970248,
                    Profile = "car.fast",
                    Shape = 6,
                    Time = 159.09838432147043
                },
                new()
                {
                    Attributes =
                        new[]
                        {
                            ("lit", "yes"), ("name", "Sint-Pieterszuidstraat"), ("lanes", "2"), ("oneway", "no"),
                            ("highway", "residential"), ("surface", "concrete"), ("maxspeed", "50"),
                            ("cycleway:left", "track"), ("lanes:forward", "1"), ("cycleway:right", "lane"),
                            ("lanes:backward", "1"), ("_segment_guid", "0ac7dd6d-0000-0000-0700-000009000000"),
                            ("_segment_offset1", "0"), ("_segment_offset2", "0.5994659342336156"),
                            ("_segment_forward", "False"), ("maneuver", "Follow along on Sint-Pieterszuidstraat")
                        },
                    AttributesAreForward = false,
                    Distance = 37.03283867891828,
                    Profile = "car.fast",
                    Shape = 8,
                    Time = 514.0158008633857
                }
            },
            Stops = new List<Route.Stop>
            {
                new()
                {
                    Attributes = Array.Empty<(string, string)>(),
                    Distance = 0,
                    Coordinate = (3.2143459856125105, 51.22353500004843, 0f),
                    Shape = 0,
                    Time = 0
                },
                new()
                {
                    Attributes = Array.Empty<(string, string)>(),
                    Distance = 100.06493468506724,
                    Coordinate = (3.2142120019515517, 51.223337471472746, 0f),
                    Shape = 8,
                    Time = 7.209289242440002
                }
            },
            TotalDistance = 100.06493468506724,
            TotalTime = 7.209289242440002
        };

        var gen = new LinearInstructionListGenerator(new List<IInstructionGenerator>
            {
                new RoundaboutInstructionGenerator(),
                new TurnGenerator(),
                new BaseInstructionGenerator(),
                new EndInstructionGenerator()
            }
        );
        var instructions = gen.GenerateInstructions(route);
        var roundabout = (RoundaboutInstruction)instructions[1];
        Assert.Equal("roundabout", roundabout.Type);
        Assert.Equal(1, roundabout.ExitNumber);
        Assert.Equal(-178, roundabout.TurnDegrees);
    }
}
