using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Instructions;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class RoundaboutInstructionTest
    {
        [Fact]
        public void GenerateRoundabout_FirstExitRight_GetsInstruction()
        {
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude)>
            {
                (3.1850286573171616, 51.20640699014240), // Ramp-up
                (3.1848630309104920, 51.20649017227455),
                (3.1847423315048218, 51.20651705939626), // on the roundabout
                (3.1847235560417170, 51.20658847823707), // Still on the roundabout
                (3.1846323609352107, 51.20662628816679), // the exit
                (3.1846685707569122, 51.20672627427577) // ramp-down
            };

            route.ShapeMeta = new List<Route.Meta>
            {
                new Route.Meta
                {
                    Attributes = new[] {("highway", "residential"), ("name", "Legeweg")},
                    Shape = 0
                },
                new Route.Meta
                {
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")},
                    Shape = 2
                },
                new Route.Meta
                {
                    Attributes = new[] {("highway", "residential"), ("name", "Sint-Hubertuslaan")},
                    Shape = 4
                }
            };

            route.Branches = new[]
            {
                new Route.Branch
                {
                    Coordinate = (3.184565305709839, 51.206622927285395),
                    Shape = 4,
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")}
                }
            };
            var gen = RoundaboutInstruction.Constructor;

            var instr = (RoundaboutInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal("right", instr.TurnDegrees.DegreesToText());
            Assert.Equal(0, instr.ExitNumber);
            Assert.Equal(4, instr.ShapeIndexEnd);
        }

        [Fact]
        public void GenerateRoundabout_StraightOn_GetsInstruction2ndExit()
        {
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude)>
            {
                (3.1850286573171616, 51.2064069901424),
                (3.184863030910492, 51.20649017227455),
                (3.1847423315048218, 51.20651705939626), // Roundabout
                (3.184723556041717, 51.20658847823707),
                (3.1846323609352107, 51.20662628816679), // First exit
                (3.1845176964998245, 51.20661410497061),
                (3.1844761222600937, 51.20655654982782), // Second exit
                (3.1842997670173645, 51.20656999337126)
            };

            route.ShapeMeta = new List<Route.Meta>
            {
                new Route.Meta
                {
                    Attributes = new[] {("highway", "residential"), ("name", "Legeweg")},
                    Shape = 0
                },
                new Route.Meta
                {
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")},
                    Shape = 2
                },
                new Route.Meta
                {
                    Attributes = new[] {("highway", "residential"), ("name", "Sint-Hubertuslaan")},
                    Shape = 6
                }
            };

            route.Branches = new[]
            {
                new Route.Branch
                {
                    Coordinate = (3.184565305709839, 51.206622927285395),
                    Shape = 4,
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")}
                },
                new Route.Branch
                {
                    Coordinate = (3.184565305709839, 51.206622927285395),
                    Shape = 6,
                    Attributes = new[] {("junction", "roundabout"), ("highway", "residential"), ("name", "Legeweg")}
                }
            };
            var gen = RoundaboutInstruction.Constructor;

            var instr = (RoundaboutInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal("straight on", instr.TurnDegrees.DegreesToText());
            Assert.Equal(1, instr.ExitNumber);
            Assert.Equal(6, instr.ShapeIndexEnd);
        }
    }
}