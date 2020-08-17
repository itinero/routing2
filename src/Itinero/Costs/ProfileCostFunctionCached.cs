using System.Collections.Generic;
using Itinero.Costs.EdgeTypes;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero.Costs
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

        private EdgeFactor _factor;
        private uint _length;

        public void MoveTo(NetworkEdgeEnumerator edgeEnumerator)
        {
            // get edge factor and length.
            var edgeTypeId = edgeEnumerator.EdgeTypeId;
            if (edgeTypeId == null)
            {
                _factor = edgeEnumerator.FactorInEdgeDirection(_profile);
            }
            else
            {
                var edgeFactor = _edgeFactorCache.Get(edgeTypeId.Value);
                if (edgeFactor == null)
                {
                    _factor = edgeEnumerator.FactorInEdgeDirection(_profile);
                    _edgeFactorCache.Set(edgeTypeId.Value, _factor);
                }
                else
                {
                    _factor = edgeFactor.Value;
                }
            
                var length = edgeEnumerator.Length;
                if (length.HasValue)
                {
                    _length = length.Value;
                }
                else
                {
                    _length = (uint)(edgeEnumerator.EdgeLength() * 100);
                }
            }
        }

        public bool CanStop(bool? forward = null)
        {
            return _factor.CanStop;
        }

        public bool CanAccess(bool? forward = null)
        {
            var cost = forward == null || forward.Value ? _factor.ForwardFactor : 0;
            if (cost > 0) return true;
            cost = forward == null || !forward.Value ? _factor.BackwardFactor : 0;
            return cost > 0;
        }

        public double GetCosts(bool forward)
        {
            if (forward)
            {
                return _factor.ForwardFactor * _length;
            }

            return _factor.BackwardFactor * _length;
        }

        public double GetTurnCost(bool forward, IEnumerable<(EdgeId edgeId, ushort? turn)> previousEdges)
        {
            throw new System.NotImplementedException();
        }
    }
}