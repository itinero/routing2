using System.Collections.Generic;
using Itinero.Profiles;

namespace Itinero.Tests.Profiles
{
    internal static class ProfileScaffolding
    {
        public static Profile Any => new SimpleProfile();
    }

    internal class SimpleProfile : Profile
    {
        public static readonly EdgeFactor DefaultEdgeFactor = new(1, 1, 1, 1);
        private readonly EdgeFactor _edgeFactor;

        public SimpleProfile(EdgeFactor? edgeFactor = null)
        {
            _edgeFactor = edgeFactor ?? DefaultEdgeFactor;
        }

        public override string Name { get; } = "Test";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
        {
            return _edgeFactor;
        }

        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
        {
            return Itinero.Profiles.TurnCostFactor.Empty;
        }
    }
}