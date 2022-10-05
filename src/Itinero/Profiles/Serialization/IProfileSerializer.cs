using System.IO;

namespace Itinero.Profiles.Serialization
{
    /// <summary>
    /// Abstract representation of a profile serializer.
    /// </summary>
    public interface IProfileSerializer
    {
        /// <summary>
        /// Used to identify the appropriate serializer for a profile.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Writes the given profile to the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="profile">The profile.</param>
        public void WriteTo(Stream stream, Profile profile);

        /// <summary>
        /// Reads a profile from the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The profile.</returns>
        public Profile ReadFrom(Stream stream);
    }
}
