using System;
using System.IO;

namespace Itinero.Profiles.Lua.Osm
{
    /// <summary>
    /// Contains a few default embedded OSM profiles.
    /// </summary>
    public static class OsmProfiles
    {
        private static readonly Lazy<Profile> LazyBicycle = new(() => LuaProfile.Load(LoadEmbeddedResource("Itinero.Profiles.Lua.Osm.bicycle.lua"),
            "Itinero.Profiles.Lua.Osm.bicycle.lua"));

        /// <summary>
        /// Gets the default bicycle profile.
        /// </summary>
        public static Profile Bicycle { get; } = LazyBicycle.Value;
        
        private static readonly Lazy<Profile> LazyPedestrian = new(() => LuaProfile.Load(LoadEmbeddedResource("Itinero.Profiles.Lua.Osm.pedestrian.lua"),
            "Itinero.Profiles.Lua.Osm.pedestrian.lua"));

        /// <summary>
        /// Gets the default pedestrian profile.
        /// </summary>
        public static Profile Pedestrian { get; } = LazyPedestrian.Value;
        
        /// <summary>
        /// Loads a string from an embedded resource stream.
        /// </summary>
        private static string LoadEmbeddedResource(string name)
        {
            using var stream = typeof(LuaProfile).Assembly.GetManifestResourceStream(name);
            if (stream == null) throw new Exception($"Profile {name} not found.");
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}