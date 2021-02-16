using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Types;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class FollowBendTest
    {
        private static readonly (double lon, double lat, float? e)[] RabattestraatGeom = RouteScaffolding.P(
            (3.2872939109802246, 51.25332724127623, null),
            (3.287535309791565, 51.253407820538555, null),
            (3.2878518104553223, 51.253434680261265, null),
            (3.2881200313568115, 51.25339103320389, null),
            (3.288302421569824, 51.25325337682838, null),
            (3.2883989810943604, 51.25303178277289, null),
            (3.288388252258301, 51.25282361768734, null),
            (3.288302421569824, 51.25271953479114, null),
            (3.2879215478897095, 51.25222597720485, null),
            (3.287878632545471, 51.25207824465225, null),
            (3.287953734397888, 51.25196072978281, null),
            (3.2880690693855286, 51.251900293447285, null),
            (3.2884150743484497, 51.25177942053796, null)
        );

        // AT index 7, the left bend begins
        // At index 1, there is a track
        private static readonly Route Rabattestraat = RouteScaffolding.GenerateRoute(
            (RabattestraatGeom, new List<(string, string)> {
                ("highway", "unclassified"),
                ("name", "Rabauestraat")
            }));

        private static readonly Route RabattestraatWithOuterBranch = RouteScaffolding.GenerateRoute(
            new List<Route.Branch> {
                new() {
                    Shape = 2,
                    Coordinate = (3.2877659797668457, 51.253599195720575, null)
                }
            },
            (RabattestraatGeom, new List<(string, string)> {
                ("highway", "unclassified"),
                ("name", "Rabauestraat")
            }));


        private static readonly Route RabattestraatWithInnerBranch = RouteScaffolding.GenerateRoute(
            new List<Route.Branch> {
                new() {
                    Shape = 4,
                    Coordinate = (3.288109302520752, 51.25313250747596, null)
                }
            },
            (RabattestraatGeom, new List<(string, string)> {
                ("highway", "unclassified"),
                ("name", "Rabauestraat")
            }));

        [Fact]
        public void GenerateBend_LeftBend_GetsBend()
        {
            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(Rabattestraat), 8
            );
            Assert.NotNull(bend);
            Assert.Equal(85, bend.TurnDegrees);
            Assert.Equal(8, bend.ShapeIndex);
            Assert.Equal(11, bend.ShapeIndexEnd);
        }

        [Fact]
        public void GenerateBend_RightBend_GetsBend()
        {
            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(Rabattestraat), 1
            );
            Assert.NotNull(bend);
            Assert.Equal(-143, bend.TurnDegrees);
            Assert.Equal(1, bend.ShapeIndex);
            Assert.Equal(6, bend.ShapeIndexEnd);
        }

        [Fact]
        public void GenerateBend_RightBendWithOuter_GetsBend()
        {
            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(RabattestraatWithOuterBranch), 1
            );
            Assert.NotNull(bend);
            Assert.Equal(-143, bend.TurnDegrees);
            Assert.Equal(1, bend.ShapeIndex);
            Assert.Equal(6, bend.ShapeIndexEnd);
        }

        [Fact]
        public void GenerateBend_RightBendWithInner_GetsBend()
        {
            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(RabattestraatWithInnerBranch), 1
            );
            // We follow the bend, but only until we reach the inner branch
            Assert.NotNull(bend);
            Assert.Equal(-77, bend.TurnDegrees);
            Assert.Equal(1, bend.ShapeIndex);
            Assert.Equal(3, bend.ShapeIndexEnd);
        }
    }
}