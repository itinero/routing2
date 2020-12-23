using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles
{
    public class TileStaticTests
    {
        [Fact]
        public void TileStatic_TileRange_BoxInOneTile_ShouldEnumerateOneTile()
        {
            var tiles = ((4.796, 51.267), (4.798, 51.265)).TileRange(14);
            
            Assert.NotEmpty(tiles);
            Assert.Single(tiles);
        }
    }
}