using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.Data;

/// <summary>
/// A manager to manage mapping or vertex and edge ids and turn restrictions.
/// </summary>
/// <remarks>
/// Safe for concurrent tile insertion. Every structure here is bookkeeping that spans tiles —
/// the tile data itself lives in the tiles — so this is what several inserts running at once
/// contend on, and each piece needs a different guarantee:
///
/// <list type="bullet">
/// <item>the id sets only ever gain independent entries, so a concurrent dictionary is enough;</item>
/// <item>a boundary crossing has to be claimed <b>atomically</b> — two tiles must not both
/// believe they matched the same crossing and each create the edge;</item>
/// <item>pending restrictions are scanned and removed as a batch, which a concurrent collection
/// cannot express, so they keep a lock and it is held inside this class rather than left to
/// callers to remember.</item>
/// </list>
/// </remarks>
public class GlobalNetworkManager
{
    /// <summary>
    /// Creates a new global network manager.
    /// </summary>
    public GlobalNetworkManager()
    {
        this.VertexIdSet = new GlobalVertexIdSet();
        this.EdgeIdSet = new GlobalEdgeIdSet();
    }

    /// <summary>
    /// The global vertex id set.
    /// </summary>
    public GlobalVertexIdSet VertexIdSet { get; }

    /// <summary>
    /// The global edge id set.
    /// </summary>
    public GlobalEdgeIdSet EdgeIdSet { get; }

    /// <summary>
    /// Pending boundary crossings keyed by GlobalEdgeId.
    /// When a crossing's matching tile isn't loaded yet, store the local vertex and metadata here.
    /// When the other tile loads and has a crossing with the same GlobalEdgeId, it creates the edge.
    /// </summary>
    private readonly ConcurrentDictionary<GlobalEdgeId, (VertexId vertex,
        IEnumerable<(string key, string value)> attributes, uint edgeTypeId, bool isIncoming)>
        _pendingBoundaryCrossings = new();

    /// <summary>
    /// Claims the pending half of a boundary crossing, or records this half and waits for the other.
    /// </summary>
    /// <param name="globalEdgeId">The crossing's global edge id.</param>
    /// <param name="half">This tile's half of the crossing.</param>
    /// <param name="other">The other half, when this call claimed it.</param>
    /// <returns>True when the pair is complete and the caller should create the edge.</returns>
    /// <remarks>
    /// The claim is what makes this safe: exactly one of the two tiles gets <c>true</c>, because
    /// <c>TryRemove</c> hands the stored half to a single caller. A read-then-remove would let
    /// both tiles see the other's half and both create the edge — which is the kind of race that
    /// produces a duplicated boundary edge rather than an exception.
    ///
    /// A failed <c>TryAdd</c> must retry the claim rather than give up, and that is not a
    /// refinement — it is the difference between working and silently losing edges. Both halves
    /// can arrive at once: each finds nothing to claim, then one wins the add and the other's
    /// <c>TryAdd</c> fails. Returning false there drops the losing half entirely and leaves the
    /// winner's half waiting for a partner that has already been and gone, so the boundary edge
    /// is never created. Looping turns that lost race into a claim, because the other half is
    /// necessarily present the moment the add fails.
    ///
    /// The loop terminates: a crossing has exactly two halves, so at most one add can be lost.
    /// </remarks>
    public bool TryClaimBoundaryCrossing(GlobalEdgeId globalEdgeId,
        (VertexId vertex, IEnumerable<(string key, string value)> attributes, uint edgeTypeId,
            bool isIncoming) half,
        out (VertexId vertex, IEnumerable<(string key, string value)> attributes, uint edgeTypeId,
            bool isIncoming) other)
    {
        while (true)
        {
            if (_pendingBoundaryCrossings.TryRemove(globalEdgeId, out other)) return true;
            if (_pendingBoundaryCrossings.TryAdd(globalEdgeId, half)) return false;
        }
    }

