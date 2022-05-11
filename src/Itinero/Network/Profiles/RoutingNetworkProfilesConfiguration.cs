using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;
using Itinero.Profiles;
using Itinero.Profiles.Serialization;
using Itinero.Routing.Costs.Caches;

namespace Itinero.Network.Profiles
{
    internal class RoutingNetworkProfilesConfiguration
    {
        private readonly Dictionary<string, (Profile profile, EdgeFactorCache cache)> _profiles;

        public RoutingNetworkProfilesConfiguration()
        {
            _profiles = new Dictionary<string, (Profile profile, EdgeFactorCache cache)>();
        }

        private RoutingNetworkProfilesConfiguration(
            Dictionary<string, (Profile profile, EdgeFactorCache cache)> profiles)
        {
            _profiles = new Dictionary<string, (Profile profile, EdgeFactorCache cache)>(profiles);
        }

        public RoutingNetworkProfilesConfiguration Clone()
        {
            return new(_profiles);
        }

        public bool HasProfile(string name)
        {
            return _profiles.ContainsKey(name);
        }

        public bool AddProfile(Profile profile)
        {
            if (_profiles.ContainsKey(profile.Name)) {
                return false;
            }

            _profiles[profile.Name] = (profile, new EdgeFactorCache());
            return true;
        }

        internal bool TryGetProfileHandlerEdgeTypesCache(Profile profile, out EdgeFactorCache? cache)
        {
            cache = null;
            if (!_profiles.TryGetValue(profile.Name, out var profileValue)) {
                return false;
            }

            cache = profileValue.cache;
            return true;
        }

        public IEnumerable<Profile> Profiles => _profiles.Values.Select(x => x.profile);

        public void WriteTo(Stream stream, IProfileSerializer profileSerializer)
        {
            // write version #.
            stream.WriteVarInt32(1);

            // write number of profiles.
            stream.WriteVarInt32(_profiles.Count);

            // write profiles.
            foreach (var (profile, _) in _profiles.Values) {
                stream.WriteProfile(profile, profileSerializer);
            }
        }

        public static RoutingNetworkProfilesConfiguration
            ReadFrom(Stream stream, IProfileSerializer profileSerializer)
        {
            // verify version #.
            var version = stream.ReadVarInt32();
            if (version != 1) {
                throw new InvalidDataException("Invalid version #.");
            }

            // read number of profiles.
            var profileCount = stream.ReadVarInt32();

            // write profiles.
            var profiles = new Dictionary<string, (Profile profile, EdgeFactorCache cache)>(profileCount);
            for (var p = 0; p < profileCount; p++) {
                var profile = profileSerializer.ReadFrom(stream);
                profiles[profile.Name] = (profile, new EdgeFactorCache());
            }

            return new RoutingNetworkProfilesConfiguration(profiles);
        }
    }
}