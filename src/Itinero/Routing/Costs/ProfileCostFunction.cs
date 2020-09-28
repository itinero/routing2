using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;

namespace Itinero.Routing.Costs
{
    internal class ProfileCostFunction : ICostFunction
    {
        private readonly Profile _profile;

        public ProfileCostFunction(Profile profile)
        {
            _profile = profile;
        }

        public (bool canAccess, bool canStop, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool forward,
            IEnumerable<(EdgeId edgeId, byte? turn)> previousEdges)
        {
            var factor = _profile.FactorInEdgeDirection(edgeEnumerator);
            var length =  edgeEnumerator.Length ?? (uint) (edgeEnumerator.EdgeLength<IEdgeEnumerator<RoutingNetwork>, RoutingNetwork>() * 100);
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