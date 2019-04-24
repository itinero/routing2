using Itinero.Data.Attributes;

namespace Itinero.Profiles
{
    /// <summary>
    /// Abstract representation of a profile.
    /// </summary>
    public abstract class Profile
    {
        /// <summary>
        /// Gets an edge factor for the given attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>An edge factor.</returns>
        public abstract EdgeFactor Factor(IAttributeCollection attributes);
    }
}