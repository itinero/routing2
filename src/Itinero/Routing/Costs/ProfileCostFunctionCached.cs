using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routing.Costs.Caches;

namespace Itinero.Routing.Costs
{
    internal class ProfileCostFunctionCached : ICostFunction
    {
        private readonly Profile _profile;
        private readonly EdgeFactorCache _edgeFactorCache;
        private readonly TurnCostFactorCache _turnCostFactorCache;

        internal ProfileCostFunctionCached(Profile profile, EdgeFactorCache edgeFactorCache, TurnCostFactorCache turnCostFactorCache)
        {
            _profile = profile;
            _edgeFactorCache = edgeFactorCache;
            _turnCostFactorCache = turnCostFactorCache;
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
                    // no cached value, cache forward value.
                    factor = _profile.Factor(edgeEnumerator.Attributes);
                    _edgeFactorCache.Set(edgeTypeId.Value, factor);
                }
                else {
                    factor = edgeFactor.Value;
                }
                    
                // cached value is always forward.
                if (!edgeEnumerator.Forward) {
                    factor = factor.Reverse;
                }
            }

            var lengthNullable = edgeEnumerator.Length;
            var length = lengthNullable ??
                         (uint) (edgeEnumerator.EdgeLength<IEdgeEnumerator<RoutingNetwork>, RoutingNetwork>() * 100);
            var cost = forward ? factor.ForwardFactor * length : factor.BackwardFactor * length;
            var canAccess = cost > 0;

            var totalTurnCost = 0.0; 
            var (_, turn) = previousEdges.FirstOrDefault();
            if (turn == null) return (canAccess, factor.CanStop, cost, totalTurnCost);
            var turnCosts = edgeEnumerator.GetTurnCostToTail(turn.Value);

            foreach (var (turnCostType,attributes, turnCost, prefixEdges) in turnCosts) {
                // TODO: compare prefix edges with the previous edges.

                var turnCostFactor = _turnCostFactorCache.Get(turnCostType);
                if (turnCostFactor == null)
                {
                    turnCostFactor = _profile.TurnCostFactor(attributes);
                    _turnCostFactorCache.Set(turnCostType, turnCostFactor.Value);
                }
                if (turnCostFactor.Value.IsBinary && turnCost > 0) {
                    totalTurnCost = double.MaxValue;
                    break;
                }

                totalTurnCost += turnCostFactor.Value.CostFactor * turnCost;
            }

            return (canAccess, factor.CanStop, cost, totalTurnCost);
        }
    }
}