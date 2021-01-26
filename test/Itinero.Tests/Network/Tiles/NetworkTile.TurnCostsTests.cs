using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles {
    public class NetworkTile_TurnCostsTests {
        [Fact]
        public void NetworkTile_AddTurnCosts_2_Edges_1Turn_ShouldAddTailAndHead() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);
            var vertex3 = graphTile.AddVertex(4.865891933441162, 51.267741966756304);

            var edge1 = graphTile.AddEdge(vertex1, vertex2);
            var edge2 = graphTile.AddEdge(vertex2, vertex3);

            graphTile.AddTurnCosts(vertex2, 145, new[] {edge1, edge2},
                new uint[,] {{0, 100}, {100, 0}});

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex2);

            while (enumerator.MoveNext()) {
                if (enumerator.EdgeId == edge1) {
                    Assert.Equal((byte) 0, enumerator.Head);
                    Assert.Null(enumerator.Tail);
                }
                else if (enumerator.EdgeId == edge2) {
                    Assert.Null(enumerator.Head);
                    Assert.Equal((byte) 1, enumerator.Tail);
                }
            }
        }

        [Fact]
        public void NetworkTile_AddTurnCosts_2_Edges_1Turn_ShouldSetTurnCosts() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);
            var vertex3 = graphTile.AddVertex(4.865891933441162, 51.267741966756304);

            var edge1 = graphTile.AddEdge(vertex1, vertex2);
            var edge2 = graphTile.AddEdge(vertex2, vertex3);

            graphTile.AddTurnCosts(vertex2, 145, new[] {edge1, edge2},
                new uint[,] {{0, 145454}, {79878, 0}});

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex2);

            while (enumerator.MoveNext()) {
                if (enumerator.EdgeId == edge1) {
                    var costs = enumerator.GetTurnCostTo(1).ToList();
                    // cost from edge2 -> edge1
                    Assert.Single(costs);
                    var cost = costs[0];
                    Assert.Equal(145U, cost.turnCostType);
                    Assert.Equal(79878U, cost.cost);
                }
                else if (enumerator.EdgeId == edge2) {
                    var costs = enumerator.GetTurnCostTo(0).ToList();
                    // cost from edge1 -> edge2
                    Assert.Single(costs);
                    var cost = costs[0];
                    Assert.Equal(145U, cost.turnCostType);
                    Assert.Equal(145454U, cost.cost);
                }
            }
        }

        [Fact]
        public void NetworkTile_AddTurnCosts_2_Edges_1Turn_2Tables_ShouldSetTurnCosts() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);
            var vertex3 = graphTile.AddVertex(4.865891933441162, 51.267741966756304);

            var edge1 = graphTile.AddEdge(vertex1, vertex2);
            var edge2 = graphTile.AddEdge(vertex2, vertex3);

            graphTile.AddTurnCosts(vertex2, 145, new[] {edge1, edge2},
                new uint[,] {{0, 145454}, {79878, 0}});
            graphTile.AddTurnCosts(vertex2, 456, new[] {edge1, edge2},
                new uint[,] {{0, 13144}, {46823, 0}});

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex2);

            while (enumerator.MoveNext()) {
                if (enumerator.EdgeId == edge1) {
                    var costs = enumerator.GetTurnCostTo(1).ToList();
                    // cost from edge2 -> edge1
                    Assert.Equal(2, costs.Count);
                    var cost = costs[0];
                    Assert.Equal(456U, cost.turnCostType);
                    Assert.Equal(46823U, cost.cost);
                    cost = costs[1];
                    Assert.Equal(145U, cost.turnCostType);
                    Assert.Equal(79878U, cost.cost);
                }
                else if (enumerator.EdgeId == edge2) {
                    var costs = enumerator.GetTurnCostTo(0).ToList();
                    // cost from edge1 -> edge2
                    Assert.Equal(2, costs.Count);
                    var cost = costs[0];
                    Assert.Equal(456U, cost.turnCostType);
                    Assert.Equal(13144U, cost.cost);
                    cost = costs[1];
                    Assert.Equal(145U, cost.turnCostType);
                    Assert.Equal(145454U, cost.cost);
                }
            }
        }
    }
}