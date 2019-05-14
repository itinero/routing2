using System;
using System.IO;

namespace Itinero.Profiles.Lua.Osm
{
    /// <summary>
    /// Contains a few default embedded OSM profiles.
    /// </summary>
    public static class OsmProfiles
    {
        private static readonly Lazy<Profile> LazyBicycle = new Lazy<Profile>(() =>
        {
            var script = new Script();
            script.DoString(LoadEmbeddedResource("Itinero.Profiles.Lua.Osm.bicycle.lua"));
            return new LuaProfile(script);
        });

        /// <summary>
        /// Gets the default bicycle profile.
        /// </summary>
        public static Profile Bicycle { get; } = LazyBicycle.Value;
        
        /// <summary>
        /// Loads a string from an embedded resource stream.
        /// </summary>
        private static string LoadEmbeddedResource(string name)
        {
            using (var stream = typeof(OsmProfiles).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null) throw new Exception($"Profile {name} not found.");
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}