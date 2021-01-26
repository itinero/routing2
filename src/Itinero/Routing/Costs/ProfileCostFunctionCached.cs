using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routing.Costs.EdgeTypes;

namespace Itinero.Routing.Costs
{
    internal class ProfileCostFunctionCached : ICostFunction
    {
        private readonly Profile _profile;
        private readonly EdgeFactorCache _edgeFactorCache;

        internal ProfileCostFunctionCached(Profile profile, EdgeFactorCache edgeFactorCache)
        {
            _profile = profile;
            _edgeFactorCache = edgeFactorCache;
        }

        public (bool canAccess, bool canStop, double cost, double turnCost) Get(
            IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool forward,
            IEnumerable<(EdgeId edgeId, byte? turn)> previousEdges)
        {
            // get edge factor and length.
            EdgeFactor factor;
            var edgeTypeId = edgeEnumerator.EdgeTypeId;
            if (edgeTypeId == null) {
                factor = _profile.FactorInEdgeDirection(edgeEnumerator);
            }
            else {
                var edgeFactor = _edgeFactorCache.Get(edgeTypeId.Value);
                if (edgeFactor == null) {
                    factor = _profile.FactorInEdgeDirection(edgeEnumerator);
                    _edgeFactorCache.Set(edgeTypeId.Value, factor);
                }
                else {
                    factor = edgeFactor.Value;
                }
            }

            var lengthNullable = edgeEnumerator.Length;
            var length = lengthNullable ??
                         (uint) (edgeEnumerator.EdgeLength<IEdgeEnumerator<RoutingNetwork>, RoutingNetwork>() * 100);
            var cost = forward ? factor.ForwardFactor * length : factor.BackwardFactor * length;
            var canAccess = cost > 0;

            var totalTurnCost = 0.0;
            var (_, turn) = previousEdges.FirstOrDefault();
            if (turn != null) {
                var turnCosts = forward
                    ? edgeEnumerator.GetTurnCostTo(turn.Value)
                    : edgeEnumerator.GetTurnCostFrom(turn.Value);

                foreach (var (turnCostType, turnCost) in turnCosts) {
                    var turnCostAttributes =
                        edgeEnumerator.Network.RouterDb.GetTurnCostType(turnCostType);
                    var turnCostFactor = _profile.TurnCostFactor(turnCostAttributes);
                    if (turnCostFactor.IsBinary && turnCost > 0) {
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