using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.MapMatching.Model;

/// <summary>
/// The model builder. Builds an HMM factor graph for map matching using the
/// Newson &amp; Krumm (2009) probabilistic model:
/// - Emission probability: Gaussian based on GPS noise (sigma_z)
/// - Transition probability: Exponential based on |great-circle distance - route distance| (beta)
/// </summary>
public class ModelBuilder
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly ModelBuilderSettings _settings;

    /// <summary>
    /// Creates a new model builder.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="settings">The settings.</param>
    public ModelBuilder(RoutingNetwork routingNetwork, ModelBuilderSettings? settings = null)
    {
        _routingNetwork = routingNetwork;
        _settings = settings ?? new ModelBuilderSettings();
    }

    /// <summary>
    /// Builds one or more graph models for the given track.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <param name="profile">The profile to match with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One or more graph models with probabilities as costs.</returns>
    public async Task<IEnumerable<GraphModel>> BuildModels(Track track, Profile profile, CancellationToken cancellationToken = default)
    {
        var models = new List<GraphModel>();

        var start = 0;
        while (start < track.Count - 1)
        {
            var (model, lastUsed) = await this.BuildModel(track, profile, start, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return ArraySegment<GraphModel>.Empty;

            if (lastUsed == start)
            {
                // nothing consumed, move next.
                start++;
                continue;
            }

            models.Add(model);
            start = lastUsed;
        }

        return models;
    }

    private async Task<(GraphModel model, int index)> BuildModel(Track track, Profile profile, int start, CancellationToken cancellationToken)
    {
        var model = new GraphModel(track);

        // Newson & Krumm (2009) HMM parameters.
        var sigmaZ = _settings.SigmaZ;
        var beta = _settings.Beta;
        var searchRadius = _settings.SearchRadius;
        var minPointDistance = _settings.MinPointDistance;
        var maxPointSkip = _settings.MaxPointSkip;
        var breakageDistance = _settings.BreakageDistance;
        var maxRouteDistanceFactor = _settings.MaxRouteDistanceFactor;

        // precompute for emission cost: 1 / (2 * sigma_z^2)
        var invDoubleSigmaZSq = 1.0 / (2.0 * sigmaZ * sigmaZ);
        // precompute for transition cost: 1 / beta
        var invBeta = 1.0 / beta;

        var previousLayer = new List<int>();
        var startNode = new GraphNode();
        previousLayer.Add(model.AddNode(startNode));

        var lastPoint = start;
        var lastUsedLocation = start > 0
            ? (track[start].Location.longitude, track[start].Location.latitude, (float?)null)
            : ((double, double, float?))default;
        var consecutiveSkips = 0;

        for (var i = start; i < track.Count; i++)
        {
            var trackPoint = track[i];
            var trackPointLocation = (trackPoint.Location.longitude, trackPoint.Location.latitude, (float?)null);

            // close-point filtering: skip points too close to the last used point.
            if (i > start && lastUsedLocation != default)
            {
                var distToLast = lastUsedLocation.DistanceEstimateInMeter(trackPointLocation);

                // breakage distance: if too far, break the model.
                if (distToLast > breakageDistance)
                {
                    break;
                }

                // skip points too close together.
                if (distToLast < minPointDistance)
                {
                    continue;
                }
            }

            var trackPointLayer = new List<int>();
            var isConnected = false;

            // use a snap bounding box larger than the search radius
            // to account for tile boundaries and long edges in the spatial index.
            var snapBox = Math.Max(searchRadius * 3, 500);
            await foreach (var snapPoint in _routingNetwork.Snap(profile, s =>
                               {
                                   s.OffsetInMeter = snapBox;
                                   s.OffsetInMeterMax = snapBox;
                               })
                               .ToAllAsync(trackPointLocation.longitude, trackPointLocation.latitude, cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) return (new GraphModel(track), start);

                var distanceToCandidate = trackPointLocation.DistanceEstimateInMeter(snapPoint.LocationOnNetwork(_routingNetwork));
                if (distanceToCandidate > searchRadius) continue;

                // Emission cost: Gaussian model (negative log probability, ignoring normalization constant).
                // cost = distance^2 / (2 * sigma_z^2)
                var emissionCost = distanceToCandidate * distanceToCandidate * invDoubleSigmaZSq;

                // add node.
                var node = new GraphNode() { TrackPoint = i, SnapPoint = snapPoint, Cost = emissionCost };
                var nodeId = model.AddNode(node);

                var nodeIsConnected = false;

                // great-circle distance between consecutive track points.
                var greatCircleDistance = 0.0;
                if (lastPoint > start || i > start)
                {
                    var previousTrackPoint = track[lastPoint];
                    var previousTrackPointLocation = (previousTrackPoint.Location.longitude,
                        previousTrackPoint.Location.latitude, (float?)null);
                    greatCircleDistance = previousTrackPointLocation.DistanceEstimateInMeter(trackPointLocation);
                }

                // add edges from previous layer.
                foreach (var previousNode in previousLayer)
                {
                    var previousSnapPoint = model.GetNode(previousNode).SnapPoint;
                    var transitionCost = 0.0;
                    Path? cachedPath = null;
                    var attributes = new List<(string key, string value)>();

                    if (previousSnapPoint != null)
                    {
                        if (previousSnapPoint.Value.EdgeId == snapPoint.EdgeId &&
                            previousSnapPoint.Value.Offset == snapPoint.Offset)
                        {
                            // same snap point, zero transition cost.
                            transitionCost = 0;
                        }
                        else
                        {
                            var maxRouteDistance = greatCircleDistance * maxRouteDistanceFactor;
                            cachedPath = await this.RouteAsync(previousSnapPoint.Value,
                                snapPoint, profile, maxRouteDistance);
                            if (cachedPath == null) continue;

                            var routeDistance = cachedPath.LengthInMeters();

                            // Transition cost: Exponential model (negative log probability).
                            // cost = |greatCircleDistance - routeDistance| / beta
                            var dt = Math.Abs(greatCircleDistance - routeDistance);
                            transitionCost = dt * invBeta;

                            attributes.Add(("great_circle_distance", greatCircleDistance.ToString(CultureInfo.InvariantCulture)));
                            attributes.Add(("route_distance", routeDistance.ToString(CultureInfo.InvariantCulture)));
                            attributes.Add(("dt", dt.ToString(CultureInfo.InvariantCulture)));
                        }
                    }

                    model.AddEdge(new GraphEdge()
                    {
                        Node1 = previousNode,
                        Node2 = nodeId,
                        Cost = transitionCost,
                        Attributes = attributes,
                        CachedPath = cachedPath
                    });
                    isConnected = true;
                    nodeIsConnected = true;
                }

                if (nodeIsConnected)
                {
                    trackPointLayer.Add(nodeId);
                }
            }

            if (!isConnected)
            {
                // try skipping this point before breaking.
                consecutiveSkips++;
                if (consecutiveSkips > maxPointSkip)
                {
                    break;
                }
                continue;
            }

            // successfully matched this point: reset skip counter and advance.
            consecutiveSkips = 0;
            previousLayer = trackPointLayer;
            lastPoint = i;
            lastUsedLocation = trackPointLocation;
        }

        var endNode = new GraphNode();
        var endNodeId = model.AddNode(endNode);

        // add edges from previous layer to last node.
        foreach (var previousNode in previousLayer)
        {
            model.AddEdge(new GraphEdge()
            {
                Node1 = previousNode,
                Node2 = endNodeId,
                Cost = 0 // last edges have cost 0.
            });
        }

        return (model, lastPoint);
    }

    private async Task<Path?> RouteAsync(SnapPoint snapPoint1, SnapPoint snapPoint2, Profile profile,
        double maxDistance)
    {
        var path = await _routingNetwork.Route(new RoutingSettings() { MaxDistance = maxDistance, Profile = profile })
            .From(snapPoint1).To(snapPoint2).PathAsync(CancellationToken.None);
        if (path.IsError) return null;

        return path.Value;
    }
}
