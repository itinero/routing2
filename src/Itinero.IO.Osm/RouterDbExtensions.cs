using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Providers;
using Itinero.IO.Osm.Streams;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Itinero.IO.Osm;

/// <summary>
/// Contains extensions method for the router db.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Creates a router db reading the given OSM data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="configure">The configure function.</param>
    public static RouterDb FromOsmData(OsmStreamSource data,
        Action<DataProviderSettings>? configure = null)
    {
        var provider = new OsmDataProvider(data, configure);
        provider.Load();

        return provider.RouterDb;
    }
}
