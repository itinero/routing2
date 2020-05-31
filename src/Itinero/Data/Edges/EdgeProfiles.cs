using System.Collections.Generic;
using Itinero.Profiles;

namespace Itinero.Data.Edges
{
    internal class EdgeProfiles
    {
        private readonly uint _id;
        private readonly Dictionary<string, Profile> _profiles;

        public EdgeProfiles()
        {
            _id = 1;
            _profiles = new Dictionary<string, Profile>();
        }

        private EdgeProfiles(EdgeProfiles edgeProfiles, Profile profile)
        {
            _id = edgeProfiles._id + 1;
            _profiles = new Dictionary<string, Profile>(edgeProfiles._profiles) {{profile.Name, profile}};
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public uint Id => _id;

        /// <summary>
        /// Applies the given profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        public EdgeProfiles Apply(Profile profile)
        {
            if (_profiles.ContainsKey(profile.Name))
            {
                return this;
            }
            return new EdgeProfiles(this, profile);
        }

        /// <summary>
        /// Returns true if the edge profiles apply to the given profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <returns>True if the profile applies to the given edge profiles.</returns>
        public bool AppliesTo(Profile profile)
        {
            return _profiles.ContainsKey(profile.Name);
        }

        /// <summary>
        /// Gets the number of edge profiles.
        /// </summary>
        public uint Count { get; private set; }
        
        /// <summary>
        /// Gets the edge profile id for the given attributes set.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The profile id, if any.</returns>
        public uint? Get(IEnumerable<(string key, string value)> attributes)
        {
            // figure out the edge profile.
            // calculate edge factor.
            
            // calculate edge factor again by removing tags.
            // execute the algorithm to filter out all tags that don't matter.
            
            // 
            
            return null;
        }
    }
}