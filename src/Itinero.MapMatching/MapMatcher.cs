using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Solver;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing;

[assembly: InternalsVisibleTo("Itinero.MapMatching.Tests")]
[assembly: InternalsVisibleTo("Itinero.MapMatching.Tests.Functional")]

namespace Itinero.MapMatching;

/// <summary>
/// The map matcher.
/// </summary>
public class MapMatcher
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly Profile _profile;
    private readonly ModelBuilder _modelBuilder;
    private readonly ModelSolver _modelSolver;

    /// <summary>
    /// Creates a new map matcher.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <param name="settings">The settings.</param>
    /// <exception cref="Exception"></exception>
    public MapMatcher(RoutingNetwork routingNetwork, MapMatcherSettings settings)
    {
        this.Settings = settings;
        _routingNetwork = routingNetwork;
        _profile = settings.Profile ?? throw new Exception("No profile set"); ;

        _modelBuilder = new ModelBuilder(routingNetwork, settings.ModelBuilderSettings);
        _modelSolver = new ModelSolver();
    }

    /// <summary>
    /// The settings.
    /// </summary>
    public MapMatcherSettings Settings { get; }

    /// <summary>
    /// Gets the routing network.
    /// </summary>
    public RoutingNetwork RoutingNetwork => _routingNetwork;

    /// <summary>
    /// Matches the given track.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One or more matched segments.</returns>
    /// <exception cref="Exception"></exception>
    public async Task<IEnumerable<MapMatch>> MatchAsync(Track track, CancellationToken cancellationToken = default)
    {
        var matches = new List<MapMatch>();

        // build track model.
        var trackModels = await _modelBuilder.BuildModels(track, _profile, cancellationToken);

        foreach (var trackModel in trackModels)
        {
            // run solver.
            var bestMatch = _modelSolver.Solve(trackModel).ToList();
            if (cancellationToken.IsCancellationRequested) return ArraySegment<MapMatch>.Empty;

            // calculate the paths between each matched point pair.
            var rawPaths = new List<Path>();
            for (var l = 2; l < bestMatch.Count - 1; l++)
            {
                if (cancellationToken.IsCancellationRequested) return ArraySegment<MapMatch>.Empty;

                var sourceNodeId = bestMatch[l - 1];
                var targetNodeId = bestMatch[l];
                var sourceNode = trackModel.GetNode(sourceNodeId);
                var targetNode = trackModel.GetNode(targetNodeId);
                var source = sourceNode.SnapPoint;
                var target = targetNode.SnapPoint;

                if (source == null) throw new Exception("Track point should have a snap point");
                if (target == null) throw new Exception("Track point should have a snap point");

                // try to use the cached path from model building.
                var edge = trackModel.GetEdge(sourceNodeId, targetNodeId);
                if (edge?.CachedPath != null)
                {
                    rawPaths.Add(edge.CachedPath);
                }
                else
                {
                    // fallback: re-route if no cached path (e.g. same snap point).
                    var path = await _routingNetwork.Route(new RoutingSettings() { Profile = _profile })
                        .From(source.Value).To(target.Value).PathAsync(CancellationToken.None);
                    if (path.IsError)
                        throw new Exception(
                            $"Raw path calculation failed, it shouldn't fail at this point because it succeeded on the same path before: {path.ErrorMessage}");
                    rawPaths.Add(path);
                }
            }

            matches.Add(new MapMatch(track, _profile, rawPaths));
        }

        return matches;
    }
}
