using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Routes.Builders;
using Itinero.Tests.Profiles;
using Itinero.Tests.Routes.Paths;
using Xunit;

namespace Itinero.Tests.Routes.Builders {
    public class RouteBuilderWithBranchesTests {
        [Fact]
        public void RouteBuilder_BranchesAtStart_BranchesAreAdded() {
            var center = (4.801073670387268, 51.268064181900094);

            var b1 = (4.800550523695055, 51.2677764590809);
            var b2 = (4.800690032623627, 51.26831024794311);
            var b3 = (4.80110855940934, 51.26815246129831);
            var empty = new (double longitude, double latitude)[0];
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    center, // Center and start
                    b1, // Branches
                    b2, b3,
                    (4.801131337881088, 51.26801803143219), // Next point on the route (but also connected with center)
                    (4.801218509674072, 51.2679660072128) // Route continuation
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                    attributes)[] {
                        (0, 1, empty, new List<(string, string)> {("name", "B0")}),
                        (0, 2, empty, new List<(string, string)> {("name", "B1")}),
                        (0, 3, empty, new List<(string, string)> {("name", "B2")}),
                        /*#3*/ (0, 4, empty, new List<(string, string)> {("name", "B3")}),
                        /* #4 */ (4, 5, empty, new List<(string, string)> {("name", "C")})
                    });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[3], true), (edges[4], true)});

            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            var route = result.Value;

            Assert.Equal(3, route.Branches.Length);

            ItineroAsserts.SameLocations(b1, route.Branches[2].Coordinate);
            ItineroAsserts.SameLocations(b2, route.Branches[1].Coordinate);
            ItineroAsserts.SameLocations(b3, route.Branches[0].Coordinate);
            Assert.True(route.Branches[0].AttributesAreForward);

            Assert.True(route.Branches.All(b => b.Shape == 0));

            Assert.Equal(("name", "B3"), route.ShapeMeta[0].Attributes.ToList()[0]);
            Assert.Equal(("name", "C"), route.ShapeMeta[1].Attributes.ToList()[0]);
        }

        [Fact]
        public void RouteBuilder_ReversedBranchesAtStart_BranchesAreAdded() {
            var center = (4.801073670387268, 51.268064181900094);

            var b1 = (4.800550523695055, 51.2677764590809);
            var b2 = (4.800690032623627, 51.26831024794311);
            var b3 = (4.80110855940934, 51.26815246129831);
            var empty = new (double longitude, double latitude)[0];
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    center, // Center and start
                    b1, // Branches
                    b2, b3,
                    (4.801131337881088, 51.26801803143219), // Next point on the route (but also connected with center)
                    (4.801218509674072, 51.2679660072128) // Route continuation
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                    attributes)[] {
                        (1, 0, empty, new List<(string, string)> {("name", "B0")}),
                        (2, 0, empty, new List<(string, string)> {("name", "B1")}),
                        (3, 0, empty, new List<(string, string)> {("name", "B2")}),
                        /*#3*/ (0, 4, empty, new List<(string, string)> {("name", "B3")}),
                        /* #4 */ (4, 5, empty, new List<(string, string)> {("name", "C")})
                    });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[3], true), (edges[4], true)});

            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            var route = result.Value;

            Assert.Equal(3, route.Branches.Length);

            ItineroAsserts.SameLocations(b1, route.Branches[2].Coordinate);
            ItineroAsserts.SameLocations(b2, route.Branches[1].Coordinate);
            ItineroAsserts.SameLocations(b3, route.Branches[0].Coordinate);

            Assert.False(route.Branches[0].AttributesAreForward);

            Assert.True(route.Branches.All(b => b.Shape == 0));

            Assert.Equal(("name", "B3"), route.ShapeMeta[0].Attributes.ToList()[0]);
            Assert.Equal(("name", "C"), route.ShapeMeta[1].Attributes.ToList()[0]);
        }
        
         [Fact]
        public void RouteBuilder_MixedBranchesAtMiddle_BranchesAreAdded() {
            var center = (4.801073670387268, 51.268064181900094);

            var b1 = (4.800550523695055, 51.2677764590809);
            var b2 = (4.800690032623627, 51.26831024794311);
            var b3 = (4.80110855940934, 51.26815246129831);
            var empty = new (double longitude, double latitude)[0];
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    center, // Center and start
                    b1, // Branches
                    b2, b3,
                    (4.801131337881088, 51.26801803143219), // Next point on the route (but also connected with center)
                    (4.801218509674072, 51.2679660072128) // Route continuation
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                    attributes)[] {
                        (0, 1, empty, new List<(string, string)> {("name", "B0")}),
                        (1, 2, empty, new List<(string, string)> {("name", "B1")}),
                        (3, 1, empty, new List<(string, string)> {("name", "B2")}),
                        /*#3*/ (4, 1, empty, new List<(string, string)> {("name", "B3")}),
                        /* #4 */ (4, 5, empty, new List<(string, string)> {("name", "C")})
                    });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], true), (edges[3], false), (edges[4], true)});

            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            var route = result.Value;

            Assert.Equal(2, route.Branches.Length);

            ItineroAsserts.SameLocations(b2, route.Branches[1].Coordinate);
            ItineroAsserts.SameLocations(b3, route.Branches[0].Coordinate);

            Assert.False(route.Branches[0].AttributesAreForward);
            Assert.True(route.Branches[1].AttributesAreForward);

            Assert.Equal(("name", "B0"), route.ShapeMeta[0].Attributes.ToList()[0]);
            Assert.Equal(("name", "B3"), route.ShapeMeta[1].Attributes.ToList()[0]);
            Assert.Equal(("name", "C"), route.ShapeMeta[2].Attributes.ToList()[0]);
        }
    }
}