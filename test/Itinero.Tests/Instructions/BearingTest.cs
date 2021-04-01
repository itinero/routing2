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
        public void PointToNorth_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheNorth = (3.2203634083271027, 51.215595606725316, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheNorth);
            Assert.True(-3 < angle && angle < 3);
        }

        [Fact]
        public void PointToNorthEast_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheNorthEast = (   3.220451921224594, 51.21552294176568, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheNorthEast);
            Assert.True(40 < angle && angle < 50);
        }
        
        [Fact]
        public void PointToEast_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheEast = (   3.220517635345459, 51.2154742183759, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheEast);
            Assert.True(88 < angle && angle < 92);
        }
        
        [Fact]
        public void PointToWest_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheWest = (      3.2202866300940514,
                51.2154742183759, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheWest);
            Assert.True(-92 < angle && angle < -88);
        }
        
                
        [Fact]
        public void PointToSouthWest_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheSouthWest = (      3.2203013822436333, 51.21543473559121, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheSouthWest);
            Assert.True(-137 < angle && angle < -132);
        }

        
        [Fact]
        public void PointToSouth_CalculateAngle_IsZero()
        {
            var centerPoint = (3.220367431640625, 51.21547295828757, 0);
            var toTheSouth = (     3.22036474943161, 51.21542906518848, 0);
            var angle = Utils.AngleBetween(centerPoint, toTheSouth);
            Assert.True((-180 <= angle && angle < -177) ||(178 < angle && angle <= 180));
        }

        [Fact]
        public void ElfJulistraat_CalculateBearing_Is150()
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