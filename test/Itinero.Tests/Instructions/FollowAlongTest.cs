using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Instructions.Types;
using Itinero.Instructions.Types.Generators;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class FollowAlongTest
    {
        private static readonly (double, double, float?)[] KlaverstraatGeom = RouteScaffolding.P(
            (3.2202011346817017, 51.215701453744565, null),
            (3.220316469669342, 51.21548471911082, null),
            (3.2195869088172913, 51.21530158595063, null),
            (3.2183021306991577, 51.21495043867946, null),
            (3.218068778514862, 51.21488491329411, null),
            (3.2174840569496155, 51.21467657555176, null),
            (3.216794729232788, 51.21445647578384, null));

        private static readonly Route Klaverstraat = RouteScaffolding.GenerateRoute(
            (KlaverstraatGeom,
                new List<(string, string)> {
                    ("highway", "residential"),
                    ("name", "Klaverstraat")
                }),
            (RouteScaffolding.P((3.2170093059539795, 51.21416580806587, null)),
                new List<(string, string)> {
                    ("name", "Ezelstraat"),
                    ("highway", "tertiary")
                }));

        [Fact]
        public void GenerateFollowAlong_SimpleExample_GetFollowAlong()
        {
            var followAlong = new FollowAlongGenerator().Generate(new IndexedRoute(Klaverstraat), 1);
            Assert.NotNull(followAlong);
            Assert.Equal("followalong", followAlong.Type);
            Assert.Equal(1, followAlong.ShapeIndex);
            Assert.Equal(5, followAlong.ShapeIndexEnd);
        }
    }
}