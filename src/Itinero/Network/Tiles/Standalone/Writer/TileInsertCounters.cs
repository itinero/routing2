using System;
using System.Diagnostics;
using System.Threading;

namespace Itinero.Network.Tiles.Standalone.Writer;

/// <summary>
/// Splits the cost of adding a standalone tile into its phases.
/// </summary>
/// <remarks>
/// Added because insertion turned out to be the dominant cost in the tile path and nothing said
/// which part of it was expensive. Measured in situ: a tile decodes in 0.157ms but inserts in
/// 2.645ms — 17x — and <c>insertWaitSeconds</c> was 0.0033s across 7,169 inserts, so it is not lock
/// contention. That makes it 14.9% of all cold CPU, spent inside this one method, and the four
/// phases have completely different fixes.
///
/// Off unless <c>ITINERO_PERF_PROBE=1</c>: these brackets sit around per-tile work that runs
/// thousands of times per route-block, and a <see cref="Stopwatch.GetTimestamp"/> pair is not free
/// at that rate.
/// </remarks>
public static class TileInsertCounters
{
    /// <summary>
    /// Whether the phase probes are active.
    /// </summary>
    public static readonly bool Enabled =
        Environment.GetEnvironmentVariable("ITINERO_PERF_PROBE") == "1";

    private static long _tiles;
    private static long _installTicks;
    private static long _registerTicks;
    private static long _claimTicks;
    private static long _boundaryTicks;
    private static long _restrictionTicks;
    /// <summary>
    /// Time inside <c>EdgeIdSet.Set</c> alone, to split the register phase.
    /// </summary>
    /// <remarks>
    /// That phase costs 3.4µs per edge, and it does exactly two things: walk the tile's edges with
    /// the enumerator, and write one dictionary entry each. 3.4µs is far too much for either, so
    /// which one it is decides the fix — a cheaper walk over the raw edge data, or a different map.
    /// Two timestamps per edge distort this measurement; that is acceptable to attribute it once.
    /// </remarks>
    private static long _registerSetTicks;

    private static long _internalEdges;
    private static long _crossings;

    /// <summary>
    /// Retry passes over the pending-restriction list, and restrictions examined by them.
    /// </summary>
    /// <remarks>
    /// This pass runs on every insert and walks the whole list, so a restriction that cannot
    /// resolve yet is re-examined by every later insert — cost quadratic in tiles. Counted
    /// separately from the phase timing because the phase total cannot distinguish "restrictions
    /// in this tile are expensive" from "this tile paid for every earlier tile's leftovers".
    ///
    /// Always on: two interlocked adds per insert, no timestamps.
    /// </remarks>
    /// <summary>
    /// Restrictions a tile brought with it, and how many could not resolve immediately.
    /// </summary>
    /// <remarks>
    /// Restrictions are supposed to be rare, which would make this phase negligible — it is 28% of
    /// an insert instead. Either they are not rare, or each one is expensive. Counting them is what
    /// tells the two apart, and the second would point at the resolver: GetEdge falls back to
    /// WalkFromAnchor, which steps over node indices doing two map lookups per step, so one
    /// restriction on a long way can cost dozens of lookups.
    /// </remarks>
    /// <summary>
    /// Resolve attempts split by outcome, because the two say opposite things about what to fix.
    /// </summary>
    /// <remarks>
    /// 18,333 attempts (5,009 first tries plus 13,324 retries) against a 5.18s restriction phase is
    /// ~283us per attempt on average, but a failed attempt should exit early at the first edge it
    /// cannot resolve and cost almost nothing. If failures are cheap then the retry volume is not
    /// the problem and the ~4,900 successes at ~1ms each are; if failures are expensive then the
    /// retry indexing is too coarse. Averaging the two hides which.
    /// </remarks>
    private static long _resolveOkCount;

    private static long _resolveOkTicks;

    private static long _resolveFailCount;

    private static long _resolveFailTicks;

    /// Records one resolve attempt and what it cost.
    public static void CountResolve(bool resolved, long ticks)
    {
        if (!Enabled) return;

        if (resolved)
        {
            Interlocked.Increment(ref _resolveOkCount);
            Interlocked.Add(ref _resolveOkTicks, ticks);
        }
        else
        {
            Interlocked.Increment(ref _resolveFailCount);
            Interlocked.Add(ref _resolveFailTicks, ticks);
        }
    }

