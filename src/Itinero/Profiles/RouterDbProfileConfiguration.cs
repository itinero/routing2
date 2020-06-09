using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;
using Itinero.Profiles.Handlers.EdgeTypes;
using Itinero.Profiles.Serialization;

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

        public void WriteTo(Stream stream, IProfileSerializer profileSerializer)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write number of profiles.
            stream.WriteVarInt32(_profiles.Count);
            
            // write profiles.
            foreach (var (profile, _) in _profiles.Values)
            {
                stream.WriteProfile(profile, profileSerializer);
            }
        }

        public static RouterDbProfileConfiguration ReadFrom(Stream stream, IProfileSerializer profileSerializer)
        {
            // verify version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Invalid version #.");
            
            // read number of profiles.
            var profileCount = stream.ReadVarInt32();
            
            // write profiles.
            var profiles = new Dictionary<string, (Profile profile, ProfileHandlerEdgeTypesCache cache)>(profileCount);
            for (var p = 0; p < profileCount; p++)
            {
                var profile = profileSerializer.ReadFrom(stream);
                profiles[profile.Name] = (profile, new ProfileHandlerEdgeTypesCache());
            }
            
            return new RouterDbProfileConfiguration(profiles);
        }
    }
}