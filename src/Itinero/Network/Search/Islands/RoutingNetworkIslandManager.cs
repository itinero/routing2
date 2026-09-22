using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Profiles;

namespace Itinero.Network.Search.Islands;

internal class RoutingNetworkIslandManager
{
    // Concurrent: every snap enters BuildForTileAsync, and a ReaderWriterLockSlim
    // admits only one upgradeable reader at a time, so guarding this dictionary
    // with one serialised every snapping thread against every other — including
    // threads asking about entirely different tiles.
    private readonly ConcurrentDictionary<(string profile, uint tile), Lazy<Task>> _tilesInProgress = new();

    // Looked up once per edge relaxation (IsMainN); concurrent so that lookup
    // takes no lock.
    private readonly ConcurrentDictionary<string, Islands> _islands;

    private readonly Dictionary<(string profile, IslandKind kind), IslandDirectedGraph> _directedGraphs = new();
    private readonly ReaderWriterLockSlim _directedGraphsLock = new();

    // Per-profile semaphore that serialises IslandClassifier.BuildForTileAsync
    // calls against each other for the same profile. The shared Full+NonLocal
    // dgs are reset to their initial state at the end of each call (per spec);
    // running two classifications for the same profile concurrently would let
    // one wipe the other's working state mid-flight. Different profiles still
    // run in parallel — each has its own dg pair and its own semaphore.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _buildSerialisers = new();

    internal SemaphoreSlim GetBuildSerialiser(string profileName) =>
        _buildSerialisers.GetOrAdd(profileName, _ => new SemaphoreSlim(1, 1));

    internal RoutingNetworkIslandManager(int maxIslandSize)
    {
        this.MaxIslandSize = maxIslandSize;
        _islands = new ConcurrentDictionary<string, Islands>();
    }

    private RoutingNetworkIslandManager(int maxIslandSize, ConcurrentDictionary<string, Islands> islands)
    {
        this.MaxIslandSize = maxIslandSize;
        _islands = islands;
    }

    /// <summary>
    /// Checks if an edge is on an island using the directed graph.
    /// Returns true if island, false if not island, null if inconclusive.
    /// </summary>
    internal bool? IsEdgeOnIsland(Profile profile, EdgeId edgeId) =>
        this.IsEdgeOnIsland(profile.Name, edgeId);

    internal bool? IsEdgeOnIsland(string profileName, EdgeId edgeId)
    {
        // Snapping is a Full-classification concern, so the existing
        // single-DG semantics route through the Full DG.
        IslandDirectedGraph? dg;
        try
        {
            _directedGraphsLock.EnterReadLock();

            if (!_directedGraphs.TryGetValue((profileName, IslandKind.Full), out dg))
                return null;
        }
        finally
        {
            _directedGraphsLock.ExitReadLock();
        }

        // The lock covers the dictionary lookup only; dg's own reads and
        // _islands need no lock.
        if (!_islands.TryGetValue(profileName, out var profileIslands))
            return null;
        if (profileIslands.IsEdgeOnIsland(edgeId))
            return true;

        if (dg.IsNotIsland(edgeId))
            return false;

        return null;
    }

    internal IslandDirectedGraph GetOrCreateDirectedGraph(Profile profile, IslandKind kind = IslandKind.Full)
    {
        var key = (profile.Name, kind);
        try
        {
            _directedGraphsLock.EnterUpgradeableReadLock();

            if (_directedGraphs.TryGetValue(key, out var dg)) return dg;

            try
            {
                _directedGraphsLock.EnterWriteLock();

                dg = new IslandDirectedGraph();
                _directedGraphs[key] = dg;
                return dg;
            }
            finally
            {
                _directedGraphsLock.ExitWriteLock();
            }
        }
        finally
        {
            _directedGraphsLock.ExitUpgradeableReadLock();
        }
    }

    internal int MaxIslandSize { get; }

    internal bool TryGetIslandsFor(string profileName, out Islands islands)
    {
        return _islands.TryGetValue(profileName, out islands);
    }

    internal Islands GetIslandsFor(Profile profile)
    {
        // The factory can run more than once under a race, but only one instance
        // is published and every caller gets that one.
        return _islands.GetOrAdd(profile.Name, _ => new Islands());
    }

