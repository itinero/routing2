using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Generators;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions {
    public class RoundaboutInstructionTest {
        [Fact]
        public void GenerateRoundabout_FirstExitRight_GetsInstruction() {
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route {
                Profile = "bicycle.something",
                Shape = new List<(double longitude, double latitude, float? e)> {
                    (3.1850286573171616, 51.20640699014240, null), // Ramp-up
                    (3.1848630309104920, 51.20649017227455, null),
                    (3.1847423315048218, 51.20651705939626, null), // on the roundabout
                    (3.1847235560417170, 51.20658847823707, null), // Still on the roundabout
                    (3.1846323609352107, 51.20662628816679, null), // the exit
                    (3.1846685707569122, 51.20672627427577, null),
                    (3.1847423315048218, 51.20736399569539, null) // ramp-down
                },
                ShapeMeta = new List<Route.Meta> {
                    new() {
                        Attributes = new[] {("highway", "residential"), ("name", "Legeweg")},
                        Shape = 2
                    },
                    new() {
                        Attributes =
                            new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")},
                        Shape = 4
                    },
                    new() {
                        Attributes = new[] {("highway", "residential"), ("name", "Sint-Hubertuslaan")},
                        Shape = 6
                    }
                },
                Branches = new Route.Branch[0]
            };


            var gen = new RoundaboutInstructionGenerator();

            var instr = (RoundaboutInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal("right", instr.TurnDegrees.DegreesToText());
            Assert.Equal(1, instr.ExitNumber);
            Assert.Equal(4, instr.ShapeIndexEnd);
        }

        [Fact]
        public void GenerateRoundabout_StraightOn_GetsInstruction2ndExit() {
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude, float? e)> {
                (3.1850286573171616, 51.2064069901424, null),
                (3.184863030910492, 51.20649017227455, null),
                (3.1847423315048218, 51.20651705939626, null), // Roundabout
                (3.184723556041717, 51.20658847823707, null),
                (3.1846323609352107, 51.20662628816679, null), // First exit
                (3.1845176964998245, 51.20661410497061, null),
                (3.1844761222600937, 51.20655654982782, null), // Second exit
                (3.1842997670173645, 51.20656999337126, null)
            };

            route.ShapeMeta = new List<Route.Meta> {
                new() {
                    Attributes = new[] {("highway", "residential"), ("name", "Legeweg")},
                    Shape = 2
                },
                new() {
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")},
                    Shape = 6
                },
                new() {
                    Attributes = new[] {("highway", "residential"), ("name", "Sint-Hubertuslaan")},
                    Shape = 7
                }
            };

            route.Branches = new[] {
                new Route.Branch {
                    Coordinate = (3.184565305709839, 51.206622927285395, null),
                    Shape = 4,
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")}
                },
                new Route.Branch {
                    Coordinate = (3.184565305709839, 51.206622927285395, null),
                    Shape = 6,
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")}
                }
            };
            var gen = new RoundaboutInstructionGenerator();

            var instr = (RoundaboutInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal("straight on", instr.TurnDegrees.DegreesToText());
            Assert.Equal(2, instr.ExitNumber);
            Assert.Equal(6, instr.ShapeIndexEnd);
            Assert.Equal(2, instr.ExitNumber);
        }
    }
}