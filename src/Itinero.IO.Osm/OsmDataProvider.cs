using System;
using Itinero.Data.Providers;
using Itinero.IO.Osm.Streams;
using OsmSharp.Streams;

namespace Itinero.IO.Osm;

/// <summary>
/// A provider that reads an OSM extract and keeps what routing does not read.
/// </summary>
/// <remarks>
/// The default provider with an OSM edge type map and a way to fill itself: it writes every tile
/// while reading, so the whole extract is resident when <see cref="Load"/> returns. Correct for an
/// extract, not for a planet.
/// </remarks>
public sealed class OsmDataProvider : DefaultDataProvider
{
    private readonly OsmStreamSource _data;
    private readonly DataProviderSettings _settings;

    /// <summary>
    /// Creates a new provider over the given OSM data.
    /// </summary>
    /// <param name="data">The OSM data.</param>
    /// <param name="configure">The configure function.</param>
    public OsmDataProvider(OsmStreamSource data, Action<DataProviderSettings>? configure = null)
        : base(new OsmEdgeTypeMap())
    {
        _data = data;
        _settings = new DataProviderSettings();
        configure?.Invoke(_settings);
    }

    /// <summary>
    /// Reads the OSM data and writes it into this provider's db.
    /// </summary>
    public void Load()
    {
        var data = _data;

        // 1: complete objects.
        if (_settings.TagsFilter.CompleteFilter != null)
        {
            data = data.ApplyCompleteFilter(_settings.TagsFilter.CompleteFilter);
        }

        // 2: filter relations.
        if (_settings.TagsFilter.MemberFilter != null)
        {
            data = data.ApplyRelationMemberFilter(_settings.TagsFilter.MemberFilter);
        }

        // 3: filter tags on ways and relations.
        if (_settings.TagsFilter.Filter != null)
        {
            data = data.ApplyFilter(_settings.TagsFilter.Filter);
        }

        using var builder = this.BeginWriting();

        var target = new RouterDbStreamTarget(builder, this, _settings.ElevationHandler);
        target.RegisterSource(data);
        target.Initialize();
        target.Pull();
    }
}
