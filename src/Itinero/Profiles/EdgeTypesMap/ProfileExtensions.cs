using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Itinero.Profiles.EdgeTypesMap
{
    internal static class ProfileExtensions
    {
        private static readonly Dictionary<int, IEnumerable<(string key, string value)>> Cache = 
            new();
        
        public static IEnumerable<(string key, string value)> GetEdgeProfileFor(this Profile profile,
            IEnumerable<(string key, string value)> attributes)
        {
            return new[] {profile}.GetEdgeProfileFor(attributes);
        }

        private class Comparer : IComparer<(string key, string value)>
        {
            public readonly Dictionary<string, uint> Histogram = new ();
            public readonly (string key, string value)[] Tags = new  (string key, string value)[64];
            
            private Comparer()
            {
                
            }
            
            public int Compare((string key, string value) x, (string key, string value) y)
            {
                if (!Histogram.TryGetValue(x.key, out var xi)) {
                    xi = 0;
                }
                if (!Histogram.TryGetValue(y.key, out var yi)) {
                    yi = 0;
                }

                return -xi.CompareTo(yi);
            }

            public static readonly Comparer Singleton = new Comparer();
        }

        public static IEnumerable<(string key, string value)> GetEdgeProfileFor(this IEnumerable<Profile> profiles,
            IEnumerable<(string key, string value)> attributes)
        {
            var tagsCount = 0;
            var comparer = Comparer.Singleton;
            var tags = comparer.Tags;
            
            var c = 0;
            var allHash = profiles.GetEdgeFactorHash(attributes.Select(x => {
                if (tagsCount > tags.Length) {
                    tags = null;
                } 

                if (tags != null && comparer.Histogram.ContainsKey(x.key)) {
                    tags[tagsCount] = x;
                    tagsCount++;
                }
                
                c++;
                return x;
            }), out var isRelevant);
            if (!isRelevant) {
                return Enumerable.Empty<(string key, string value)>();
            }

            if (Cache.TryGetValue(allHash, out var pruned)) {
                return pruned;
            }

            // if (c <= 1) return attributes;

            if (tagsCount > 0) {
                Array.Sort(tags, 0, tagsCount, comparer);

                var range = 1;
                while (range <= tagsCount) {
                    var hash = profiles.GetEdgeFactorHash(tags.Take(range), out _);
                    if (hash == allHash) {
                        return tags.Take(range);
                    }

                    range++;
                }
            }

            var removed = new HashSet<string>();
            var count = -1;
            while (removed.Count != count) {
                count = removed.Count;
                foreach (var (key, value) in attributes) {
                    if (removed.Contains(key)) {
                        continue;
                    }
            
                    var hash = profiles.GetEdgeFactorHash(attributes.Where(x =>
                        x.key != key && !removed.Contains(x.key)), out _);
                    if (hash == allHash) {
                        removed.Add(key);
                    }
                }
            }
            
            var set = attributes.Where(x => {
                if (removed.Contains(x.key)) return false;
                
                if (!comparer.Histogram.TryGetValue(x.key, out var hc)) {
                    comparer.Histogram[x.key] = 1;
                }
                else if (hc < uint.MaxValue) {
                    comparer.Histogram[x.key] = hc + 1;
                }
                return true;
            });
            Cache[allHash] = set;
            return set;
        }

        private static int GetEdgeFactorHash(this IEnumerable<Profile> profiles,
            IEnumerable<(string key, string value)> attributes, out bool isRelevant)
        {
            var hash = 13.GetHashCode();
            isRelevant = false;
            
            foreach (var profile in profiles) {
                var factor = profile.Factor(attributes);

                if (!factor.IsNoFactor) isRelevant = true;
                
                hash ^= factor.GetHashCode();
            }

            return hash;
        }
    }
}