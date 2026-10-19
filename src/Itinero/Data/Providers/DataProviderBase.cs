using System;
using System.Collections.Generic;
using Itinero.Indexes;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Data.Providers;

/// <summary>
/// A provider that writes: you write to it, and it mutates the db it is attached to.
/// </summary>
/// <remarks>
/// A class rather than an interface because deriving from it is the only way to get write access,
/// and nothing can be a provider without that. A db has no write surface of its own — the
/// direction is provider to db, never the reverse.
/// </remarks>
public abstract class DataProviderBase
{
    /// <summary>
    /// Creates a provider, and the db it fills.
    /// </summary>
    /// <param name="edgeTypeIndex">The edge types, id to attributes.</param>
    /// <param name="turnCostTypeIndex">The turn cost types, id to attributes.</param>
    protected DataProviderBase(IAttributeSetIndex edgeTypeIndex, IAttributeSetIndex turnCostTypeIndex)
    {
        this.RouterDb = new RouterDb(this, edgeTypeIndex, turnCostTypeIndex);
    }

    /// <summary>
    /// The db this provider fills. Created with the provider, because nothing else can create one.
    /// </summary>
    public RouterDb RouterDb { get; }

    /// <summary>
    /// Replaces the db's network with one read from the given stream.
    /// </summary>
    public void LoadFrom(System.IO.Stream stream)
    {
        this.RouterDb.LoadFrom(stream);
    }

    /// <summary>
    /// Opens the means to write. Disposing what it returns publishes the result.
    /// </summary>
    /// <remarks>
    /// Returns a scope rather than exposing a builder and a commit, so the write slot cannot be
    /// left held: there is one object, its lifetime is the write, and disposal is the publish.
    /// </remarks>
    protected NetworkBuilder BeginWriting()
    {
        return new NetworkBuilder(this.RouterDb.GetMutableNetwork());
    }

    /// <summary>
    /// Gets the attributes the edge type map dropped, if any.
    /// </summary>
    /// <param name="edge">The edge, which carries the tile a tile-keyed provider needs.</param>
    /// <param name="globalId">The global id, the stable key.</param>
    public abstract IEnumerable<(string key, string value)> GetAttributes(EdgeId edge, GlobalEdgeId globalId);

    /// <summary>
    /// Appends the edge's shape points to the given list, in the edge's own direction.
    /// </summary>
    /// <remarks>
    /// Into a caller-supplied list rather than returned: snapping asks for every candidate near an
    /// endpoint and discards most of them, so a returned enumerable is an allocation per discard.
    /// </remarks>
    /// <param name="edge">The edge, which carries the tile a tile-keyed provider needs.</param>
    /// <param name="globalId">The global id, the stable key.</param>
    /// <param name="into">The list to append to. Not cleared.</param>
    public abstract void GetShape(EdgeId edge, GlobalEdgeId globalId,
        List<(double longitude, double latitude, float? e)> into);
}
