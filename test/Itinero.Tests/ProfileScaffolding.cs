using System.Collections.Generic;
using Itinero.Profiles;

namespace Itinero.Tests
{
    internal static class ProfileScaffolding
    {
        public static Profile Any => new SimpleProfile();

        private class SimpleProfile : Profile
        {
            public override string Name { get; } = "Test";
            public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            {
                return new EdgeFactor(1, 1, 1, 1, true);
            }

            public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
            {
                return Itinero.Profiles.TurnCostFactor.Empty;
            }
        }
    }
}