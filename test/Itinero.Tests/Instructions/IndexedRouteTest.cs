using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class IndexedRouteTest
    {
        [Fact]
        public void BuildMetaList_SmallRoute_MetaDataIsCorrect()
        {
            var route = new Route {
                Profile = "bicycle.something",
                Shape = new List<(double longitude, double latitude, float? e)> {
                    (3.1850286573171616, 51.20640699014240, null), // Ramp-up
                    (3.1848630309104920, 51.20649017227455, null),
                    (3.1847423315048218, 51.20651705939626, null), // on the roundabout
                    (3.1847235560417170, 51.20658847823707, null), // Still on the roundabout
                    (3.1846323609352107, 51.20662628816679, null), // the exit
                    (3.1846685707569122, 51.20672627427577, null), // ramp-down
                    (3.1847423315048218, 51.20736399569539, null)
                },
                ShapeMeta = new List<Route.Meta> {
                    new() {Attributes = new[] {("name", "A")}, Shape = 1},
                    new() {
                        Attributes =
                            new[] {("name", "B")},
                        Shape = 4
                    },
                    new() {
                        Attributes = new[] {("name", "C")},
                        Shape = 6
                    }
                }
            };

            var ir = new IndexedRoute(route);
            Assert.Equal(route.Shape.Count - 1, ir.Meta.Count);

            Assert.Equal(("name", "A"), ir.Meta[0].Attributes.ToList()[0]);

            Assert.Equal(("name", "B"), ir.Meta[1].Attributes.ToList()[0]);
            Assert.Equal(("name", "B"), ir.Meta[2].Attributes.ToList()[0]);
            Assert.Equal(("name", "B"), ir.Meta[3].Attributes.ToList()[0]);

            Assert.Equal(("name", "C"), ir.Meta[4].Attributes.ToList()[0]);
            Assert.Equal(("name", "C"), ir.Meta[5].Attributes.ToList()[0]);
        }
    }
}