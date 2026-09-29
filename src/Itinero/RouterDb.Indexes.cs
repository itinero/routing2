using System;
using System.Collections.Generic;
using System.Threading;
using Itinero.Indexes;
using Itinero.Profiles;

namespace Itinero;

public sealed partial class RouterDb
{
    private readonly AttributeSetIndex _edgeTypeIndex;
    private readonly AttributeSetIndex _turnCostTypeIndex;
    private readonly AttributeSetMap _turnCostTypeMap;

    internal RouterDbProfileConfiguration ProfileConfiguration { get; }

    /// <summary>
    /// Gets the edge type attributes for the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The attributes.</returns>
    public IEnumerable<(string key, string value)> GetEdgeType(uint id)
    {
        return _edgeTypeIndex.GetById(id);
    }

    /// <summary>
    /// The edge type map.
    /// </summary>
    internal AttributeSetMap EdgeTypeMap { get; set; }

    /// <summary>
    /// Gets the edge type count.
    /// </summary>
    public long EdgeTypeCount => _edgeTypeIndex.Count;

    /// <summary>
    /// Gets the turn attributes for the given type.
    /// </summary>
    /// <returns>The attributes.</returns>
    // Built fresh on every call before, and GetTileForRead calls this per tile access.
    private sealed class MapCache
    {
        public required Guid Id;
        public required Func<IEnumerable<(string key, string value)>, uint> Func;
        public required AttributeSetMap Map;
    }

    private MapCache? _edgeTypeMapCache;

    /// <summary>
    /// Gets the turn attributes for the given type.
    /// </summary>
    /// <returns>The attributes.</returns>
    internal (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) GetEdgeTypeMap()
    {
        var map = this.EdgeTypeMap;
        var cached = Volatile.Read(ref _edgeTypeMapCache);

        // On the instance, not the id: EdgeTypeMap is settable and a new instance with the same
        // id would otherwise keep serving the old closure.
        if (cached != null && ReferenceEquals(cached.Map, map)) return (cached.Id, cached.Func);

        var built = new MapCache
        {
            Id = map.Id,
            Map = map,
            Func = a => _edgeTypeIndex.Get(map.Map(a)),
        };
        Volatile.Write(ref _edgeTypeMapCache, built);

        return (built.Id, built.Func);
    }

    /// <summary>
    /// Gets the turn cost type count.
    /// </summary>
    public long TurnCostTypeCount => _turnCostTypeIndex.Count;

    /// <summary>
    /// Gets the turn attributes for the given type.
    /// </summary>
    /// <param name="id">The id or index.</param>
    /// <returns>The attributes.</returns>
    public IEnumerable<(string key, string value)> GetTurnCostType(uint id)
    {
        return _turnCostTypeIndex.GetById(id);
    }

    private MapCache? _turnCostTypeMapCache;

    internal (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) GetTurnCostTypeMap()
    {
        var map = _turnCostTypeMap;
        var cached = Volatile.Read(ref _turnCostTypeMapCache);
        if (cached != null && ReferenceEquals(cached.Map, map)) return (cached.Id, cached.Func);

        var built = new MapCache
        {
            Id = map.Id,
            Map = map,
            Func = a => _turnCostTypeIndex.Get(map.Map(a)),
        };
        Volatile.Write(ref _turnCostTypeMapCache, built);

        return (built.Id, built.Func);
    }
}
