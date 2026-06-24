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
    private readonly Dictionary<(string profile, uint tile), Task> _tilesInProgress = new();
    private readonly ReaderWriterLockSlim _tilesInProgressLock = new();
    private readonly Dictionary<string, Islands> _islands;
    private readonly Dictionary<(string profile, IslandKind kind), IslandDirectedGraph> _directedGraphs = new();
    private readonly ReaderWriterLockSlim _islandsLock = new();

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
        _islands = new();
    }

    private RoutingNetworkIslandManager(int maxIslandSize, Dictionary<string, Islands> islands)
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
        try
        {
            _islandsLock.EnterReadLock();

            // Snapping is a Full-classification concern, so the existing
            // single-DG semantics route through the Full DG.
            if (!_directedGraphs.TryGetValue((profileName, IslandKind.Full), out var dg))
                return null;

            if (!_islands.TryGetValue(profileName, out var profileIslands))
                return null;
            if (profileIslands.IsEdgeOnIsland(edgeId))
                return true;

            if (dg.IsNotIsland(edgeId))
                return false;

            return null;
        }
        finally
        {
            _islandsLock.ExitReadLock();
        }
    }

    internal IslandDirectedGraph GetOrCreateDirectedGraph(Profile profile, IslandKind kind = IslandKind.Full)
    {
        var key = (profile.Name, kind);
        try
        {
            _islandsLock.EnterUpgradeableReadLock();

            if (_directedGraphs.TryGetValue(key, out var dg)) return dg;

            try
            {
                _islandsLock.EnterWriteLock();

                dg = new IslandDirectedGraph();
                _directedGraphs[key] = dg;
                return dg;
            }
            finally
            {
                _islandsLock.ExitWriteLock();
            }
        }
        finally
        {
            _islandsLock.ExitUpgradeableReadLock();
        }
    }

    internal int MaxIslandSize { get; }

    internal bool TryGetIslandsFor(string profileName, out Islands islands)
    {
        try
        {
            _islandsLock.EnterReadLock();

            return _islands.TryGetValue(profileName, out islands);
        }
        finally
        {
            _islandsLock.ExitReadLock();
        }
    }

    internal Islands GetIslandsFor(Profile profile)
    {
        try
        {
            _islandsLock.EnterUpgradeableReadLock();

            if (_islands.TryGetValue(profile.Name, out var islands)) return islands;

            try
            {
                _islandsLock.EnterWriteLock();

                islands = new Islands();
                _islands[profile.Name] = islands;
                return islands;
            }
            finally
            {
                _islandsLock.ExitWriteLock();
            }
        }
        finally
        {
            _islandsLock.ExitUpgradeableReadLock();
        }
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

        try
        {
            _islandsLock.EnterReadLock();

            if (!_islands.TryGetValue(profile.Name, out var islands)) return null;

            // Unreachable in the Full classification → not in main-N.
            if (islands.IsEdgeOnIsland(edgeId)) return false;

            // Non-main-N pocket → not in main-N.
            if (islands.IsEdgeLocal(edgeId)) return false;

            // Tile finished classifying and the edge is in neither set → main-N.
            // Otherwise we don't yet know.
            return islands.GetTileDone(edgeId.TileId) ? true : null;
        }
        finally
        {
            _islandsLock.ExitReadLock();
        }
    }

    internal async Task BuildForTileAsync(RoutingNetwork network, Profile profile, uint tileId,
        CancellationToken cancellationToken)
    {
        // queue task, if not done yet.
        Task task;
        try
        {
            _tilesInProgressLock.EnterUpgradeableReadLock();

            if (!_tilesInProgress.TryGetValue((profile.Name, tileId), out task))
            {
                try
                {
                    _tilesInProgressLock.EnterWriteLock();

                    task = IslandClassifier.BuildForTileAsync(network, profile, tileId, cancellationToken);
                    _tilesInProgress[(profile.Name, tileId)] = task;
                }
                finally
                {
                    _tilesInProgressLock.ExitWriteLock();
                }
            }
        }
        finally
        {
            _tilesInProgressLock.ExitUpgradeableReadLock();
        }

        // await the task.
        await task;

        // remove from the queue.
        try
        {
            _tilesInProgressLock.EnterUpgradeableReadLock();

            if (_tilesInProgress.ContainsKey((profile.Name, tileId)))
            {
                try
                {
                    _tilesInProgressLock.EnterWriteLock();

                    _tilesInProgress.Remove((profile.Name, tileId));
                }
                finally
                {
                    _tilesInProgressLock.ExitWriteLock();
                }
            }
        }
        finally
        {
            _tilesInProgressLock.ExitUpgradeableReadLock();
        }
    }

    internal RoutingNetworkIslandManager Clone()
    {
        try
        {
            _islandsLock.EnterReadLock();

            var islands = new Dictionary<string, Islands>();
            foreach (var (profileName, profileIslands) in _islands)
            {
                islands[profileName] = profileIslands.Clone();
            }

            return new RoutingNetworkIslandManager(this.MaxIslandSize, islands);
        }
        finally
        {
            _islandsLock.ExitReadLock();
        }
    }
}
