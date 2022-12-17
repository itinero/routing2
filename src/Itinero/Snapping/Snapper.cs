using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Snapping;

/// <summary>
/// Just like the `Snapper`, it'll snap to a location.
/// However, the 'Snapper' will match to any road whereas the `LocationSnapper` will only snap to roads accessible to the selected profiles
/// </summary>
internal sealed class Snapper : ISnapper
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly bool _anyProfile;
    private readonly bool _checkCanStopOn;
    private readonly double _offsetInMeter;
    private readonly double _offsetInMeterMax;
    private readonly double _maxDistance;
    private readonly ICostFunction[] _costFunctions;
    
    public Snapper(RoutingNetwork routingNetwork, IEnumerable<Profile> profiles, bool anyProfile, bool checkCanStopOn, double offsetInMeter, double offsetInMeterMax, double maxDistance)
    {
        _routingNetwork = routingNetwork;
        _anyProfile = anyProfile;
        _checkCanStopOn = checkCanStopOn;
        _offsetInMeter = offsetInMeter;
        _offsetInMeterMax = offsetInMeterMax;
        _maxDistance = maxDistance;
        
        _costFunctions = profiles.Select(_routingNetwork.GetCostFunctionFor).ToArray();
    }
    
    
    private bool AcceptableFunc(IEdgeEnumerator<RoutingNetwork> edgeEnumerator)
    {
        var hasProfiles = _costFunctions.Length > 0;
        if (!hasProfiles)
        {
            return true;
        }
        else
        {
            var allOk = true;

            foreach (var costFunction in _costFunctions)
            {
                var costs = costFunction.Get(edgeEnumerator, true,
                    Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

                var profileIsOk = costs.canAccess &&
                                  (!_checkCanStopOn || costs.canStop);

                if (_anyProfile && profileIsOk)
                {
                    return true;
                }

                allOk = profileIsOk && allOk;
            }

            return allOk;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Result<SnapPoint>> To(VertexId vertexId, EdgeId? edgeId, bool? asDeparture = null)
    {
        var enumerator = _routingNetwork.GetEdgeEnumerator();
        RoutingNetworkEdgeEnumerator? secondEnumerator = null;

        if (!enumerator.MoveTo(vertexId))
        {
            yield break;
        }

        while (enumerator.MoveNext())
        {
            if (edgeId != null &&
                enumerator.EdgeId != edgeId.Value)
            {
                continue;
            }

            if (_costFunctions.Length == 0 ||
                asDeparture == null)
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
                if (asDeparture.Value)
                {
                    if (!this.AcceptableFunc(enumerator))
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
                    if (!this.AcceptableFunc(secondEnumerator))
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
    public async Task<Result<SnapPoint>> ToAsync(
        double longitude, double latitude,
        CancellationToken cancellationToken = default)
    {
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);

        // calculate one box for all locations.
        var box = location.BoxAround(_offsetInMeter);

        // make sure data is loaded.
        await _routingNetwork.RouterDb.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest edge.
        var snapPoint = _routingNetwork.SnapInBox(box, this.AcceptableFunc, maxDistance: _maxDistance);
        if (snapPoint.EdgeId != EdgeId.Empty) return snapPoint;

        // retry only if requested.
        if (!(_offsetInMeter < _offsetInMeterMax))
        {
            return new Result<SnapPoint>(
                $"Could not snap to location: {location.longitude},{location.latitude}");
        }

        // use bigger box.
        box = location.BoxAround(_offsetInMeterMax);

        // make sure data is loaded.
        await _routingNetwork.RouterDb.UsageNotifier.NotifyBox(_routingNetwork, box,
            cancellationToken);

        // snap to closest edge.
        snapPoint = _routingNetwork.SnapInBox(box, this.AcceptableFunc, maxDistance: _maxDistance);
        if (snapPoint.EdgeId != EdgeId.Empty)
        {
            return snapPoint;
        }

        return new Result<SnapPoint>(
            $"Could not snap to location: {location.longitude},{location.latitude}");
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<SnapPoint> ToAllAsync(double longitude, double latitude, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // calculate one box for all locations.
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);
        var box = location.BoxAround(_offsetInMeter);

        // make sure data is loaded.
        await _routingNetwork.RouterDb.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap all.
        var snapped = _routingNetwork.SnapAllInBox(box, this.AcceptableFunc, maxDistance: _maxDistance);
        foreach (var snapPoint in snapped)
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
        await _routingNetwork.RouterDb.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest vertex.
        return _routingNetwork.SnapToVertexInBox(box, this.AcceptableFunc, maxDistance: _maxDistance);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<VertexId> ToAllVerticesAsync(double longitude, double latitude,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (double longitude, double latitude, float? e) location = (longitude, latitude, null);

        // calculate one box for all locations.
        var box = location.BoxAround(_maxDistance);

        // make sure data is loaded.
        await _routingNetwork.RouterDb.UsageNotifier.NotifyBox(_routingNetwork, box, cancellationToken);

        // snap to closest vertex.
        foreach (var vertex in _routingNetwork.SnapToAllVerticesInBox(box, this.AcceptableFunc,
                     maxDistance: _maxDistance))
        {
            yield return vertex;
        }
    }
}
