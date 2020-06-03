using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles.Handlers.EdgeTypes;

namespace Itinero.Profiles
{
    internal class RouterDbProfileConfiguration
    {
        private readonly Dictionary<string, (Profile profile, ProfileHandlerEdgeTypesCache cache)> _profiles;

        public RouterDbProfileConfiguration()
        {
            _profiles = new Dictionary<string, (Profile profile, ProfileHandlerEdgeTypesCache cache)>();
        }

        private RouterDbProfileConfiguration(
            Dictionary<string, (Profile profile, ProfileHandlerEdgeTypesCache cache)> profiles)
        {
            _profiles = new Dictionary<string, (Profile profile, ProfileHandlerEdgeTypesCache cache)>(profiles);
        }

        public RouterDbProfileConfiguration Clone()
        {
            return new RouterDbProfileConfiguration(_profiles);
        }

        public bool HasProfile(string name)
        {
            return _profiles.ContainsKey(name);
        }

        public bool AddProfile(Profile profile)
        {
            if (_profiles.ContainsKey(profile.Name)) return false;
            _profiles[profile.Name] = (profile, new ProfileHandlerEdgeTypesCache());
            return true;
        }

        internal bool TryGetProfileHandlerEdgeTypesCache(Profile profile, out ProfileHandlerEdgeTypesCache? cache)
        {
            cache = null;
            if (!_profiles.TryGetValue(profile.Name, out var profileValue)) return false;

            cache = profileValue.cache;
            return true;
        }

        public IEnumerable<Profile> Profiles => _profiles.Values.Select(x => x.profile);
    }
}