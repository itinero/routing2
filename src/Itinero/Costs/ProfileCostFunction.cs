using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
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

        public (bool canAccess, bool canStop, double cost, double turnCost) Get(NetworkEdgeEnumerator edgeEnumerator, bool forward,
            IEnumerable<(EdgeId edgeId, byte? turn)> previousEdges)
        {
            var factor = edgeEnumerator.FactorInEdgeDirection(_profile);
            var length =  edgeEnumerator.Length ?? (uint) (edgeEnumerator.EdgeLength() * 100);
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
                        edgeEnumerator.RouterDb.Graph.GetTurnCostTypeAttributes(turnCostType);
                    var turnCostFactor = _profile.TurnCostFactor(turnCostAttributes);
                    totalTurnCost += turnCostFactor * turnCost;
                }
            }
            
            return (canAccess, factor.CanStop, cost, totalTurnCost);
        }
    }
}