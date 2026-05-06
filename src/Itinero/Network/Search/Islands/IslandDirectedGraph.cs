using System.Collections.Generic;
using System.Threading;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// A directed meta-graph for island detection.
/// Each vertex represents one or more routing edges (merged via union-find).
/// Directed links represent travel direction between edge groups.
/// </summary>
internal class IslandDirectedGraph
{
    internal static readonly EdgeId MainNetworkSentinel = new(uint.MaxValue - 1, uint.MaxValue - 1);

    private readonly Dictionary<EdgeId, EdgeId> _parent = new();
    private readonly Dictionary<EdgeId, int> _rank = new();
    private readonly Dictionary<EdgeId, int> _size = new();
    private readonly Dictionary<EdgeId, HashSet<EdgeId>> _outgoing = new();
    private readonly Dictionary<EdgeId, HashSet<EdgeId>> _incoming = new();
    private readonly Dictionary<EdgeId, List<EdgeId>> _members = new();
    private readonly HashSet<EdgeId> _processed = new();

    // The graph is built incrementally (mutations) and queried concurrently from
    // many snap operations. The underlying Dictionaries / HashSets are not safe
    // for read-during-write — concurrent IsNotIsland calls during an in-flight
    // ProcessEdge corrupt the dict and throw "concurrent update". A reader-writer
    // lock keeps reads concurrent against each other (cheap on the snap path)
    // and exclusive against any mutation.
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

    public IslandDirectedGraph()
    {
        _parent[MainNetworkSentinel] = MainNetworkSentinel;
        _rank[MainNetworkSentinel] = int.MaxValue;
        _size[MainNetworkSentinel] = int.MaxValue;
    }

    public void AddVertex(EdgeId edgeId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_parent.ContainsKey(edgeId)) return;
            _parent[edgeId] = edgeId;
            _rank[edgeId] = 0;
            _size[edgeId] = 1;
            _members[edgeId] = new List<EdgeId> { edgeId };
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsInGraph(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try { return _parent.ContainsKey(edgeId); }
        finally { _lock.ExitReadLock(); }
    }

    public bool IsProcessed(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try { return _processed.Contains(edgeId); }
        finally { _lock.ExitReadLock(); }
    }

