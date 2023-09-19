namespace Itinero.Network.Search.Islands;

/// <summary>
/// Settings for the island builder.
/// </summary>
public class IslandBuilderSettings
{
    /// <summary>
    /// The minimum island size, anything larger is not considered an island.
    /// </summary>
    public uint MinIslandSize { get; set; } = 1024;
}
