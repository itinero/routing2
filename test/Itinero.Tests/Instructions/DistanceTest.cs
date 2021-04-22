using System;
using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class DistanceTest
    {
        [Fact]
        public void TestDistance_TwoClosebyPoints()
        {
            var route = new Route {
                Shape = new List<(double longitude, double latitude, float? e)> {
                    (3.2201984524726863,
                        51.215719934945206, 0f),
                    (3.2203218340873714,
                        51.21548639922819, 0f)
                }
            };
            var expected = 27.38; // According to geojson.io
         var instructionsCalculated =   route.DistanceBetween(0, 1);
         Assert.True(Math.Abs(expected - instructionsCalculated) < 0.05);
        }
    }
}