    public void SetProcessed(EdgeId edgeId)
    {
        _lock.EnterWriteLock();
        try { _processed.Add(edgeId); }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsNotIsland(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_parent.ContainsKey(edgeId)) return false;
            return this.FindNoLock(edgeId) == this.FindNoLock(MainNetworkSentinel);
        }
        finally { _lock.ExitReadLock(); }
    }

    public int GetSize(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try { return _size[this.FindNoLock(edgeId)]; }
        finally { _lock.ExitReadLock(); }
    }

    public List<EdgeId>? GetMembers(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            var root = this.FindNoLock(edgeId);
            return _members.TryGetValue(root, out var m) ? m : null;
        }
        finally { _lock.ExitReadLock(); }
    }

    public void AddDirectedLink(EdgeId from, EdgeId to)
    {
        _lock.EnterWriteLock();
        try
        {
            var fromRoot = this.FindNoLock(from);
            var toRoot = this.FindNoLock(to);
            if (fromRoot == toRoot) return;

            if (!_outgoing.TryGetValue(fromRoot, out var targets))
            {
                targets = new HashSet<EdgeId>();
                _outgoing[fromRoot] = targets;
            }

            if (targets.Add(toRoot))
            {
                // also update incoming index
                if (!_incoming.TryGetValue(toRoot, out var sources))
                {
                    sources = new HashSet<EdgeId>();
                    _incoming[toRoot] = sources;
                }
                sources.Add(fromRoot);
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool HasDirectedLink(EdgeId from, EdgeId to)
    {
        _lock.EnterReadLock();
        try
        {
            var fromRoot = this.FindNoLock(from);
            var toRoot = this.FindNoLock(to);
            return _outgoing.TryGetValue(fromRoot, out var targets) && targets.Contains(toRoot);
        }
        finally { _lock.ExitReadLock(); }
    }

    public void Merge(EdgeId a, EdgeId b)
    {
        _lock.EnterWriteLock();
        try { this.MergeNoLock(a, b); }
        finally { _lock.ExitWriteLock(); }
    }

    private void MergeNoLock(EdgeId a, EdgeId b)
    {
        var rootA = this.FindNoLock(a);
        var rootB = this.FindNoLock(b);
        if (rootA == rootB) return;

        // sentinel always wins
        var sentinelRoot = this.FindNoLock(MainNetworkSentinel);
        if (rootB == sentinelRoot)
            (rootA, rootB) = (rootB, rootA);
        else if (rootA != sentinelRoot && _rank[rootA] < _rank[rootB])
            (rootA, rootB) = (rootB, rootA);

        _parent[rootB] = rootA;
        if (rootA != sentinelRoot)
        {
            _size[rootA] += _size[rootB];
            if (_rank[rootA] == _rank[rootB]) _rank[rootA]++;
        }

        // merge outgoing
        if (_outgoing.TryGetValue(rootB, out var bOut))
        {
            if (!_outgoing.TryGetValue(rootA, out var aOut))
            {
                aOut = new HashSet<EdgeId>();
                _outgoing[rootA] = aOut;
            }

            foreach (var t in bOut)
            {
                var tRoot = this.FindNoLock(t);
                if (tRoot != rootA)
                {
                    aOut.Add(tRoot);
                    // update incoming: t's incoming should point to rootA not rootB
                    if (_incoming.TryGetValue(tRoot, out var tInc))
                    {
                        tInc.Remove(rootB);
                        tInc.Add(rootA);
                    }
                }
            }

            _outgoing.Remove(rootB);
        }

        // merge incoming
        if (_incoming.TryGetValue(rootB, out var bInc))
        {
            if (!_incoming.TryGetValue(rootA, out var aInc))
            {
                aInc = new HashSet<EdgeId>();
                _incoming[rootA] = aInc;
            }

            foreach (var s in bInc)
            {
                var sRoot = this.FindNoLock(s);
                if (sRoot != rootA)
                {
                    aInc.Add(sRoot);
                    // update outgoing: s's outgoing should point to rootA not rootB
                    if (_outgoing.TryGetValue(sRoot, out var sOut))
                    {
                        sOut.Remove(rootB);
                        sOut.Add(rootA);
                    }
                }
            }

            _incoming.Remove(rootB);
        }

        // remove self-loops
        if (_outgoing.TryGetValue(rootA, out var aOutFinal))
            aOutFinal.Remove(rootA);
        if (_incoming.TryGetValue(rootA, out var aIncFinal))
            aIncFinal.Remove(rootA);

        // merge members
        if (_members.TryGetValue(rootB, out var bMembers))
        {
            if (rootA == sentinelRoot)
            {
                _members.Remove(rootB);
            }
            else
            {
                if (!_members.TryGetValue(rootA, out var aMembers))
                {
                    aMembers = new List<EdgeId>();
                    _members[rootA] = aMembers;
                }
                aMembers.AddRange(bMembers);
                _members.Remove(rootB);
            }
        }
    }

    public void CollapseToMainNetwork(EdgeId root)
    {
        _lock.EnterWriteLock();
        try
        {
            root = this.FindNoLock(root);
            if (root == this.FindNoLock(MainNetworkSentinel)) return;
            this.MergeNoLock(MainNetworkSentinel, root);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void RemoveEdge(EdgeId edgeId)
    {
        _lock.EnterWriteLock();
        try
        {
            var root = this.FindNoLock(edgeId);

            if (_members.TryGetValue(root, out var members))
            {
                members.Remove(edgeId);
                if (members.Count == 0)
                {
                    _members.Remove(root);

                    // clean up adjacency
                    if (_outgoing.TryGetValue(root, out var targets))
                    {
                        foreach (var t in targets)
                        {
                            if (_incoming.TryGetValue(this.FindNoLock(t), out var tInc))
                                tInc.Remove(root);
                        }
                        _outgoing.Remove(root);
                    }

                    if (_incoming.TryGetValue(root, out var sources))
                    {
                        foreach (var s in sources)
                        {
                            if (_outgoing.TryGetValue(this.FindNoLock(s), out var sOut))
                                sOut.Remove(root);
                        }
                        _incoming.Remove(root);
                    }
                }
            }

            _parent.Remove(edgeId);
            _processed.Remove(edgeId);
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// O(1) dead-end check using incoming index.
    /// </summary>
    public bool IsDeadEnd(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            var root = this.FindNoLock(edgeId);
            if (root == this.FindNoLock(MainNetworkSentinel)) return false;

            var hasOutgoing = _outgoing.TryGetValue(root, out var targets) && targets.Count > 0;
            if (!hasOutgoing) return true;

            var hasIncoming = _incoming.TryGetValue(root, out var sources) && sources.Count > 0;
            return !hasIncoming;
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Checks if this edge can reach the sentinel in BOTH directions.
    /// </summary>
    public bool CanReachMainNetwork(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            var root = this.FindNoLock(edgeId);
            var sentinel = this.FindNoLock(MainNetworkSentinel);
            if (root == sentinel) return true;

            var visited = new HashSet<EdgeId>();
            var canForward = this.DfsCanReach(root, sentinel, visited, true);
            if (!canForward) return false;

            visited.Clear();
            return this.DfsCanReach(root, sentinel, visited, false);
        }
        finally { _lock.ExitReadLock(); }
    }

    private bool DfsCanReach(EdgeId current, EdgeId target, HashSet<EdgeId> visited, bool forward)
    {
        current = this.FindNoLock(current);
        if (current == target) return true;
        if (!visited.Add(current)) return false;

        var adj = forward
            ? (_outgoing.TryGetValue(current, out var o) ? o : null)
            : (_incoming.TryGetValue(current, out var i) ? i : null);

        if (adj == null) return false;

        foreach (var next in adj)
        {
            if (this.DfsCanReach(this.FindNoLock(next), target, visited, forward)) return true;
        }

        return false;
    }

    /// <summary>
    /// Detects cycles among the given candidate roots and merges them.
    /// Uses Tarjan's SCC algorithm on the subgraph of candidates.
    /// Returns true if any merges happened.
    /// </summary>
    public bool DetectAndMergeCycles(List<EdgeId> candidateRoots)
    {
        _lock.EnterWriteLock();
        try
        {
            // build the set of roots to consider
            var rootSet = new HashSet<EdgeId>();
            foreach (var c in candidateRoots)
            {
                var r = this.FindNoLock(c);
                if (r != this.FindNoLock(MainNetworkSentinel))
                    rootSet.Add(r);
            }

            if (rootSet.Count < 2) return false;

            // Tarjan's SCC
            var index = 0;
            var stack = new Stack<EdgeId>();
            var onStack = new HashSet<EdgeId>();
            var indices = new Dictionary<EdgeId, int>();
            var lowLinks = new Dictionary<EdgeId, int>();
            var sccs = new List<List<EdgeId>>();

            foreach (var v in rootSet)
            {
                if (!indices.ContainsKey(v))
                    this.Strongconnect(v, rootSet, ref index, stack, onStack, indices, lowLinks, sccs);
            }

            // merge SCCs with more than one vertex
            var merged = false;
            foreach (var scc in sccs)
            {
                if (scc.Count < 2) continue;
                for (var i = 1; i < scc.Count; i++)
                {
                    this.MergeNoLock(scc[0], scc[i]);
                }
                merged = true;
            }

            return merged;
        }
        finally { _lock.ExitWriteLock(); }
    }

    private void Strongconnect(EdgeId v, HashSet<EdgeId> rootSet,
        ref int index, Stack<EdgeId> stack, HashSet<EdgeId> onStack,
        Dictionary<EdgeId, int> indices, Dictionary<EdgeId, int> lowLinks,
        List<List<EdgeId>> sccs)
    {
        indices[v] = index;
        lowLinks[v] = index;
        index++;
        stack.Push(v);
        onStack.Add(v);

        if (_outgoing.TryGetValue(v, out var targets))
        {
            foreach (var t in targets)
            {
                var w = this.FindNoLock(t);
                if (!rootSet.Contains(w)) continue; // only consider candidates

                if (!indices.ContainsKey(w))
                {
                    this.Strongconnect(w, rootSet, ref index, stack, onStack, indices, lowLinks, sccs);
                    lowLinks[v] = System.Math.Min(lowLinks[v], lowLinks[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowLinks[v] = System.Math.Min(lowLinks[v], indices[w]);
                }
            }
        }

        if (lowLinks[v] == indices[v])
        {
            var scc = new List<EdgeId>();
            EdgeId w;
            do
            {
                w = stack.Pop();
                onStack.Remove(w);
                scc.Add(w);
            } while (w != v);

            sccs.Add(scc);
        }
    }

    /// <summary>
    /// Walks up the union-find chain to the root. Acquires the read lock; for callers
    /// that already hold the lock (read or write), use <see cref="FindNoLock"/>.
    /// </summary>
    public EdgeId Find(EdgeId x)
    {
        _lock.EnterReadLock();
        try { return this.FindNoLock(x); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Lock-free Find for use inside methods that already hold _lock. Intentionally
    /// does NOT path-compress: the graph is built once then queried from many concurrent
    /// snap calls; compressing would mutate <see cref="_parent"/> during reads and
    /// require an exclusive lock for every Find. Without compression each Find is
    /// O(log n) thanks to rank-balanced unions in <see cref="MergeNoLock"/> — fast
    /// enough for the snap path.
    /// </summary>
    private EdgeId FindNoLock(EdgeId x)
    {
        if (!_parent.TryGetValue(x, out var parent)) return x;
        while (parent != x)
        {
            x = parent;
            if (!_parent.TryGetValue(x, out parent)) return x;
        }
        return x;
    }
}
