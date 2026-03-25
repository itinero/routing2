using System.Collections.Generic;
using System.Linq;
using Itinero.Routing.Costs.Caches;

namespace Itinero.Profiles;

internal class RouterDbProfileConfiguration
{
    private readonly Dictionary<string, (Profile profile, EdgeFactorCache cache, TurnCostFactorCache turnCostFactorCache)> _profiles;

    public RouterDbProfileConfiguration()
    {
        _profiles = new Dictionary<string, (Profile profile, EdgeFactorCache cache, TurnCostFactorCache turnCostFactorCache)>();
    }

    internal IEnumerable<string> GetProfileNames()
    {
        return _profiles.Keys.ToList();
    }

    internal bool HasProfile(string name)
    {
        return _profiles.ContainsKey(name);
    }

    public void AddProfiles(IEnumerable<Profile> profiles)
    {
        foreach (var profile in profiles)
        {
            _profiles[profile.Name] = (profile, new EdgeFactorCache(), new TurnCostFactorCache());
        }
    }

    internal bool TryGetProfileHandlerEdgeTypesCache(string profileName, out EdgeFactorCache? cache, out TurnCostFactorCache? turnCostFactorCache)
    {
        cache = null;
        turnCostFactorCache = null;
        if (!_profiles.TryGetValue(profileName, out var profileValue))
        {
            return false;
        }

        cache = profileValue.cache;
        turnCostFactorCache = profileValue.turnCostFactorCache;
        return true;
    }

    public IEnumerable<Profile> Profiles => _profiles.Values.Select(x => x.profile);
}
