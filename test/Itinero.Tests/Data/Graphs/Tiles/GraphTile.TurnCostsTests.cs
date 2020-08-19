using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Tiles
{
    public partial class GraphTileTests
    {
        [Fact]
        public void GraphTile_AddTurnCosts_2_Edges_1Turn_ShouldAddTailAndHead()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);
            var vertex3 = graphTile.AddVertex(4.865891933441162,51.267741966756304);

            var edge1 = graphTile.AddEdge(vertex1, vertex2);
            var edge2 = graphTile.AddEdge(vertex2, vertex3);
            
            graphTile.AddTurnCosts(vertex2, 145, new [] { edge1, edge2 }, 
                new uint [,] { { 0, 100 }, { 100, 0 }} );
            
            var enumerator = new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex2);

            while (enumerator.MoveNext())
            {
                if (enumerator.EdgeId == edge1)
                {
                    Assert.Equal((byte)0, enumerator.Head);
                    Assert.Null(enumerator.Tail);
                }
                else if (enumerator.EdgeId == edge2)
                {
                    Assert.Null(enumerator.Head);
                    Assert.Equal((byte)1, enumerator.Tail);
                }
            }
        }
    }
}