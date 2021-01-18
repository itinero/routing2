using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Instructions;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions {
    public class FollowBendTest {
                    
        // AT index 7, the left bend begins
        // At index 1, there is a track
        private static Route Rabauestraat = RouteScaffolding.GenerateRoute(
            (RouteScaffolding.P(
                (3.2872939109802246, 51.25332724127623),
                (3.287535309791565, 51.253407820538555),
                (3.2878518104553223, 51.253434680261265), 
                (3.2881200313568115, 51.25339103320389),
                (3.288302421569824, 51.25325337682838),
                (3.2883989810943604, 51.25303178277289), 
                (3.288388252258301, 51.25282361768734),
                (3.288302421569824, 51.25271953479114), 
                (3.2879215478897095, 51.25222597720485),
                (3.287878632545471, 51.25207824465225),
                (3.287953734397888, 51.25196072978281),
                (3.2884150743484497, 51.25177942053796)
            ), new List<(string, string)> {
                ("highway","unclassified"),
                ("name","Rabauestraat")
            }));

        [Fact]
        public void GenerateBend_LeftBend_GetsBend() {

            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(Rabauestraat), 8
            );
            Assert.NotNull(bend);
            Assert.Equal(83, bend.TurnDegrees);
            Assert.Equal(8, bend.ShapeIndex);
            Assert.Equal(10, bend.ShapeIndexEnd);

        }
        
        [Fact]
        public void GenerateBend_RightBend_GetsBend() {

            var bend = (FollowBendInstruction) new FollowBendGenerator().Generate(
                new IndexedRoute(Rabauestraat), 1
            );
            Assert.NotNull(bend);
            Assert.Equal(-143, bend.TurnDegrees);
            Assert.Equal(1, bend.ShapeIndex);
            Assert.Equal(7, bend.ShapeIndexEnd);

        }
    }
}