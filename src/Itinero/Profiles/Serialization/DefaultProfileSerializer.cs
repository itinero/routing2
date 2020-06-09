using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data;
using Itinero.Logging;

namespace Itinero.Profiles.Serialization
{
    /// <summary>
    /// A default profile serializer.
    /// </summary>
    /// <remarks>
    /// This serializer only serializes the names of each profile. To deserialize profiles with the same name first have to registered.
    /// </remarks>
    public sealed class DefaultProfileSerializer : IProfileSerializer
    {
        private readonly Dictionary<string, Profile> _profiles;

        /// <summary>
        /// Creates a new default profile serializer.
        /// </summary>
        public DefaultProfileSerializer()
        {
            _profiles = new Dictionary<string, Profile>();
        }

        /// <summary>
        /// Uses the given profile for deserialization.
        /// </summary>
        /// <param name="profile">The profile.</param>
        public void Use(Profile profile)
        {
            _profiles[profile.Name] = profile;
        }

        /// <inheritdoc/>
        public string Id => nameof(DefaultProfileSerializer);

        /// <inheritdoc/>
        public void WriteTo(Stream stream, Profile profile)
        {
            stream.WriteWithSize(profile.Name); 
        }

        /// <inheritdoc/>
        public Profile ReadFrom(Stream stream)
        {
            var name = stream.ReadWithSizeString();
            if (!_profiles.TryGetValue(name, out var profile)) 
                throw new InvalidOperationException($"Could not deserialize profile with key: {name} Register all used profiles before deserialization");

            return profile;
        }
    }
}