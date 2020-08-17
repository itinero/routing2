using System.Collections.Generic;
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

        private EdgeFactor _factor;
        private uint _length;

        public void MoveTo(NetworkEdgeEnumerator edgeEnumerator)
        {
            _factor = edgeEnumerator.FactorInEdgeDirection(_profile);

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

        public bool CanAccess(bool? forward = null)
        {
            var cost = forward == null || forward.Value ? _factor.ForwardFactor : 0;
            if (cost > 0) return true;
            cost = forward == null || !forward.Value ? _factor.BackwardFactor : 0;
            return cost > 0;
        }

        public bool CanStop(bool? forward = null)
        {
            return _factor.CanStop;
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