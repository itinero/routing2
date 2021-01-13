using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions;
using Itinero.Instructions.Instructions;
using Itinero.Instructions.ToText;
using Itinero.Routes;
using Xunit;
using LinearInstructionGenerator = Itinero.Instructions.LinearInstructionGenerator;

namespace Itinero.Tests.Instructions
{
    public class InstructionTest
    {
        [Fact]
        public void GenerateInstructions_AdvancedRoute_EmitsInstructions()
        {
            var route = new Route
            {
                Profile = "bicycle.something",
                Shape = new List<(double longitude, double latitude)>
                {
                    // Blokstraat
                    (3.2194970548152924, 51.215430955322816), // 0
                    (3.218715190887451, 51.216450776477345),

                    // Crossing ring
                    (3.218286037445068, 51.21661878438491),

                    // Veldmrschlk Fochstr
                    (3.218286037445068, 51.21686071469478),
                    // Werfstraat
                    (3.217722773551941, 51.21750249588503),
                    (3.2157111167907715, 51.216222284739125), // 5
                    (3.215475082397461, 51.21605763557773),

                   
                //  Kruising scheepsdale: 1ste vak
                (3.215265870094299, 51.215849303141894),
                //  Kruising scheepsdale: 2de vak; Filips de goedelaan (fietsstraat)
                (3.2152444124221797, 51.2158005800975),
                (3.213919401168823, 51.21336940272057),
                // Karel de stoute, even links-rechts
                (3.2133668661117554, 51.212247019059234), // 10
                /*
               // Fietspad graaf-visartpark
               (3.2135170698165894, 51.21221341433617),
               (3.2133883237838745, 51.21213948385911),
               // Brug langs sluizencomplex
               (3.2130208611488342, 51.21113805024932),
               // Gewone fietspad op buitenvest
               (3.2129216194152828, 51.21093977725382),
               (3.2129859924316406, 51.21066084939121), // 15
               // Fietspad langs expressweg
               (3.212905526161194, 51.21026765905277),
               (3.213372230529785, 51.21001729251517),
               (3.2134768366813655, 51.21000553029537),

               // Oversteek bypass
               (3.21361094713211, 51.21001393188127),
               (3.2136230170726776, 51.20994839947071), // 20

               // Tussenschot
               (3.213661909103393, 51.209859342455665),
               // Oversteek expressweg
               (3.213754445314407, 51.20979044917488),
               // Fietspad langs expressweg
               (3.2136256992816925, 51.2094846289770),
               // Buiten smedenvest
               (3.2128357887268066, 51.20994839947071),
               (3.212476372718811, 51.20893011464751), // 25
               (3.2124173641204834, 51.2084125619158),
               (3.2123905420303345, 51.2079857445725),
               (3.2124334573745728, 51.20721611905115),

               // Verbinding naar brugge plus
               (3.2124119997024536, 51.20699556351492),
               (3.2123228162527084, 51.20697749891944), //30

               // Lange vesting / oprit
               (3.2119647413492203, 51.20704345566401),
               (3.211677074432373, 51.207132098019024),
               (3.2115429639816284, 51.20723124282065),
               (3.2112854719161987, 51.20738920189418),

               // Lange vesting (straat
               (3.211108446121216, 51.207431212194878), // 35
               (3.2107543945312496, 51.20722284072709),
               (3.210188448429107, 51.206962375066624) //*/
                },
                ShapeMeta = new List<Route.Meta>
                {
                    new Route.Meta
                    {
                        Distance = 0,
                        Shape = 0,
                        Attributes = new[] {("name", "blokstraat"), ("highway", "residential")}
                    },
                    new Route.Meta {Distance = 150, Shape = 2, Attributes = new[] {("highway", "cycleway")}},
                    new Route.Meta
                    {
                        Distance = 15,
                        Shape = 3,
                        Attributes = new[] {("name", "Veldmaarschalk Fochstraat"), ("highway", "residential")}
                    },
                    new Route.Meta
                    {
                        Distance = 200,
                        Shape = 4,
                        Attributes = new[]
                        {
                            ("name", "Werfstraat"), ("cyclestreet", "yes"), ("highway", "residential")
                        }
                    }
                },
                Branches = new[]
                {
                    // First crossing of queen lizzie
                    new Route.Branch()
                    {
                        Shape = 2,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "primary"), ("name", "Koningin Elisabethlaan")
                        },
                        Coordinate = (3.21843221783638, 51.216730089286074)
                    },
                    new Route.Branch()
                    {
                        Shape = 2,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "primary"), ("name", "Koningin Elisabethlaan")
                        },
                        Coordinate = (3.2177583128213882, 51.216221864716495)
                    },
                    // Second crossing of queen lizzie
                    new Route.Branch()
                    {
                        Shape = 3,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "primary"), ("name", "Koningin Elisabethlaan")
                        },
                        Coordinate = (3.2177898287773132, 51.2164717774993)
                    },
                    new Route.Branch()
                    {
                        Shape = 3,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "primary"), ("name", "Koningin Elisabethlaan")
                        },
                        Coordinate = (3.2190048694610596, 51.21737145228511)
                    },
                    new Route.Branch()
                    {
                        Shape = 3,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "primary_link"), ("name", "Koningin Elisabethlaan")
                        },
                        Coordinate = (3.2184328883886337, 51.216730089286074)
                    },

                    // crossroads of werfstraat / Veldmr. Fochstraat 
                    new Route.Branch()
                    {
                        Shape = 4,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "residential"), ("name", "Werfstraat")
                        },
                        Coordinate = (3.2186079025268555, 51.21813418800571)
                    },
                    new Route.Branch()
                    {
                        Shape = 4,
                        Attributes = new List<(string key, string value)>
                        {
                            ("highway", "residential"), ("name", "Veldmaarschalk Fochstraat")
                        },
                        Coordinate = (3.216794729232788, 51.21848699100294)
                    }
                }
            };

            var start = new Route.Stop {Coordinate = (3.219408541917801, 51.21541415412617), Shape = 0, Distance = 10};
            // estimated m between the pinned start point and the snapped startpoint

            var stop = new Route.Stop
            {
                Coordinate = (3.2099054753780365, 51.20692456541283), Shape = route.Shape.Count - 1, Distance = 15
            };
            // estimated m between the pinned start point and the snapped startpoint


            route.Stops = new List<Route.Stop>
            {
                start, stop
            };


            var instructionGenerator = new LinearInstructionGenerator(new BaseInstructionGenerator());
            var instructions = instructionGenerator.GenerateInstructions(route).ToList();
            var toText = new SubstituteText(new[]
                {
                    ("Turn ", false),
                    ("TurnDegrees", true)
                }
            );
            var texts = instructions.Select(toText.ToText).ToList();

            Assert.NotEmpty(instructions);
            Assert.Equal("Turn 98", texts[0]);
        }


        [Fact]
        public void Angles_AllOrthoDirections_CorrectAngle()
        {
            var eflJuli_klaver = (3.2203030586242676, 51.215446076394535);
            var straightN = (3.2203037291765213, 51.21552000156258);
            var straightE = (3.2204418629407883, 51.215446496424235);

            var n = Utils.AngleBetween(eflJuli_klaver, straightN);
            Assert.True(-5 < n && n < 5);
            var s = Utils.AngleBetween(straightN, eflJuli_klaver);
            Assert.True(-180 <= s && s < -175 || 175 < s && s <= 180);

            var e = Utils.AngleBetween(eflJuli_klaver, straightE);
            Assert.True(-95 < e && e < -85);
            var w = Utils.AngleBetween(straightE, eflJuli_klaver);
            Assert.True(85 <= w && w < 95);
        }
    }
}