    /// <summary>
    /// Returns whether the edge is in the profile's main-N component — the
    /// dominant SCC of the N-only subgraph, i.e. the "mainland" without
    /// L-edges.
    ///
    /// <list type="bullet">
    /// <item><c>true</c>: edge is in main-N. Default for any edge in a done tile
    /// that is neither L-tagged, on an island, nor a non-main-N pocket member.</item>
    /// <item><c>false</c>: edge is not in main-N. Either L-tagged (passed in via
    /// <paramref name="isLocalAccess"/>), on an island (unreachable in Full), or
    /// recorded as a local edge (non-main-N pocket).</item>
    /// <item><c>null</c>: classification has not yet produced a verdict for this
    /// tile.</item>
    /// </list>
    ///
    /// The L-tag check is tag-driven and resolved by the caller (typically via
    /// the cost function's <c>localAccess</c> field on the result of <c>Get</c>),
    /// then passed in. The manager itself does not consult any tag storage.
    /// </summary>
    internal bool? IsMainN(Profile profile, EdgeId edgeId, bool isLocalAccess)
    {
        // L-tagged edge — never main-N, no storage lookup needed.
        if (isLocalAccess) return false;

        if (!_islands.TryGetValue(profile.Name, out var islands)) return null;

        // Read the tile flag before the edge sets: the classifier marks a tile
        // done only after writing its island and local edges, so a tile seen as
        // done guarantees the edge reads below see those writes. Sampling the
        // edge sets first would let a not-yet-written island edge pair with a
        // tile marked done since, reporting main-N for an edge on an island.
        var tileDone = islands.GetTileDone(edgeId.TileId);

        // Unreachable in the Full classification → not in main-N.
        if (islands.IsEdgeOnIsland(edgeId)) return false;

        // Non-main-N pocket → not in main-N.
        if (islands.IsEdgeLocal(edgeId)) return false;

        // Tile finished classifying and the edge is in neither set → main-N.
        // Otherwise we don't yet know.
        return tileDone ? true : null;
    }

    internal async Task BuildForTileAsync(RoutingNetwork network, Profile profile, uint tileId,
        IslandDirectedGraph dgFull, IslandDirectedGraph dgNonLocal,
        CancellationToken cancellationToken)
    {
        // Already classified: nothing to queue and nothing to await. Checked
        // before touching the queue because a classified tile is the common case
        // once a region is warm, and everything below costs more than this does.
        // IslandClassifier.BuildForTileAsync makes the same check first, so this
        // only moves it earlier. GetTileDone is a concurrent-set lookup.
        if (this.GetIslandsFor(profile).GetTileDone(tileId)) return;

        var key = (profile.Name, tileId);

        if (!_tilesInProgress.TryGetValue(key, out var pending))
        {
            // Lazy, not a bare Task: GetOrAdd may invoke a factory more than
            // once under a race, and starting a tile's classification twice
            // would put a second run behind the per-profile serialiser for work
            // already being done. Only the Lazy that wins publication ever has
            // Value read, so the classification starts exactly once.
            Lazy<Task>? mine = null;
            mine = new Lazy<Task>(() =>
            {
                // CancellationToken.None, deliberately: this task is shared with every later
                // caller for the same tile, so it must not carry the token of whoever happened
                // to ask first. Passing that token let one caller going away cancel the work
                // everyone else was waiting on — and, because the entry was only removed
                // after a successful await, the cancelled task stayed in the dictionary and was
                // handed to every subsequent caller, each of which got an
                // OperationCanceledException for a request it never cancelled. That poisoned
                // the tile for the lifetime of the network. Callers stay cancellable through
                // their own WaitAsync below.
                // The graphs belong to whichever request wins publication. A caller that
                // merely awaits this task does not get them populated — but the tile is
                // done by then, so its durable Islands entry answers instead.
                var started = IslandClassifier.BuildForTileAsync(network, profile, tileId,
                    dgFull, dgNonLocal, CancellationToken.None);

                // Remove on completion whatever the outcome, so a task that failed is retried by
                // the next caller rather than replayed at it forever. Removal is matched on this
                // exact Lazy: if a later caller has already published a replacement, a stale
                // continuation must not evict it and let a third caller start a duplicate run.
                _ = started.ContinueWith(
                    _ => _tilesInProgress.TryRemove(
                        new KeyValuePair<(string profile, uint tile), Lazy<Task>>(key, mine!)),
                    TaskContinuationOptions.ExecuteSynchronously);

                return started;
            });

            pending = _tilesInProgress.GetOrAdd(key, mine);
        }

        // Await the shared task, but only for as long as this caller is still interested. Giving up
        // here does not stop the classification for anyone else.
        await pending.Value.WaitAsync(cancellationToken);
    }

    internal RoutingNetworkIslandManager Clone()
    {
        // A profile added while iterating may or may not make it into the clone;
        // either is correct, it was not part of the network being cloned.
        var islands = new ConcurrentDictionary<string, Islands>();
        foreach (var (profileName, profileIslands) in _islands)
        {
            islands[profileName] = profileIslands.Clone();
        }

        return new RoutingNetworkIslandManager(this.MaxIslandSize, islands);
    }
}
