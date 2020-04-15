using System.Collections.Generic;

namespace Itinero.Profiles
{
    /// <summary>
    /// A default profile where all factors are '1'.
    /// </summary>
    public class DefaultProfile : Profile
    {
        /// <inheritdoc/>
        public override string Name { get; } = "Default";

        /// <inheritdoc/>
        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
        {
            return new EdgeFactor(1, 1, 1, 1);
        }
    }
}