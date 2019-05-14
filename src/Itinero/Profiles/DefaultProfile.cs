using Itinero.Data.Attributes;

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
        public override EdgeFactor Factor(IAttributeCollection attributes)
        {
            return new EdgeFactor(1, 1, 1, 1);
        }
    }
}