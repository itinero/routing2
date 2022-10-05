using System;
using System.IO;
using Itinero.Network.Mutation;
using Itinero.Network.Serialization;

namespace Itinero;

/// <summary>
/// Extensions related to the router db.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Mutate the router db using a delegate.
    /// </summary>
    /// <param name="routerDb">The router db.</param>
    /// <param name="mutate">The delegate.</param>
    public static void Mutate(this RouterDb routerDb, Action<RoutingNetworkMutator> mutate)
    {
        using var mutable = routerDb.GetMutableNetwork();
        mutate(mutable);
    }
}
