using System;
using System.IO;

namespace Itinero.Profiles.Lua.Osm
{
    /// <summary>
    /// Contains a few default embedded OSM profiles.
    /// </summary>
    public static class OsmProfiles
    {
        private static readonly Lazy<Profile> LazyBicycle = new(() =>
        {
            var script = new Script();
            script.DoString(LoadEmbeddedResource("Itinero.Profiles.Lua.Osm.bicycle.lua"));
            return new LuaProfile(script);
        });

        /// <summary>
        /// Gets the default bicycle profile.
        /// </summary>
        public static Profile Bicycle { get; } = LazyBicycle.Value;
        
        private static readonly Lazy<Profile> LazyPedestrian = new(() =>
        {
            var script = new Script();
            script.DoString(LoadEmbeddedResource("Itinero.Profiles.Lua.Osm.pedestrian.lua"));
            return new LuaProfile(script);
        });

        /// <summary>
        /// Gets the default pedestrian profile.
        /// </summary>
        public static Profile Pedestrian { get; } = LazyPedestrian.Value;
        
        /// <summary>
        /// Loads a string from an embedded resource stream.
        /// </summary>
        private static string LoadEmbeddedResource(string name)
        {
            using var stream = typeof(Script).Assembly.GetManifestResourceStream(name);
            if (stream == null) throw new Exception($"Profile {name} not found.");
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}