using System;
using Itinero.Network;
using Itinero.Profiles;

namespace Itinero.MapMatching;

/// <summary>
/// Contains extension method for the router class.
/// </summary>
public static class RoutingNetworkExtensions
{
    /// <summary>
    /// Creates a configured map matcher.
    /// </summary>
    /// <param name="routingNetwork">The router db.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>Map matcher results.</returns>
    public static MapMatcher Matcher(this RoutingNetwork routingNetwork, Profile? profile = null)
    {
        return new MapMatcher(routingNetwork, new MapMatcherSettings()
        {
            Profile = profile
        });
    }

    /// <summary>
    /// Creates a configured map matcher.
    /// </summary>
    /// <param name="routingNetwork">The router db.</param>
    /// <param name="setup">A callback to setup.</param>
    /// <returns>Map matcher results.</returns>
    public static MapMatcher Matcher(this RoutingNetwork routingNetwork, Action<MapMatcherSettings>? setup = null)
    {
        var settings = new MapMatcherSettings();
        setup?.Invoke(settings);

        return new MapMatcher(routingNetwork, settings);
    }
}
