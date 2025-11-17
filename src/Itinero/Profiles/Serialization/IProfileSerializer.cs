using System.IO;

namespace Itinero.Profiles.Serialization;

/// <summary>
/// Abstract representation of a profile serializer.
/// </summary>
public interface IProfileSerializer
{
    /// <summary>
    /// Used to identify the appropriate serializer for a profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Writes the given profile to the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="profile">The profile.</param>
    void WriteTo(Stream stream, Profile profile);

    /// <summary>
    /// Reads a profile from the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>The profile.</returns>
    Profile ReadFrom(Stream stream);
}