    /// <summary>
    /// How many boundary crossings are still waiting for their other half.
    /// </summary>
    public int PendingBoundaryCrossingCount => _pendingBoundaryCrossings.Count;

    /// <summary>
    /// Restrictions that couldn't be resolved yet because not all edges are available.
    /// </summary>
    private readonly List<GlobalRestriction> _pendingRestrictions = new();

    private readonly object _pendingRestrictionsLock = new();

    /// <summary>
    /// <see cref="_pendingRestrictions"/>.Count, readable without the lock.
    /// </summary>
    /// <remarks>
    /// Only ever read to answer "is there anything at all to do", and a stale zero is harmless:
    /// whoever adds a restriction publishes the new count after adding it, and the retry that
    /// misses it runs on the next insert instead. Restrictions are resolved eventually, not
    /// immediately, which is already how the deferred queue works.
    /// </remarks>
    private int _pendingRestrictionCount;

    /// <summary>
    /// Pending restrictions indexed by the ways they name.
    /// </summary>
    /// <remarks>
    /// Without this, every insert retried every pending restriction. Measured over one cold block:
    /// 7,157 retry passes examined 535,147 restrictions and resolved 2,335 — a 0.44% hit rate, with
    /// ~75 permanently unresolvable restrictions re-walked by every later insert. Cost quadratic in
    /// tiles, and it made restriction resolution ~39% of tile insertion.
    ///
    /// Keyed on the <b>way</b> (<see cref="GlobalEdgeId.EdgeId"/>), not the full edge id, because
    /// the resolver does not require the exact id a restriction names: it falls back to
    /// <c>WalkFromAnchor</c>, which tries every sub-span <c>(EdgeId, anchor, other)</c> of the same
    /// way. So the id that arrives is usually a *piece* of the way the restriction asked for, and
    /// indexing by exact id parks restrictions under keys that never arrive. Two turn-restriction
    /// tests caught exactly that. Inversion needs no special handling here — it swaps tail and head
    /// and leaves the way alone.
    ///
    /// Every way a restriction names is indexed, not just one, so it is retried whenever any of
    /// them gains an edge. A resolved restriction leaves its other entries behind; they are
    /// filtered by <see cref="_pending"/> on the way out and cleared by the next full sweep.
    /// </remarks>
    private readonly Dictionary<long, List<GlobalRestriction>> _pendingByWay = new();

    /// <summary>
    /// Which restrictions are still pending, so a stale index entry can be recognised.
    /// </summary>
    private readonly HashSet<GlobalRestriction> _pending = new();

    /// <summary>
    /// Records a restriction whose edges are not all loaded yet.
    /// </summary>
    public void AddPendingRestriction(GlobalRestriction restriction)
    {
        lock (_pendingRestrictionsLock)
        {
            _pendingRestrictions.Add(restriction);
            this.ParkNoLock(restriction);
            Volatile.Write(ref _pendingRestrictionCount, _pendingRestrictions.Count);
        }
    }

    /// <summary>
    /// How long a parked restriction waits, measured in inserts.
    /// </summary>
    /// <remarks>
    /// The question this answers: is parking a property of the data, or an artifact of the order
    /// tiles happen to be inserted in? A restriction at a tile boundary can only resolve once the
    /// boundary edge exists, and that edge is created by whichever of the two tiles is inserted
    /// second — so the restriction in the first one parks even though its partner arrives
    /// milliseconds later in the same batch of nine.
    ///
    /// If the waits cluster under ~9 inserts, parking is intra-batch ordering and the fix is to
    /// resolve after the batch rather than per tile. If they are long, the edges genuinely live
    /// outside the batch and the retry has to stay.
    /// </remarks>
    private readonly Dictionary<GlobalRestriction, int> _parkedAt = new();

    private int _insertSeq;

    private long _waitSum;

    private int _waitCount;

    private int _waitMax;

    private int _waitWithinBatch;

