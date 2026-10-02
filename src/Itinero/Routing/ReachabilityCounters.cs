using System;
using System.Globalization;
using System.Threading;

namespace Itinero.Routing;

/// <summary>
/// Process-wide tallies for <see cref="ReachabilityRouter"/>, so the cost of the optimistic
/// path can be attributed rather than guessed at.
/// </summary>
/// <remarks>
/// Free-running, read by subtracting two snapshots; nothing resets them, which keeps the write
/// path to one uncontended increment.
/// </remarks>
public static class ReachabilityCounters
{
    private static long _routes;
    private static long _originAdvances;
    private static long _destinationAdvances;
    private static long _searches;
    private static long _settledStates;



    // Split by whether the route ever abandoned a candidate: the retried group's share of total
    // settled states is the cost of retrying.
    private static long _routesRetried;
    private static long _settledClean;
    private static long _settledRetried;
    private static long _wastedStates;

    /// <summary>Route requests entered.</summary>
    public static long Routes => Interlocked.Read(ref _routes);

    private static long _pocketsComposed;
    private static long _pocketsAbandoned;

    /// <summary>
    /// Times a stranded candidate got a pocket composed for it — the local-access fallback.
    /// </summary>
    /// <remarks>
    /// Per route this is how often phase 1 was not enough. Nothing is composed for a candidate
    /// that routes straight away, so the common path never pays for it.
    /// </remarks>
    public static long PocketsComposed => Interlocked.Read(ref _pocketsComposed);

    public static void CountPocketComposed() => Interlocked.Increment(ref _pocketsComposed);

    /// <summary>
    /// Pockets that outgrew their budget, so the candidate got no relabelling.
    /// </summary>
    /// <remarks>
    /// Should stay at zero: a local-access region larger than the island threshold means the
    /// tagging or the bound is wrong, not that the route is unusual.
    /// </remarks>
    public static long PocketsAbandoned => Interlocked.Read(ref _pocketsAbandoned);

    public static void CountPocketAbandoned() => Interlocked.Increment(ref _pocketsAbandoned);

    /// <summary>Times the origin gave up on a candidate and took the next one.</summary>
    /// <remarks>
    /// Counts only candidates abandoned after a failed search. The first snap is not an advance
    /// — so zero here means every route was served by the edge nearest its origin.
    /// </remarks>
    public static long OriginAdvances => Interlocked.Read(ref _originAdvances);

    /// <summary>Times the destination gave up on a candidate and took the next one.</summary>
    public static long DestinationAdvances => Interlocked.Read(ref _destinationAdvances);

    /// <summary>Bidirectional searches run, including continuations of a surviving half.</summary>
    public static long Searches => Interlocked.Read(ref _searches);

    /// <summary>States settled across both halves, summed over all searches.</summary>
    /// <remarks>
    /// The per-route figure is what compares against the baseline's A* search: a router paying
    /// for goal direction settles far fewer states for the same path, and that difference is
    /// CPU rather than I/O, so it shows up most in the hot regime.
    /// </remarks>
    public static long SettledStates => Interlocked.Read(ref _settledStates);

    /// <summary>Routes that abandoned at least one candidate.</summary>
    public static long RoutesRetried => Interlocked.Read(ref _routesRetried);

    /// <summary>Settled states charged to routes that never retried.</summary>
    public static long SettledClean => Interlocked.Read(ref _settledClean);

    /// <summary>Settled states charged to routes that retried at least once.</summary>
    public static long SettledRetried => Interlocked.Read(ref _settledRetried);

    /// <summary>
    /// States settled by halves that were then thrown away — the work a retry actually wastes.
    /// </summary>
    /// <remarks>
    /// Bounded by construction: a half is abandoned only when its heap empties below threshold,
    /// so it had already exhausted a component smaller than that.
    /// </remarks>
    public static long WastedStates => Interlocked.Read(ref _wastedStates);

    internal static void CountRoute() => Interlocked.Increment(ref _routes);

    /// Called once per route, at exit, with what that route cost.
    internal static void CountRouteOutcome(bool retried, long settled)
    {
        if (retried)
        {
            Interlocked.Increment(ref _routesRetried);
            Interlocked.Add(ref _settledRetried, settled);
        }
        else
        {
            Interlocked.Add(ref _settledClean, settled);
        }
    }

    internal static void CountWasted(long states) => Interlocked.Add(ref _wastedStates, states);




    internal static void CountOriginAdvance() => Interlocked.Increment(ref _originAdvances);

    internal static void CountDestinationAdvance() => Interlocked.Increment(ref _destinationAdvances);

    internal static void CountSearch() => Interlocked.Increment(ref _searches);

    internal static void CountSettled(long states) => Interlocked.Add(ref _settledStates, states);

    /// <summary>A one-line summary, for a log heartbeat.</summary>
    public static string Describe()
    {
        var routes = Routes;
        if (routes == 0) return "reachability: no routes yet";

        string Per(long value, string format) =>
            (value / (double)routes).ToString(format, CultureInfo.InvariantCulture);

        var retried = RoutesRetried;
        var clean = routes - retried;

        string Mean(long total, long count, string format) => count == 0
            ? "n/a"
            : (total / (double)count).ToString(format, CultureInfo.InvariantCulture);

        string Pct(long part, long whole) => whole == 0
            ? "n/a"
            : (part * 100.0 / whole).ToString("0.0", CultureInfo.InvariantCulture) + "%";

        return "reachability: routes=" + routes.ToString(CultureInfo.InvariantCulture) +
               ", searches/route=" + Per(Searches, "0.000") +
               ", origin_advances/route=" + Per(OriginAdvances, "0.000") +
               ", dest_advances/route=" + Per(DestinationAdvances, "0.000") +
               ", settled/route=" + Per(SettledStates, "0.0") +
               " | retried=" + Pct(retried, routes) +
               " of routes, settled/route clean=" + Mean(SettledClean, clean, "0.0") +
               " retried=" + Mean(SettledRetried, retried, "0.0") +
               ", wasted_states=" + Pct(WastedStates, SettledStates) + " of all settled" +
               " (" + Mean(WastedStates, routes, "0.0") + "/route)" +
               " | pockets/route=" + Per(PocketsComposed, "0.000") +
               ", pockets_abandoned=" + PocketsAbandoned.ToString(CultureInfo.InvariantCulture);
    }
}
