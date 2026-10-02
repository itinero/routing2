using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Edges;
using Itinero.Network.Search.Reachability;
using Itinero.Network.Tiles;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;

namespace Itinero.Routing;

/// <summary>
/// Routes between two locations, doing the snapping and the routing as one operation.
/// </summary>
/// <remarks>
/// Each candidate is tried by expanding a <see cref="SearchHalf"/> from it, and that same half is
/// what the search continues with, so the connectivity check costs nothing the route would not.
/// </remarks>
/// <remarks>
/// One-to-one only: a half is consumed by the search that adopts it, so sharing an origin across
/// destinations would mean copying it.
/// </remarks>
public static class ReachabilityRouter
{

    /// <summary>
    /// Calculates a path between two locations.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="settings">Routing settings; carries the profile.</param>
    /// <param name="origin">Where the route starts, as longitude/latitude.</param>
    /// <param name="destination">Where it ends.</param>
    /// <param name="bounds">How much network counts as connected, and the work ceiling.</param>
    /// <param name="searchBoxMeters">Half-size of the box searched for candidate edges.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The path, and the two snap points it was built between.</returns>
    public static async Task<Result<ReachabilityRoute>> PathAsync(
        this RoutingNetwork network,
        RoutingSettings settings,
        (double longitude, double latitude) origin,
        (double longitude, double latitude) destination,
        ReachabilityBounds bounds,
        double searchBoxMeters,
        CancellationToken cancellationToken = default)
    {
        var profile = settings.Profile;
        var costFunction = network.GetCostFunctionFor(profile);
        if (settings.CostFunctionWrapper != null) costFunction = settings.CostFunctionWrapper(costFunction);

        var accessChecker = (IEdgeChecker)network.Snap([profile], s =>
        {
            s.CheckIslands = false;
            s.CheckCanStopOn = false;
            s.OffsetInMeter = searchBoxMeters;
            s.OffsetInMeterMax = searchBoxMeters;
            s.MaxDistance = double.MaxValue;
        });

        await network.UsageNotifier.NotifyBox(network, BoxFor(origin, searchBoxMeters), cancellationToken);
        await network.UsageNotifier.NotifyBox(network, BoxFor(destination, searchBoxMeters), cancellationToken);

        await using var originCandidates = network.NearestCandidatesAsync(
            BoxFor(origin, searchBoxMeters), accessChecker, searchBoxMeters, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        await using var destinationCandidates = network.NearestCandidatesAsync(
            BoxFor(destination, searchBoxMeters), accessChecker, searchBoxMeters, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        // Edges no longer worth snapping to, per direction, from the settled set of a half that
        // ran out of network: that set is closed, so every edge in it leads exactly as far.
        var deadForOrigin = new HashSet<EdgeId>();
        var deadForDestination = new HashSet<EdgeId>();

        var sourceSnap = await NextSnapAsync(network, costFunction, originCandidates, deadForOrigin);
        if (sourceSnap == null)
        {
            return new Result<ReachabilityRoute>(FormattableString.Invariant(
                $"Could not snap origin to connected network: {origin.longitude},{origin.latitude}"));
        }

        var targetSnap = await NextSnapAsync(network, costFunction, destinationCandidates, deadForDestination);
        if (targetSnap == null)
        {
            return new Result<ReachabilityRoute>(FormattableString.Invariant(
                $"Could not snap destination to connected network: {destination.longitude},{destination.latitude}"));
        }

        SearchHalf? forwardHalf = null;
        SearchHalf? backwardHalf = null;

        // A stranded candidate gets a second attempt with its pocket relabelled before we give up
        // on it and move further away. One attempt per candidate; null means no pocket yet.
        ICostFunction? forwardCostFn = null;
        ICostFunction? backwardCostFn = null;
        var forwardPocketTried = false;
        var backwardPocketTried = false;
        var pocketBound = network.IslandManager.MaxIslandSize;

        var maxBox = settings.MaxBoxFor(network, [sourceSnap.Value, targetSnap.Value]);

        // Goal direction, the same balanced potential the island-detection router uses. It depends
        // on both snap locations, so a candidate advance invalidates it and it is rebuilt below.
        var potential = IRouterOneToOneExtensions.BuildBalancedPotential(
            network, profile, sourceSnap.Value, targetSnap.Value);

        ReachabilityCounters.CountRoute();
        var settledBefore = 0L;
        var routeSettled = 0L;
        var retried = false;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A half whose endpoint did not move is kept, settled set and frontier intact, and
                // carries on from where it stopped. Only the end that failed starts over.
                forwardHalf ??= NewHalf(network, forwardCostFn ?? costFunction, sourceSnap.Value, true);
                backwardHalf ??= NewHalf(network, backwardCostFn ?? costFunction, targetSnap.Value, false);

                var path = await RouteAsync(forwardHalf, backwardHalf, sourceSnap.Value, targetSnap.Value,
                    potential);

                // Settled counts are cumulative per half and a surviving half keeps growing across
                // retries, so charge the increment rather than the total.
                var settledNow = (long)forwardHalf.Settled.Count + backwardHalf.Settled.Count;
                var settledDelta = settledNow >= settledBefore ? settledNow - settledBefore : settledNow;
                ReachabilityCounters.CountSettled(settledDelta);
                routeSettled += settledDelta;
                settledBefore = settledNow;

                if (path != null) return new ReachabilityRoute(path, sourceSnap.Value, targetSnap.Value);

                var originStranded = Stranded(forwardHalf, bounds.Threshold);
                var destinationStranded = Stranded(backwardHalf, bounds.Threshold);

                // Both halves got past the threshold and still did not meet: there is plenty of
                // network at both ends and it does not join up. Trying other edges nearby cannot
                // change that.
                if (!originStranded && !destinationStranded)
                {
                    return new Result<ReachabilityRoute>("Path not found");
                }

                if (originStranded && !forwardPocketTried)
                {
                    forwardPocketTried = true;
                    ReachabilityCounters.CountPocketComposed();
                    var pocket = LocalPocket.Compose(network, costFunction,
                        SettledEdges(forwardHalf), asOrigin: true,
                        componentAllowance: pocketBound, budget: pocketBound);

                    if (!pocket.Abandoned)
                    {
                        // Same candidate, second attempt: the pocket's untagged members now read as
                        // local-access, so the half can cross them to reach its way out.
                        forwardCostFn = new PocketCostFunction(costFunction, pocket.Members);
                        forwardHalf = null;
                        retried = true;
                        continue;
                    }

                    ReachabilityCounters.CountPocketAbandoned();
                }

                if (originStranded)
                {
                    foreach (var edge in SettledEdges(forwardHalf)) deadForOrigin.Add(edge);
                    sourceSnap = await NextSnapAsync(network, costFunction, originCandidates, deadForOrigin);
                    if (sourceSnap == null)
                    {
                        return new Result<ReachabilityRoute>(FormattableString.Invariant(
                            $"Could not snap origin to connected network: {origin.longitude},{origin.latitude}"));
                    }

                    ReachabilityCounters.CountOriginAdvance();
                    ReachabilityCounters.CountWasted(forwardHalf.Settled.Count);
                    retried = true;
                    forwardHalf = null;
                    forwardCostFn = null;
                    forwardPocketTried = false;
                }

                if (destinationStranded && !backwardPocketTried)
                {
                    backwardPocketTried = true;
                    ReachabilityCounters.CountPocketComposed();
                    var pocket = LocalPocket.Compose(network, costFunction,
                        SettledEdges(backwardHalf), asOrigin: false,
                        componentAllowance: pocketBound, budget: pocketBound);

                    if (!pocket.Abandoned)
                    {
                        backwardCostFn = new PocketCostFunction(costFunction, pocket.Members);
                        backwardHalf = null;
                        retried = true;
                        continue;
                    }

                    ReachabilityCounters.CountPocketAbandoned();
                }

                if (destinationStranded)
                {
                    foreach (var edge in SettledEdges(backwardHalf)) deadForDestination.Add(edge);
                    targetSnap = await NextSnapAsync(network, costFunction, destinationCandidates, deadForDestination);
                    if (targetSnap == null)
                    {
                        return new Result<ReachabilityRoute>(FormattableString.Invariant(
                            $"Could not snap destination to connected network: {destination.longitude},{destination.latitude}"));
                    }

                    ReachabilityCounters.CountDestinationAdvance();
                    ReachabilityCounters.CountWasted(backwardHalf.Settled.Count);
                    retried = true;
                    backwardHalf = null;
                    backwardCostFn = null;
                    backwardPocketTried = false;
                }

                maxBox = settings.MaxBoxFor(network, [sourceSnap.Value, targetSnap.Value]);

                // An endpoint moved, so the goal direction did too. The surviving half's frontier
                // is keyed on the old one; the search re-keys it from stored costs on the next
                // call, which is why rebuilding here rather than adjusting in place is safe.
                potential = IRouterOneToOneExtensions.BuildBalancedPotential(
                    network, profile, sourceSnap.Value, targetSnap.Value);
            }
        }
        finally
        {
            // Every exit from the loop lands here, including the failures — a route that ran
            // out of candidates is exactly the kind the retry accounting must not omit.
            ReachabilityCounters.CountRouteOutcome(retried, routeSettled);
        }

        async Task<Path?> RouteAsync(SearchHalf fwd, SearchHalf bwd, SnapPoint from, SnapPoint to,
            HeuristicFunc? goalDirection)
        {
            ReachabilityCounters.CountSearch();
            var search = SearchPool<BidirectionalDijkstra>.Rent();
            try
            {
                var (path, _) = await search.ContinueAsync(network, fwd, bwd, from, to, costFunction,
                    SettleAsync, cancellationToken, isMainN: null, potential: goalDirection,
                    localAccessRule: true,
                    forwardCostFunction: forwardCostFn, backwardCostFunction: backwardCostFn);
                return path;
            }
            finally
            {
                SearchPool<BidirectionalDijkstra>.Return(search);
            }
        }

        async Task<bool> SettleAsync(VertexId v)
        {
            if (!network.UsageNotifier.IsVertexDataReady(network, v))
            {
                await network.UsageNotifier.NotifyVertex(network, v, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested) return false;
            if (maxBox == null) return false;

            return !maxBox.Value.Overlaps(network.GetVertex(v));
        }
    }

    /// A half seeded at a snap point.
    private static SearchHalf NewHalf(RoutingNetwork network, ICostFunction costFunction,
        SnapPoint snap, bool isForwardHalf)
    {
        var half = new SearchHalf(isForwardHalf);
        half.PushTerminal(network.GetEdgeEnumerator(), snap, costFunction, potential: null);
        return half;
    }

    /// <summary>
    /// Whether this half ran out of network rather than being stopped by anything else.
    /// </summary>
    /// <remarks>
    /// An empty heap below the threshold means everything reachable has been seen and there is
    /// not enough of it — the same verdict the candidate evaluation would reach.
    /// </remarks>
    private static bool Stranded(SearchHalf half, int threshold) =>
        half.Heap.Count == 0 && half.Settled.Count < threshold;

    /// The box searched for candidate edges around a location.
    private static ((double longitude, double, float? e) topLeft,
        (double longitude, double latitude, float? e) bottomRight) BoxFor(
        (double longitude, double latitude) location, double searchBoxMeters)
    {
        (double longitude, double latitude, float? e) at = (location.longitude, location.latitude, null);
        return at.BoxAround(searchBoxMeters);
    }

    /// <summary>
    /// The next candidate that is not already known to be hopeless.
    /// </summary>
    /// <remarks>
    /// Skips everything the failed search settled: that set is closed and under threshold, so one
    /// failed search dismisses the whole dead end rather than one edge of it.
    /// </remarks>
    private static async Task<SnapPoint?> NextSnapAsync(
        RoutingNetwork network,
        ICostFunction costFunction,
        IAsyncEnumerator<SnapPoint> candidates,
        HashSet<EdgeId> dead)
    {
        var enumerator = network.GetEdgeEnumerator();
        while (await candidates.MoveNextAsync())
        {
            var candidate = candidates.Current;
            if (dead.Contains(candidate.EdgeId)) continue;

            // Only "can the profile use this edge at all". Whether it leads anywhere is settled by
            // the search itself, and only paid for when it turns out not to.
            if (!Routable(enumerator, costFunction, candidate.EdgeId)) continue;

            return candidate;
        }

        return null;
    }

    /// Whether the (masked) cost function can traverse this edge at all.
    private static bool Routable(RoutingNetworkEdgeEnumerator enumerator, ICostFunction routable,
        EdgeId edgeId)
    {
        foreach (var forward in new[] { true, false })
        {
            if (!enumerator.MoveTo(edgeId, forward)) continue;

            var (canAccess, _, _, cost, _) = routable.Get(enumerator, tailToHead: true);
            if (canAccess && cost > 0) return true;
        }

        return false;
    }

    /// The distinct edges a half has settled.
    private static HashSet<EdgeId> SettledEdges(SearchHalf half)
    {
        var edges = new HashSet<EdgeId>();
        foreach (var (edge, _) in half.Settled) edges.Add(edge);
        return edges;
    }


}

/// <summary>
/// A path, together with the snap points chosen for it.
/// </summary>
/// <remarks>
/// The snap points come back with the path because nobody else computed them — snapping happened
/// inside, as part of deciding which candidate edges a route can use.
/// </remarks>
/// <param name="Path">The path found.</param>
/// <param name="Source">Where the origin snapped.</param>
/// <param name="Target">Where the destination snapped.</param>
public readonly record struct ReachabilityRoute(Path Path, SnapPoint Source, SnapPoint Target);
