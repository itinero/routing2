using System.Collections.Generic;

namespace Itinero.Profiles
{
    /// <summary>
    /// Abstract representation of a profile.
    /// </summary>
    /// <remarks>
    /// This is a single possible behaviour of a vehicle (e.g. 'bicycle.fastest').
    /// </remarks>
    public abstract class Profile
    {
        /// <summary>
        /// Gets the name of this profile.
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Gets an edge factor for the given attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>An edge factor.</returns>
        public abstract EdgeFactor Factor(IEnumerable<(string key, string value)> attributes);

        /// <summary>
        /// Gets a factor for the turn costs for the given attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>A turn cost factor.</returns>
        public abstract TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes);
    }
}