    /// Parked-restriction waits: how many inserts passed before each resolved.
    public (int count, double meanInserts, int maxInserts, int withinBatch) ParkWaits
    {
        get
        {
            lock (_pendingRestrictionsLock)
            {
                return (_waitCount, _waitCount == 0 ? 0 : _waitSum / (double)_waitCount,
                    _waitMax, _waitWithinBatch);
            }
        }
    }

    /// Records how long a restriction waited. Caller holds the lock.
    private void RecordWaitNoLock(GlobalRestriction restriction)
    {
        if (!_parkedAt.Remove(restriction, out var parkedAt)) return;

        var waited = _insertSeq - parkedAt;
        _waitSum += waited;
        _waitCount++;
        if (waited > _waitMax) _waitMax = waited;

        // Nine is the batch: VertexTouched loads a tile plus its eight neighbours together.
        if (waited <= 9) _waitWithinBatch++;
    }

    /// Indexes a restriction under every way it names. Caller holds the lock.
    private void ParkNoLock(GlobalRestriction restriction)
    {
        _pending.Add(restriction);
        if (!_parkedAt.ContainsKey(restriction)) _parkedAt[restriction] = _insertSeq;
        foreach (var edge in restriction)
        {
            if (!_pendingByWay.TryGetValue(edge.EdgeId, out var waiting))
            {
                waiting = new List<GlobalRestriction>(1);
                _pendingByWay[edge.EdgeId] = waiting;
            }

            // A restriction naming the same way twice (a U-turn) would otherwise be retried
            // twice for one arrival.
            if (!waiting.Contains(restriction)) waiting.Add(restriction);
        }
    }

    /// <summary>
    /// Inserts between full sweeps of the pending list.
    /// </summary>
    /// <remarks>
    /// The index above is the mechanism; this is the safety net for the one case it cannot see — a
    /// restriction parked under an edge that is already present, so no arrival will ever probe its
    /// key. Rare (it needs the race in <see cref="FirstMissingEdge"/>), but a restriction stuck
    /// forever is a missing turn restriction, which is a correctness bug, not a slow one.
    ///
    /// It is also what bounds how much later a restriction can resolve than it used to. Measured
    /// at 512, one cold block resolved 2,271 pending restrictions against 2,335 before — 64 were
    /// still waiting when the block ended. A server keeps inserting and would resolve them; a
    /// finite run does not, and a turn restriction that is briefly absent lets a route take a
    /// forbidden turn. At 64 the residue window is 8x smaller and the sweep still costs ~1/64 of
    /// the unconditional pass.
    /// </remarks>
    private const int SweepInterval = 64;

    private int _insertsSinceSweep;

    /// <summary>
    /// Retries the restrictions waiting for any of the edges that just became available.
    /// </summary>
    /// <param name="arrived">Global edge ids registered by the insert that just ran.</param>
    /// <param name="tryResolve">Attempts one restriction; true when it resolved.</param>
    public void RetryPendingRestrictionsFor(IReadOnlyList<GlobalEdgeId> arrived,
        Func<GlobalRestriction, bool> tryResolve)
    {
        // Same lock-free fast path as the full sweep: nothing pending, nothing to do, and this
        // runs on every insert.
        if (Volatile.Read(ref _pendingRestrictionCount) == 0)
        {
            // Still count the insert, so a sweep is not skipped forever by empty fast paths and so
            // the wait measurement is against a clock that keeps moving.
            Interlocked.Increment(ref _insertsSinceSweep);
            Interlocked.Increment(ref _insertSeq);
            return;
        }

        Interlocked.Increment(ref _insertSeq);
        var sweep = Interlocked.Increment(ref _insertsSinceSweep) >= SweepInterval;
        if (sweep)
        {
            Volatile.Write(ref _insertsSinceSweep, 0);
            this.RetryPendingRestrictions(tryResolve);
            return;
        }

        var examined = 0;
        var resolved = 0;
        lock (_pendingRestrictionsLock)
        {
            if (_pendingByWay.Count == 0) return;

            // Distinct ways only: a tile contributes hundreds of edges but they belong to far
            // fewer ways, and one lookup per way is the point of the index.
            var ways = new HashSet<long>();
            foreach (var edge in arrived) ways.Add(edge.EdgeId);

            foreach (var way in ways)
            {
                if (!_pendingByWay.TryGetValue(way, out var waiting)) continue;

                // Snapshot: tryResolve can re-park a restriction, which appends to this list.
                for (var i = waiting.Count - 1; i >= 0; i--)
                {
                    var restriction = waiting[i];

                    // Resolved through one of its other ways already.
                    if (!_pending.Contains(restriction))
                    {
                        waiting.RemoveAt(i);
                        continue;
                    }

                    examined++;
                    if (!tryResolve(restriction)) continue;

                    resolved++;
                    this.RecordWaitNoLock(restriction);
                    _pending.Remove(restriction);
                    _pendingRestrictions.Remove(restriction);
                    waiting.RemoveAt(i);
                }

                if (waiting.Count == 0) _pendingByWay.Remove(way);
            }

            Volatile.Write(ref _pendingRestrictionCount, _pendingRestrictions.Count);
        }

        Network.Tiles.Standalone.Writer.TileInsertCounters.CountRetryPass(examined, resolved);
    }