    /// String-table lookups and the entries they scanned.
    public static (long lookups, long comparisons) ReadStringScans() =>
        (Interlocked.Read(ref NetworkTile.StringLookups),
            Interlocked.Read(ref NetworkTile.StringComparisons));

    /// Resolve attempts: counts and mean milliseconds, by outcome.
    public static (long okCount, double okMs, long failCount, double failMs) ReadResolves()
    {
        var ok = Interlocked.Read(ref _resolveOkCount);
        var fail = Interlocked.Read(ref _resolveFailCount);
        double Ms(long ticks, long n) => n == 0 ? 0 : ticks / (double)Stopwatch.Frequency / n * 1000;
        return (ok, Ms(Interlocked.Read(ref _resolveOkTicks), ok),
            fail, Ms(Interlocked.Read(ref _resolveFailTicks), fail));
    }

    private static long _restrictionsSeen;

    private static long _restrictionsParked;

    /// Records the restrictions carried by one tile.
    public static void CountRestrictions(int seen, int parked)
    {
        Interlocked.Add(ref _restrictionsSeen, seen);
        Interlocked.Add(ref _restrictionsParked, parked);
    }

    /// Restrictions seen and parked.
    public static (long seen, long parked) ReadRestrictions() =>
        (Interlocked.Read(ref _restrictionsSeen), Interlocked.Read(ref _restrictionsParked));

    private static long _retryPasses;

    private static long _retriesExamined;

    private static long _retriesResolved;

    /// Records a retry pass that actually took the lock.
    public static void CountRetryPass(int examined, int resolved)
    {
        Interlocked.Increment(ref _retryPasses);
        Interlocked.Add(ref _retriesExamined, examined);
        Interlocked.Add(ref _retriesResolved, resolved);
    }

    /// Retry passes, restrictions examined by them, and how many that resolved.
    public static (long passes, long examined, long resolved) ReadRetries() =>
        (Interlocked.Read(ref _retryPasses), Interlocked.Read(ref _retriesExamined),
            Interlocked.Read(ref _retriesResolved));

    /// A timestamp, or 0 when the probe is off.
    public static long Now() => Enabled ? Stopwatch.GetTimestamp() : 0L;

    /// Adds time spent writing global edge ids, measured inside the register phase.
    public static void CountRegisterSet(long ticks) => Interlocked.Add(ref _registerSetTicks, ticks);

    /// Records one completed insert, given the timestamps between its phases.
    public static void Count(long start, long installed, long registered, long claimed,
        long boundariesWritten, long done, int internalEdges, int crossings)
    {
        if (!Enabled) return;

        Interlocked.Increment(ref _tiles);
        Interlocked.Add(ref _installTicks, installed - start);
        Interlocked.Add(ref _registerTicks, registered - installed);
        Interlocked.Add(ref _claimTicks, claimed - registered);
        Interlocked.Add(ref _boundaryTicks, boundariesWritten - claimed);
        Interlocked.Add(ref _restrictionTicks, done - boundariesWritten);
        Interlocked.Add(ref _internalEdges, internalEdges);
        Interlocked.Add(ref _crossings, crossings);
    }

    /// Mean milliseconds per inserted tile, by phase.
    public static (long tiles, double installMs, double registerMs, double registerSetMs,
        double claimMs, double boundaryMs, double restrictionMs, double edgesPerTile,
        double crossingsPerTile) Read()
    {
        var tiles = Interlocked.Read(ref _tiles);
        double Ms(long ticks) => tiles == 0 ? 0 : ticks / (double)Stopwatch.Frequency / tiles * 1000;
        double Per(long n) => tiles == 0 ? 0 : n / (double)tiles;
        return (tiles, Ms(Interlocked.Read(ref _installTicks)), Ms(Interlocked.Read(ref _registerTicks)),
            Ms(Interlocked.Read(ref _registerSetTicks)),
            Ms(Interlocked.Read(ref _claimTicks)), Ms(Interlocked.Read(ref _boundaryTicks)),
            Ms(Interlocked.Read(ref _restrictionTicks)), Per(Interlocked.Read(ref _internalEdges)),
            Per(Interlocked.Read(ref _crossings)));
    }
}
