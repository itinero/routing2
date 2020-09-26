using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Reading;
using Itinero.Profiles;

namespace Itinero.Costs
{
    internal class ProfileCostFunction : ICostFunction
    {
        private readonly Profile _profile;

        public ProfileCostFunction(Profile profile)
        {
            _profile = profile;
        }

        public (bool canAccess, bool canStop, double cost, double turnCost) Get(IGraphEdge<Graph> edgeEnumerator, bool forward,
            IEnumerable<(EdgeId edgeId, byte? turn)> previousEdges)
        {
            var factor = edgeEnumerator.FactorInEdgeDirection<Graph>(_profile);
            var length =  edgeEnumerator.Length ?? (uint) (edgeEnumerator.EdgeLength<IGraphEdge<Graph>, Graph>() * 100);
            var cost = forward ? factor.ForwardFactor * length : factor.BackwardFactor * length;
            var canAccess = cost > 0;

            var totalTurnCost = 0.0;
            var (_, turn) = previousEdges.FirstOrDefault();
            if (turn != null)
            {
                var turnCosts = forward ? edgeEnumerator.GetTurnCostTo(turn.Value) : 
                    edgeEnumerator.GetTurnCostFrom(turn.Value);

                foreach (var (turnCostType, turnCost) in turnCosts)
                {
                    var turnCostAttributes =
                        edgeEnumerator.Graph.GetTurnCostTypeAttributes(turnCostType);
                    var turnCostFactor = _profile.TurnCostFactor(turnCostAttributes);
                    if (turnCostFactor.IsBinary && turnCost > 0)
                    {
                        totalTurnCost = double.MaxValue;
                        break;
                    }
                    totalTurnCost += turnCostFactor.CostFactor * turnCost;
                }
            }
            
            return (canAccess, factor.CanStop, cost, totalTurnCost);
        }
    }
}