    /// <summary>
    /// Retries every pending restriction, dropping the ones that now resolve.
    /// </summary>
    /// <param name="tryResolve">Attempts one restriction; true when it resolved.</param>
    /// <remarks>
    /// The lock spans the whole pass because the operation is a scan-and-remove over the list,
    /// which no concurrent collection expresses: an index-based removal is only meaningful while
    /// the list cannot shift underneath it.
    ///
    /// Note what this means for the caller — <paramref name="tryResolve"/> runs while the lock is
    /// held, and it writes turn costs to arbitrary tiles. So restriction resolution serialises
    /// against itself across all concurrent inserts. That is deliberate: restrictions name edges
    /// in tiles that cannot be predicted from the tile being inserted, so they cannot be
    /// partitioned by tile the way edges can. They are also rare compared to edges.
    /// </remarks>
    public void RetryPendingRestrictions(Func<GlobalRestriction, bool> tryResolve)
    {
        // Lock-free when there is nothing to retry, which is the overwhelming majority of
        // inserts. This pass runs on EVERY tile insert, so taking the lock unconditionally made
        // it a global serialisation point for all concurrent inserts — measured at 29.6% of all
        // blocked time in cold, plus another 8.7% on the add side, for a list that is almost
        // always empty.
        if (Volatile.Read(ref _pendingRestrictionCount) == 0) return;

        lock (_pendingRestrictionsLock)
        {
            var examined = _pendingRestrictions.Count;
            var resolved = 0;
            for (var i = _pendingRestrictions.Count - 1; i >= 0; i--)
            {
                if (tryResolve(_pendingRestrictions[i]))
                {
                    this.RecordWaitNoLock(_pendingRestrictions[i]);
                    _pendingRestrictions.RemoveAt(i);
                    resolved++;
                }
            }

            // Rebuild the index from the survivors. The sweep resolved restrictions without
            // going through their parked keys, so every entry in there is now suspect: a
            // resolved restriction would linger, and a survivor may be waiting on a different
            // edge than when it was parked.
            _pendingByWay.Clear();
            _pending.Clear();
            foreach (var restriction in _pendingRestrictions)
            {
                this.ParkNoLock(restriction);
            }

            Volatile.Write(ref _pendingRestrictionCount, _pendingRestrictions.Count);
            Network.Tiles.Standalone.Writer.TileInsertCounters.CountRetryPass(examined, resolved);
        }
    }

    /// <summary>
    /// How many restrictions are still unresolved.
    /// </summary>
    public int PendingRestrictionCount
    {
        get
        {
            lock (_pendingRestrictionsLock)
            {
                return _pendingRestrictions.Count;
            }
        }
    }
}
