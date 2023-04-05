using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.Costs.Caches;

[assembly: InternalsVisibleTo("Itinero.Geo")]

namespace Itinero.Network;

/// <summary>
/// Extensions methods for routing networks.
/// </summary>
public static class RoutingNetworkSnapshotExtensions
{
    /// <summary>
    /// Gets the location of the given vertex.
    /// </summary>
    /// <param name="routingNetwork">The network</param>
    /// <param name="vertex">The vertex.</param>
    /// <returns>The location.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the vertex doesn't exist.</exception>
    public static (double longitude, double latitude, float? e) GetVertex(this RoutingNetwork routingNetwork,
        VertexId vertex)
    {
        if (!routingNetwork.TryGetVertex(vertex, out var longitude, out var latitude, out var e))
        {
            throw new ArgumentOutOfRangeException(nameof(vertex));
        }

        return (longitude, latitude, e);
    }

    /// <summary>
    /// Gets an enumerable with all vertices.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="box">The bounding box, if any.</param>
    /// <returns>An enumerable with all vertices.</returns>
    public static IEnumerable<VertexId> GetVertices(this RoutingNetwork routingNetwork,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight)? box = null)
    {
        if (box == null)
        {
            var enumerator = routingNetwork.GetVertexEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
        else
        {
            var vertices = routingNetwork.SearchVerticesInBox(box.Value);
            foreach (var (vertex, _) in vertices)
            {
                yield return vertex;
            }
        }
    }

    /// <summary>
    /// Gets an enumerable with all edges.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="box">The bounding box, if any.</param>
    /// <returns>An enumerable with all edges.</returns>
    public static IEnumerable<EdgeId> GetEdges(this RoutingNetwork routingNetwork,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight)? box = null)
    {
        var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
        foreach (var vertex in routingNetwork.GetVertices(box))
        {
            if (!edgeEnumerator.MoveTo(vertex))
            {
                continue;
            }

            while (edgeEnumerator.MoveNext())
            {
                if (!edgeEnumerator.Forward)
                {
                    continue;
                }

                yield return edgeEnumerator.EdgeId;
            }
        }
    }

    /// <summary>
    /// Gets a cached version of the given profile.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>The cached profile.</returns>
    public static ProfileCached GetCachedProfile(this RoutingNetwork network, Profile profile)
    {
        if (!network.RouterDb.ProfileConfiguration.TryGetProfileHandlerEdgeTypesCache(profile.Name, out var cache, out var turnCostFactorCache))
        {
            return new ProfileCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
        }

        return new ProfileCached(profile, cache ?? new EdgeFactorCache(), turnCostFactorCache ?? new TurnCostFactorCache());
    }

    /// <summary>
    /// Gets the cost function for the given profile.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>The cost function.</returns>
    public static ICostFunction GetCostFunctionFor(this RoutingNetwork network, Profile profile)
    {
        if (!network.RouterDb.ProfileConfiguration.TryGetProfileHandlerEdgeTypesCache(profile.Name, out var cache, out var turnCostFactorCache))
        {
            return new ProfileCostFunction(profile);
        }

        return new ProfileCostFunctionCached(profile, cache ?? new EdgeFactorCache(), turnCostFactorCache ?? new TurnCostFactorCache());
    }

    internal static IEnumerable<(string key, string value)>
        GetAttributes(this RoutingNetwork routerDb, EdgeId edge)
    {
        var enumerator = routerDb.GetEdgeEnumerator();
        if (!enumerator.MoveTo(edge))
        {
            return Enumerable.Empty<(string key, string value)>();
        }

        return enumerator.Attributes;
    }
}
