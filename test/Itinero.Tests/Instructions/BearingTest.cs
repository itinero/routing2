using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    /// <summary>
    ///     More advanced, real life tests
    /// </summary>
    public class BearingTest
    {
        [Fact]
        public void GeoExtensions_ElfJulistraat_CalculateBearing_Is150()
        {
            var r = new Route {
                Shape = new List<(double longitude, double latitude, float? e)> {
                    (3.220161, 51.21578, 0),
                    (3.220325, 51.21548, 0)
                }
            };
            var bearing = r.BearingAt(0);
            Assert.True(158 < bearing && bearing < 162);
        }
    }
}