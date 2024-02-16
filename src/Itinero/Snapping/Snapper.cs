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
        _islands = routingNetwork.IslandManager.MaxIslandSize == 0 ? Array.Empty<Islands>() : _profiles.Select(p => _routingNetwork.IslandManager.GetIslandsFor(p)).ToArray();
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
        foreach (var costFunction in _costFunctions)
        {
            var costs = costFunction.Get(edgeEnumerator, true,
                Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

            var profileIsOk = costs.canAccess &&
                              (!_checkCanStopOn || costs.canStop);

            if (_anyProfile && profileIsOk)
            {
                return IsNotOnIsland();
            }

            allOk = allOk && profileIsOk;
        }

        if (!allOk) return false;

        return IsNotOnIsland();

        bool? IsNotOnIsland()
        {
            var tailIsland = edgeEnumerator.Tail.TileId;
            if (!edgeEnumerator.Forward) tailIsland = edgeEnumerator.Head.TileId;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var island in _islands)
            {
                // when an edge is not an island, it is sure it is not an island.
                var onIsland = island.IsEdgeOnIsland(edgeEnumerator.EdgeId);
                if (onIsland) return false;

                // if it is not on an island we need to check if the tile was done.
                if (!island.GetTileDone(tailIsland)) return null; // inconclusive.
            }

            return true;
        }
    }

    bool? IEdgeChecker.IsAcceptable(IEdgeEnumerator<RoutingNetwork> edgeEnumerator)
    {
        return this.IsAcceptable(edgeEnumerator);
    }

    async Task<bool> IEdgeChecker.RunCheckAsync(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, CancellationToken cancellationToken)
    {
        foreach (var profile in _profiles)
        {
            var tileId = edgeEnumerator.Forward ? edgeEnumerator.Tail.TileId : edgeEnumerator.Head.TileId;
            await _routingNetwork.IslandManager.BuildForTileAsync(_routingNetwork, profile, tileId, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return true;
        }

        return (this as IEdgeChecker).IsAcceptable(edgeEnumerator) ?? throw new Exception("Edges were just calculated");
    }
}
