using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Edges;
using Itinero.Network.Search.Islands;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Snapping;

/// <summary>
/// Just like the `Snapper`, it'll snap to a location.
/// However, the 'Snapper' will match to any road whereas the `LocationSnapper` will only snap to roads accessible to the selected profiles
/// </summary>
internal sealed class Snapper : ISnapper, IEdgeChecker
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly bool _anyProfile;
    private readonly bool _checkCanStopOn;
    private readonly double _offsetInMeter;
    private readonly double _offsetInMeterMax;
    private readonly double _maxDistance;
    private readonly Islands[] _islands;
    private readonly ICostFunction[] _costFunctions;
    private readonly Profile[] _profiles;

    public Snapper(RoutingNetwork routingNetwork, IEnumerable<Profile> profiles, bool anyProfile, bool checkCanStopOn, double offsetInMeter, double offsetInMeterMax, double maxDistance)
    {
        _routingNetwork = routingNetwork;
        _anyProfile = anyProfile;
        _checkCanStopOn = checkCanStopOn;
        _offsetInMeter = offsetInMeter;
        _offsetInMeterMax = offsetInMeterMax;
        _maxDistance = maxDistance;
        _profiles = profiles.ToArray();

        _costFunctions = _profiles.Select(_routingNetwork.GetCostFunctionFor).ToArray();
        _islands = routingNetwork.IslandManager.MaxIslandSize == 0 ? [] : _profiles.Select(p => _routingNetwork.IslandManager.GetIslandsFor(p)).ToArray();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Result<SnapPoint>> ToAsync(VertexId vertexId, bool asDeparture = true)
    {
        var enumerator = _routingNetwork.GetEdgeEnumerator();
        RoutingNetworkEdgeEnumerator? secondEnumerator = null;

        if (!enumerator.MoveTo(vertexId))
        {
            yield break;
        }

        while (enumerator.MoveNext())
        {
            if (_costFunctions.Length == 0)
            {
                if (enumerator.Forward)
                {
                    yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, 0));
                }
                else
                {
                    yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, ushort.MaxValue));
                }
            }
            else
            {
                if (asDeparture)
                {
                    if (!(this.IsAcceptable(enumerator) ?? await (this as IEdgeChecker).RunCheckAsync(enumerator, default)))
                    {

                        continue;
                    }

                    if (enumerator.Forward)
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, 0));
                    }
                    else
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, ushort.MaxValue));
                    }
                }
                else
                {
                    secondEnumerator ??= _routingNetwork.GetEdgeEnumerator();
                    secondEnumerator.MoveTo(enumerator.EdgeId, !enumerator.Forward);
                    if (!(this.IsAcceptable(secondEnumerator) ?? await (this as IEdgeChecker).RunCheckAsync(secondEnumerator, default)))
                    {
                        continue;
                    }

                    if (enumerator.Forward)
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, 0));
                    }
                    else
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, ushort.MaxValue));
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Result<SnapPoint>> ToAsync(EdgeId edgeId, ushort offset, bool forward = true)
    {
        var enumerator = _routingNetwork.GetEdgeEnumerator();

        if (!enumerator.MoveTo(edgeId, forward)) return new Result<SnapPoint>("Edge not found");

        if (!(this.IsAcceptable(enumerator) ?? await (this as IEdgeChecker).RunCheckAsync(enumerator, default)))
            return new Result<SnapPoint>("Edge cannot be snapped to by configured profiles in the given direction");

        return new Result<SnapPoint>(new SnapPoint(edgeId, offset));
    }

    /// <inheritdoc/>
    public async Task<Result<SnapPoint>> ToAsync(
        double longitude, double latitude,
        CancellationToken cancellationToken = default)
    {
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);

        // calculate one box for all locations.
        var box = location.BoxAround(_offsetInMeter);

        // make sure data is loaded.
        await _routingNetwork.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest edge.
        var snapPoint = await _routingNetwork.SnapInBoxAsync(box, this, maxDistance: _maxDistance, cancellationToken);
        if (snapPoint.EdgeId != EdgeId.Empty) return snapPoint;

        // retry only if requested.
        if (!(_offsetInMeter < _offsetInMeterMax))
        {
            return new Result<SnapPoint>(
                FormattableString.Invariant($"Could not snap to location: {location.longitude},{location.latitude}"));
        }

        // use bigger box.
        box = location.BoxAround(_offsetInMeterMax);

        // make sure data is loaded.
        await _routingNetwork.UsageNotifier.NotifyBox(_routingNetwork, box,
            cancellationToken);

        // snap to closest edge.
        snapPoint = await _routingNetwork.SnapInBoxAsync(box, this, maxDistance: _maxDistance, cancellationToken);
        if (snapPoint.EdgeId != EdgeId.Empty)
        {
            return snapPoint;
        }

        return new Result<SnapPoint>(
             FormattableString.Invariant($"Could not snap to location: {location.longitude},{location.latitude}"));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<SnapPoint> ToAllAsync(double longitude, double latitude, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // calculate one box for all locations.
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);
        var box = location.BoxAround(_offsetInMeter);

        // make sure data is loaded.
        await _routingNetwork.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap all.
        var snapped = _routingNetwork.SnapAllInBoxAsync(box, this, maxDistance: _maxDistance, cancellationToken: cancellationToken);
        await foreach (var snapPoint in snapped)
        {
            yield return snapPoint;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<VertexId>> ToVertexAsync(double longitude, double latitude, CancellationToken cancellationToken = default)
    {
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);

        // calculate one box for all locations.
        var box = location.BoxAround(_maxDistance);

        // make sure data is loaded.
        await _routingNetwork.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest vertex.
        var vertex = await _routingNetwork.SnapToVertexInBoxAsync(box, _costFunctions.Length > 0 ? this : null, maxDistance: _maxDistance, cancellationToken: cancellationToken);
        if (vertex.IsEmpty()) return new Result<VertexId>("No vertex in range found");

        return vertex;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<VertexId> ToAllVerticesAsync(double longitude, double latitude,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);

        // calculate one box for all locations.
        var box = location.BoxAround(_maxDistance);

        // make sure data is loaded.
        await _routingNetwork.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest vertex.
        await foreach (var vertex in _routingNetwork.SnapToAllVerticesInBoxAsync(box, this,
                     maxDistance: _maxDistance, cancellationToken: cancellationToken))
        {
            yield return vertex;
        }
    }

    private bool? IsAcceptable(IEdgeEnumerator<RoutingNetwork> edgeEnumerator)
    {
        var hasProfiles = _costFunctions.Length > 0;
        if (!hasProfiles) return true;

        var allOk = true;
        for (var p = 0; p < _costFunctions.Length; p++)
        {
            var costFunction = _costFunctions[p];

            // check if the edge can be used in either direction.
            // both directions need to be checked here because SnapAllInBoxAsync
            // deduplicates by EdgeId: a one-way edge first encountered from its
            // head vertex would be rejected if only the tail-to-head direction is checked.
            var costs = costFunction.Get(edgeEnumerator, true, []);
            var costsReverse = costFunction.Get(edgeEnumerator, false, []);

            // if edge is not accessible in either direction, skip it.
            if (!costs.canAccess && !costsReverse.canAccess)
            {
                allOk = false;
                continue;
            }

            // check if needed if the edge can be stopped on (in either direction).
            if (_checkCanStopOn)
            {
                if (!costs.canStop && !costsReverse.canStop)
                {
                    allOk = false;
                    continue;
                }
            }

            // check if the edge is on an island.
            if (_islands.Length > 0)
            {
                var tailTileId = edgeEnumerator.Forward ? edgeEnumerator.Tail.TileId : edgeEnumerator.Head.TileId;
                var islands = _islands[p];

                // fast path: if the tile is fully done, just check _islandEdges.
                if (islands.GetTileDone(tailTileId))
                {
                    if (islands.IsEdgeOnIsland(edgeEnumerator.EdgeId))
                    {
                        allOk = false;
                        continue;
                    }
                    // tile done + not in island set → not island.
                }
                else
                {
                    // tile not done — check DG for already resolved edges.
                    var onIsland = _routingNetwork.IslandManager.IsEdgeOnIsland(_profiles[p], edgeEnumerator.EdgeId);
                    if (onIsland == true)
                    {
                        allOk = false;
                        continue;
                    }

                    if (onIsland == false)
                    {
                        // confirmed not island.
                    }
                    else
                    {
                        // not yet resolved — return null to trigger async resolution.
                        return null;
                    }
                }
            }

            // any profile is good for a positive result.
            if (_anyProfile) return true;
        }

        return allOk;
    }

    bool? IEdgeChecker.IsAcceptable(IEdgeEnumerator<RoutingNetwork> edgeEnumerator)
    {
        return this.IsAcceptable(edgeEnumerator);
    }

    async Task<bool> IEdgeChecker.RunCheckAsync(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, CancellationToken cancellationToken)
    {
        foreach (var profile in _profiles)
        {
            // Persistent store on IslandManager: every classification's results
            // (NotIsland members of graduated components, Island members of
            // dead-end components) get written back to the per-profile Islands
            // + IslandDirectedGraph the snap fast-path already reads, so
            // subsequent snap/route candidates short-circuit on cached state.
            var store = _routingNetwork.IslandManager.GetClassificationStoreFor(profile);
            var result = await IslandClassifier.ClassifyAsync(_routingNetwork, profile, edgeEnumerator.EdgeId, store, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return true;

            // Only NotIsland is acceptable. Island clearly is not; Unknown
            // means the classifier could not determine (e.g. hit the bounded
            // walk cap) — reject conservatively so we don't snap to a possible
            // island.
            if (result != IslandStatus.NotIsland)
            {
                return false;
            }
        }

        return (this as IEdgeChecker).IsAcceptable(edgeEnumerator) ?? true;
    }
}
