using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;

namespace Itinero.Profiles.EdgeTypesMap
{
    internal class ProfilesEdgeTypeMap : AttributeSetMap
    {
        public ProfilesEdgeTypeMap(IEnumerable<Profile> profiles)
        {
            var sorted = profiles.ToArray();
            Array.Sort(sorted, (x, y) => 
                string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            // build a hash and take it as the id.
            var hash = 31;
            foreach (var t in profiles)
            {
                hash ^= t.GetHashCode();
            }
            this.Id = hash;
            
            // create the mapping function.
            this.Mapping = a => sorted.GetEdgeProfileFor(a);
        }
    }
}