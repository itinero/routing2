using System;
using Itinero.Profiles.Serialization;
using Itinero.Serialization;

namespace Itinero.IO.Osm.Tiles
{
    /// <summary>
    /// Contains extension methods to manage deserialization settings.
    /// </summary>
    public static class RouterDbDeserializationSettingsExtensions
    {
        /// <summary>
        /// Configures the router db serializer to use a default profile serializer.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public static void RegisterDataProvider(this RouterDbDeserializationSettings settings)
        {
            settings.Use("Itinero.IO.Osm.Tiles.DataProvider", (routerDb, stream) =>
            {
                DataProvider.ReadFrom(stream, routerDb);
            });
        }
    }
}