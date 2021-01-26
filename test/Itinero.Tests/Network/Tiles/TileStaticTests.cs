using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles {
    public class TileStaticTests {
        [Fact]
        public void TileStatic_TileRange_Box_X1_Y1_ShouldEnumerate1Tile() {
            var tiles = ((4.796, 51.267, (float?) null), (4.798, 51.265, (float?) null)).TileRange(14);

            Assert.NotEmpty(tiles);
            Assert.Single(tiles);
        }

        [Fact]
        public void TileStatic_TileRange_Box_X2_Y1_ShouldEnumerate2Tiles() {
            var tiles = ((5.959307791040118, 49.94263396894212, (float?) null),
                (5.987251161291425, 49.94263396894212, (float?) null)).TileRange(14);

            var expectedTiles = new HashSet<(uint x, uint y)>(new[] {
                (8463U, 5560U),
                (8464U, 5560U)
            });
            Assert.Equal(expectedTiles.Count(), tiles.Count());
            expectedTiles.ExceptWith(tiles);
            Assert.Empty(expectedTiles);
        }

        [Fact]
        public void TileStatic_TileRange_Box_X1_Y2_ShouldEnumerate2Tiles() {
            var tiles = ((5.959307791040118, 49.94263396894212, (float?) null),
                (5.959307791040118, 49.92464753682374, (float?) null)).TileRange(14);

            var expectedTiles = new HashSet<(uint x, uint y)>(new[] {
                (8463U, 5560U),
                (8463U, 5561U)
            });
            Assert.Equal(expectedTiles.Count(), tiles.Count());
            expectedTiles.ExceptWith(tiles);
            Assert.Empty(expectedTiles);
        }

        [Fact]
        public void TileStatic_TileRange_Box_X2_Y2_ShouldEnumerate4Tiles() {
            var tiles = ((5.959307791040118, 49.94263396894212, (float?) null),
                (5.987251161291425, 49.92464753682374, (float?) null)).TileRange(14);

            var expectedTiles = new HashSet<(uint x, uint y)>(new[] {
                (8463U, 5560U),
                (8463U, 5561U),
                (8464U, 5560U),
                (8464U, 5561U)
            });
            Assert.Equal(expectedTiles.Count(), tiles.Count());
            expectedTiles.ExceptWith(tiles);
            Assert.Empty(expectedTiles);
        }
    }
}