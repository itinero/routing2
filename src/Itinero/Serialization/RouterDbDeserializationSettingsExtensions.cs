using System;
using Itinero.Profiles.Serialization;

namespace Itinero.Serialization
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
        /// <param name="conf">The configuration.</param>
        public static void UseDefaultProfileSerializer(this RouterDbDeserializationSettings settings,
            Action<DefaultProfileSerializer>? conf = null)
        {
            var dps = new DefaultProfileSerializer();
            conf?.Invoke(dps);

            settings.ProfileSerializer = dps;
        }
    }
}