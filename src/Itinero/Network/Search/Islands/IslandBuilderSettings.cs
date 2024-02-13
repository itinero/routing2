using Itinero.Profiles;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Settings for the island builder.
/// </summary>
public class IslandBuilderSettings
{
    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    public Profile Profile { get; set; } = null!;

    /// <summary>
    /// The minimum island size, anything larger is not considered an island.
    /// </summary>
    public uint MinIslandSize { get; set; } = 1024;
}
