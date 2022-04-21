using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Attributes;

namespace Itinero.Profiles.EdgeTypesMap;

internal class ProfileEdgeTypeSetMinimizer
{
    private readonly LruCache<long, (IEnumerable<(string key, string value)> attributes, long edgeFactorHash)>
        _cache = new(100000);
    private readonly Dictionary<string, int> _probablyImportant = new();
    private readonly IReadOnlyCollection<Profile> _profiles;

    public ProfileEdgeTypeSetMinimizer(IReadOnlyCollection<Profile> profiles,
        params string[] defaultKeys)
    {
        _profiles = profiles;
        for (var i = 0; i < defaultKeys.Length; i++) {
            var key = defaultKeys[i];
            _probablyImportant[key] = 1 + defaultKeys.Length - i;
        }
    }

    /// <summary>
    ///     Calculates the smallest attributeset which gives equivalent routing properties for the given profile.
    ///     It'll try to remove as much attributes as possible, while keeping the same speeds and factors.
    ///     Thus:
    ///     <code>
    /// var attributes = ...
    /// var edgeFactors = profiles.Select(profile => profile.Factor(attributes));
    /// var pruned =  new AttributeSetMinimizer(profiles).MinimizeAttributes(attributes);
    /// var edgeFactorsPruned = profiles.Select(profile => profile.Factor(pruned));
    /// Assert.Equals(edgeFactors, edgeFactorsPruned);
    /// </code>
    /// </summary>
    /// <param name="profiles">The profiles where routing should be stable for</param>
    /// <param name="attributes">The attributes to pruned</param>
    /// <returns>A pruned set of attributes</returns>
    public IEnumerable<(string key, string value)> MinimizeAttributes(
        IEnumerable<(string key, string value)> attributes)
    {
        if (attributes.Count() <= 1) {
            // Nothing to cull here
            return attributes;
        }

        var attributesHash = attributes.GetHash();
        if (_cache.TryGet(attributesHash, out var prunedSet)) {
            // The current attributes are already known!
            // We simply return the cached set
            return prunedSet.attributes;
        }

        // The hash of the factors for the profiles over the full attributes
        // The hash of the factors for the _pruned_ attributes must be the same in the end!
        var targetHash = this.GetEdgeFactorHash(attributes);

        // We make a ranking of what keys are probably important
        var importantKeys =
            attributes.Select(attr => (attr.key, this.ImportanceOf(attr.key)))
                .OrderByDescending(attr => attr.Item2);

        // The pruned attributes
        HashSet<(string key, string value)> pruned = new();
        long prunedAttributesHash = 0;
        long lastFactorHash = 0;

        // We add the important keys one by one, until we have the same hash
        foreach (var (importantKey, _) in importantKeys) {
            attributes.TryGetValue(importantKey, out var value);

            pruned.Add((importantKey, value));
            prunedAttributesHash = (importantKey, value).GetDiffHash(prunedAttributesHash);

            if (_cache.TryGet(prunedAttributesHash, out var fromCache)) {
                lastFactorHash = fromCache.edgeFactorHash;
                // We've already seen this hash! Lets inspect the previously calculated values...
                if (fromCache.edgeFactorHash == targetHash) {
                    // Hooray! We have found the correct value without doing a thing
                    this.AddImportance(fromCache.attributes);
                    return fromCache.attributes;
                }
            }
            else {
                long currentFactorHash = this.GetEdgeFactorHash(pruned);
                _cache.Add(prunedAttributesHash,
                    (new HashSet<(string key, string value)>(pruned), currentFactorHash));
                if (currentFactorHash == targetHash) {
                    // We have found our smaller configuration!
                    this.AddImportance(pruned);
                    return pruned;
                }

                if (lastFactorHash == currentFactorHash) {
                    // Hmm, this last key didn't change anything... We negatively affect this value in order to prevent it from accidentely floating up
                    this.AddNotImportant(importantKey);
                }

                lastFactorHash = currentFactorHash;
            }
        }

        // Normally, we don't reach this point
        return pruned;
    }

    private void AddImportance(IEnumerable<(string key, string value)> attributes)
    {
        var usedKeys = new HashSet<string>();
        foreach (var (key, _) in attributes) {
            usedKeys.Add(key);
            if (!_probablyImportant.ContainsKey(key)) {
                _probablyImportant[key] = 1;
            }
            else {
                _probablyImportant[key]++;
            }
        }
    }

    private void AddNotImportant(string key)
    {
        if (!_probablyImportant.ContainsKey(key)) {
            _probablyImportant[key] = -1;
        }
        else {
            _probablyImportant[key]--;
        }
    }

    private int ImportanceOf(string key)
    {
        if (_probablyImportant.TryGetValue(key, out var v)) {
            return v;
        }

        return 0;
    }

    /// <summary>
    ///     Calculates, for every profile, the respective factor and hashes them together
    /// </summary>
    private int GetEdgeFactorHash(IEnumerable<(string key, string value)> attributes)
    {
        var hash = 13.GetHashCode();

        foreach (var profile in _profiles) {
            var factor = profile.Factor(attributes);
            hash ^= factor.GetHashCode();
        }

        return hash;
    }
}