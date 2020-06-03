using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles;

namespace Itinero.Profiles.EdgeTypes
{
    internal static class ProfileExtensions
    {
        public static IEnumerable<(string key, string value)> GetEdgeProfileFor(this Profile profile,
            IEnumerable<(string key, string value)> attributes)
        {
            return new[] {profile}.GetEdgeProfileFor(attributes);
        }

        public static IEnumerable<(string key, string value)> GetEdgeProfileFor(this IEnumerable<Profile> profiles,
            IEnumerable<(string key, string value)> attributes)
        {
            var allHash = profiles.GetEdgeFactorHash(attributes);

            var removed = new HashSet<string>();
            var count = -1;
            while (removed.Count != count)
            {
                count = removed.Count;
                foreach (var (key, value) in attributes)
                {
                    if (removed.Contains(key)) continue;
                    
                    var hash = profiles.GetEdgeFactorHash(attributes.Where(x => 
                        x.key != key && !removed.Contains(x.key)));
                    if (hash == allHash)
                    {
                        removed.Add(key);
                    }
                }
            }

            return attributes.Where(x => !removed.Contains(x.key));
        }
        
        private static int GetEdgeFactorHash(this IEnumerable<Profile> profiles, IEnumerable<(string key, string value)> attributes)
        {
            var hash = 13.GetHashCode();
            
            foreach (var profile in profiles)
            {
                var factor = profile.Factor(attributes);
                hash ^= factor.GetHashCode();
            }

            return hash;
        }
    }
}