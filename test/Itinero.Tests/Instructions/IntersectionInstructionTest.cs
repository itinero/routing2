using System;
using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Generators;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions {
    public class IntersectionInstructionTest {
        [Fact]
        public void GenerateCrossroad_SimpleCrossroad_GetCrossroadInstruction() {
            var gen = new IntersectionInstruction.IntersectionInstructionGenerator();
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude, float? e)> {
                (3.2175779342651363, 51.21153207328682, null),
                (3.2173392176628113, 51.211704299781594, null),
                (3.217163532972336, 51.21187904600573, null),
                (3.217163532972336, 51.21187904600573, null),
                (3.216855078935623, 51.212181489826555, null)
            };

            route.ShapeMeta = new List<Route.Meta> {
                new() {
                    Shape = 4,
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                }
            };

            route.Branches = new[] {
                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.2176074385643, 51.21182023725436, null),
                    Attributes = new[] {("name", "Groenesraat"), ("highway", "residential")}
                },

                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.217085748910904, 51.21161524616228, null),
                    Attributes = new[] {("name", "Groenesraat"), ("highway", "residential")}
                }
            };
            var instr = (IntersectionInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal(2, instr.ShapeIndexEnd);
            Assert.Contains("cross the road", instr.Mode());
        }


        [Fact]
        public void GenerateCrossroad_KeepLeft_GetCrossroadInstruction() {
            var gen = new IntersectionInstruction.IntersectionInstructionGenerator();
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude, float? e)> {
                (3.2175779342651363, 51.21153207328682, null),
                (3.2173392176628113, 51.211704299781594, null),
                (3.217163532972336, 51.21187904600573, null),
                (3.217163532972336, 51.21187904600573, null),
                (3.216855078935623, 51.212181489826555, null)
            };

            route.ShapeMeta = new List<Route.Meta> {
                new() {
                    Shape = 4,
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                }
            };

            route.Branches = new[] {
                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.2176074385643, 51.21182023725436, null),
                    Attributes = new[] {("name", "Groenesraat"), ("highway", "residential")}
                }

                // The left turn has been removed
            };
            var instr = (IntersectionInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal(2, instr.ShapeIndexEnd);
            Assert.Contains("keep left", instr.Mode());
        }

        [Fact]
        public void GenerateCrossroad_SimpleLeft_GetCrossroadInstruction() {
            var gen = new IntersectionInstruction.IntersectionInstructionGenerator();
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the south-west
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude, float? e)> {
                (3.2175779342651363, 51.21153207328682, null),
                (3.2173392176628113, 51.211704299781594, null),

                (3.217083066701889, 51.21161356590366, null),
                (3.2166606187820435, 51.21148670620053, null)
            };

            route.ShapeMeta = new List<Route.Meta> {
                new Route.Meta {
                    Shape = 1,
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                },
                new Route.Meta {
                    Shape = 3,
                    Attributes = new[] {("name", "Groenestraat"), ("highway", "residential")}
                }
            };

            route.Branches = new[] {
                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.217179626226425, 51.2118614033882, null),
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                },

                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.2176195085048676, 51.21182611813286, null),
                    Attributes = new[] {("name", "Groenesraat"), ("highway", "residential")}
                }
            };
            var instr = (IntersectionInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal(0u, instr.ActualIndex);
            Assert.Equal(2, instr.ShapeIndexEnd);
            Assert.Contains("left", instr.Mode());
        }


        [Fact]
        public void GenerateCrossroad_SimpleRight_GetCrossroadInstruction() {
            var gen = new IntersectionInstruction.IntersectionInstructionGenerator();
            //https://www.openstreetmap.org/#map=19/51.21170/3.21733
            // Coming from the north-east
            var route = new Route();
            route.Profile = "bicycle.something";
            route.Shape = new List<(double longitude, double latitude, float? e)> {
                (3.217179626226425, 51.2118614033882, null),
                (3.2173392176628113, 51.211704299781594, null),

                (3.217083066701889, 51.21161356590366, null),
                (3.2166606187820435, 51.21148670620053, null)
            };

            route.ShapeMeta = new List<Route.Meta> {
                new Route.Meta {
                    Shape = 1,
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                },
                new Route.Meta {
                    Shape = 3,
                    Attributes = new[] {("name", "Groenestraat"), ("highway", "residential")}
                }
            };

            route.Branches = new[] {
                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.2175779342651363, 51.21153207328682, null),
                    Attributes = new[] {("name", "Rozendal"), ("highway", "residential")}
                },

                new Route.Branch {
                    Shape = 1,
                    Coordinate = (3.2176195085048676, 51.21182611813286, null),
                    Attributes = new[] {("name", "Groenesraat"), ("highway", "residential")}
                }
            };
            var instr = (IntersectionInstruction) gen.Generate(new IndexedRoute(route), 1);
            Assert.NotNull(instr);
            Assert.Equal(2u, instr.ActualIndex);
            Assert.Equal(2, instr.ShapeIndexEnd);
            Assert.Contains("right", instr.Mode());
        }
    }
}