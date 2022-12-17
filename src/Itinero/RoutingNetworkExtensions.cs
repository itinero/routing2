using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero;

/// <summary>
/// Contains extension methods as entry points to snapping or routing.
/// </summary>
public static class RoutingNetworkExtensions
{
    /// <summary>
    /// Creates a snapper.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="profiles">Zero or more profiles.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A snapper.</returns>
    public static ISnapper Snap(this RoutingNetwork routingNetwork, IEnumerable<Profile> profiles, Action<SnapperSettings>? settings = null)
    {
        var s = new SnapperSettings();
        settings?.Invoke(s);

        return new Snapper(routingNetwork, profiles, s.AnyProfile, s.CheckCanStopOn, s.OffsetInMeter, s.OffsetInMeterMax, s.MaxDistance);
    }

    /// <summary>
    /// Creates a snapper.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="profile">The vehicle profile.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A snapper.</returns>
    public static ISnapper Snap(this RoutingNetwork routingNetwork, Profile profile, Action<SnapperSettings>? settings = null)
    {
        return routingNetwork.Snap(new[] { profile }, settings);
    }

    /// <summary>
    /// Creates a snapper.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="profile">The vehicle profile.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A snapper.</returns>
    public static ISnapper Snap(this RoutingNetwork routingNetwork, Action<SnapperSettings>? settings = null)
    {
        return routingNetwork.Snap(ArraySegment<Profile>.Empty, settings);
    }

    /// <summary>
    /// Creates a router.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="profile">The vehicle profile.</param>
    /// <returns>The router.</returns>
    public static IRouter Route(this RoutingNetwork routingNetwork, Profile profile)
    {
        return new Router(routingNetwork, new RoutingSettings
        {
            Profile = profile
        });
    }

    /// <summary>
    /// Creates a router.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The router.</returns>
    public static IRouter Route(this RoutingNetwork routingNetwork, RoutingSettings settings)
    {
        return new Router(routingNetwork, settings);
    }
}
