using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Data.Usage;
using Itinero.Data.Providers;
using Itinero.Indexes;
using Itinero.IO;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Network.Serialization;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero;

/// <summary>
/// Represents a router db.
/// </summary>
/// <remarks>
/// This is mostly a wrapper around a RoutingNetwork.
/// If an update happens to the underlying routing network, this update will be presented here atomically.
/// In other words, to do route planning, use routerDb.getLatest() to get a consistent routing network
/// </remarks>
public sealed partial class RouterDb : IRouterDbMutable
{
    /// <summary>
    /// Creates a new router db for the given provider.
    /// </summary>
    /// <remarks>
    /// Internal: a db is created by its provider, which is the only thing that can fill one.
    /// </remarks>
    internal RouterDb(DataProviderBase provider,
        IAttributeSetIndex edgeTypeIndex, IAttributeSetIndex turnCostTypeIndex)
    {
        this.Provider = provider;
        this.EdgeTypeIndex = edgeTypeIndex;
        this.TurnCostTypeIndex = turnCostTypeIndex;
        this.Latest = new RoutingNetwork(this);
    }

    /// <summary>
    /// Replaces this db's network with one read from the given stream.
    /// </summary>
    internal void LoadFrom(Stream stream)
    {
        // check version #.
        var version = stream.ReadVarInt32();
        if (version != 1)
        {
            throw new InvalidDataException("Unknown version #.");
        }

        // read network.
        this.Latest = stream.ReadFrom(this);

        // read attributes.
        this.Meta = new List<(string key, string value)>(this.ReadAttributesFrom(stream));
    }

    /// <summary>
    /// Gets the latest.
    /// </summary>
    public RoutingNetwork Latest { get; private set; }
}
