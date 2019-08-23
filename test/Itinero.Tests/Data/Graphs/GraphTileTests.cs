using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTileTests
    {
        [Fact]
        public void GraphTile_AddVertexLocal_ShouldReturnLocalId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.7868, 51.2643, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate a new local id.
            
            vertex1 = graphTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }
    }
}