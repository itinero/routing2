using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Data.Usage;
using Itinero.Data.Providers;
using Itinero.Indexes;
using Itinero.Network;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero;

/// <summary>
/// Represents a router db.
/// </summary>
/// <remarks>
/// Holds and publishes networks: a rotation replaces the newest one atomically. A request takes
/// the newest, holds its own reference and finishes on it, so a rotation leaves it alone.
/// </remarks>
public sealed partial class RouterDb
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
    /// Gets the latest.
    /// </summary>
    public RoutingNetwork Latest { get; private set; }
}
