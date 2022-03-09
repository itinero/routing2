using Itinero.Routes;

namespace Itinero.Instructions.Types;

/// <summary>
/// Extensions on top of the route object to help with instruction generation.
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Gets the attribute or return a default value.
    /// </summary>
    /// <param name="meta">The meta route object.</param>
    /// <param name="key">The key of the attribute to get.</param>
    /// <returns>The value if any, null otherwise.</returns>
    public static string? GetAttributeOrNull(this Route.Meta? meta, string key)
    {
        if (meta == null) {
            return default;
        }

        foreach (var (k, value) in meta.Attributes) {
            if (k.Equals(key)) {
                return value;
            }
        }

        return default;
    }
}