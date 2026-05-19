using System;
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
    private readonly Dictionary<string, IslandDirectedGraph> _directedGraphs = new();
    private readonly ReaderWriterLockSlim _islandsLock = new();

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

            if (!_directedGraphs.TryGetValue(profileName, out var dg))
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

    /// <summary>
    /// Returns the per-profile <see cref="IIslandClassificationStore"/> that
    /// <see cref="IslandClassifier"/> should use. Backs onto the same
    /// <see cref="Islands"/> + <see cref="IslandDirectedGraph"/> pair the snap
    /// fast-path already reads, so classifications persist across calls and
    /// feed <c>Snapper.IsAcceptable</c> directly.
    /// </summary>
    internal IIslandClassificationStore GetClassificationStoreFor(Profile profile)
    {
        var islands = this.GetIslandsFor(profile);
        var dg = this.GetOrCreateDirectedGraph(profile);
        return new IslandManagerClassificationStore(islands, dg);
    }

    internal IslandDirectedGraph GetOrCreateDirectedGraph(Profile profile)
    {
        try
        {
            _islandsLock.EnterUpgradeableReadLock();

            if (_directedGraphs.TryGetValue(profile.Name, out var dg)) return dg;

            try
            {
                _islandsLock.EnterWriteLock();

                dg = new IslandDirectedGraph();
                _directedGraphs[profile.Name] = dg;
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

                    task = IslandBuilder.BuildForTileAsync(network, profile, tileId, cancellationToken);
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
