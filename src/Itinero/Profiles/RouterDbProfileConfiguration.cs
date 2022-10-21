using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles.EdgeTypesMap;
using Itinero.Routing.Costs.Caches;

namespace Itinero.Profiles;

internal class RouterDbProfileConfiguration
{
    private readonly Dictionary<string, (Profile profile, EdgeFactorCache cache, TurnCostFactorCache turnCostFactorCache)> _profiles;
    private readonly RouterDb _routerDb;

    public RouterDbProfileConfiguration(RouterDb routerDb)
    {
        _routerDb = routerDb;
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

        this.UpdateEdgeTypeMap();
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

    private void UpdateEdgeTypeMap()
    {
        // only update the edge type map when it is based on the active profiles.
        if (_routerDb.EdgeTypeMap is not ProfilesEdgeTypeMap)
        {
            return;
        }

        // update edge type map to include the new profile(s).
        var edgeTypeMap = new ProfilesEdgeTypeMap(_profiles.Values.Select(x => x.profile));
        _routerDb.EdgeTypeMap = edgeTypeMap;
    }

    public IEnumerable<Profile> Profiles => _profiles.Values.Select(x => x.profile);
}
