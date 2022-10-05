using System;

namespace Itinero.IO.Osm.Tiles;

/// <summary>
/// Contains extension methods for the router db.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Configures the routeable tiles data provide.
    /// </summary>
    /// <param name="routerDb">The router db.</param>
    /// <param name="configure">The configure function.</param>
    public static void UseRouteableTiles(this RouterDb routerDb, Action<DataProviderSettings>? configure = null)
    {
        var settings = new DataProviderSettings();

        configure?.Invoke(settings);

        var dataProvider = new DataProvider(routerDb, settings.Url, settings.Zoom);
    }
}
