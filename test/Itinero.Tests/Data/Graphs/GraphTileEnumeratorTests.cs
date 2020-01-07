using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTileEnumeratorTests
    {
        [Fact]
        public void GraphEdgeEnumerator_OneVertex_MoveToVertex_ShouldReturnTrue()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex = graphTile.AddVertex(4.86638, 51.269728);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(vertex));
        }
    }
}