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

    /// <summary>
    /// Returns a snapshot of every edge currently in the graph, excluding the
    /// <see cref="MainNetworkSentinel"/>. Intended for callers that need to
    /// feed every known edge into a global resolution pass (e.g. Tarjan SCC
    /// over the full set of one-way singletons forming a cycle).
    /// </summary>
    public List<EdgeId> GetAllEdges()
    {
        _lock.EnterReadLock();
        try
        {
            var result = new List<EdgeId>(_parent.Count);
            foreach (var k in _parent.Keys)
            {
                if (k == MainNetworkSentinel) continue;
                result.Add(k);
            }
            return result;
        }
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
            // Snapshot — callers iterate outside the lock and concurrent
            // merges / RemoveEdge would otherwise mutate the list out from
            // under them.
            return _members.TryGetValue(root, out var m) ? new List<EdgeId>(m) : null;
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Adds a directed link <paramref name="from"/> → <paramref name="to"/> to the
    /// graph, with **eager cycle detection**: if a path already exists in the dg
    /// from <paramref name="to"/>'s component back to <paramref name="from"/>'s
    /// component, the new link closes a strongly-connected component. All
    /// components on every path back are merged into one node (F ∩ R: forward
    /// reachable from <paramref name="to"/> ∩ backward reachable from
    /// <paramref name="from"/>). Returns <c>true</c> when this happens, so the
    /// caller can size-check the merged component for MainNet graduation.
    /// Returns <c>false</c> when the link was a regular edge (no cycle closed).
    /// </summary>
    public bool AddDirectedLink(EdgeId from, EdgeId to)
    {
        _lock.EnterWriteLock();
        try
        {
            var fromRoot = this.FindNoLock(from);
            var toRoot = this.FindNoLock(to);
            if (fromRoot == toRoot) return false;

            // Eager cycle-merge: if there's already a path toRoot ↝ fromRoot
            // in the existing graph, adding from→to closes an SCC. Collapse
            // every component on a closing path into one node.
            if (this.PathExistsNoLock(toRoot, fromRoot))
            {
                var sccRoots = this.IntersectReachableNoLock(toRoot, fromRoot);
                sccRoots.Add(fromRoot);
                sccRoots.Add(toRoot);
                EdgeId target = fromRoot;
                foreach (var r in sccRoots)
                {
                    if (this.FindNoLock(r) != this.FindNoLock(target))
                        this.MergeNoLock(target, r);
                }
                return true;
            }

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

            return false;
        }
        finally { _lock.ExitWriteLock(); }
    }

    private bool PathExistsNoLock(EdgeId from, EdgeId to)
    {
        if (this.FindNoLock(from) == this.FindNoLock(to)) return true;
        var visited = new HashSet<EdgeId>();
        var stack = new Stack<EdgeId>();
        stack.Push(this.FindNoLock(from));
        while (stack.Count > 0)
        {
            var current = this.FindNoLock(stack.Pop());
            if (current == this.FindNoLock(to)) return true;
            if (!visited.Add(current)) continue;
            if (_outgoing.TryGetValue(current, out var outs))
            {
                foreach (var o in outs)
                {
                    var r = this.FindNoLock(o);
                    if (!visited.Contains(r)) stack.Push(r);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns components that are both forward-reachable from <paramref name="forwardStart"/>
    /// AND backward-reachable from <paramref name="backwardStart"/>. Used to find the
    /// SCC that closes when a new link <c>backwardStart → forwardStart</c> is about
    /// to be added.
    /// </summary>
    private HashSet<EdgeId> IntersectReachableNoLock(EdgeId forwardStart, EdgeId backwardStart)
    {
        var forward = new HashSet<EdgeId>();
        {
            var stack = new Stack<EdgeId>();
            stack.Push(this.FindNoLock(forwardStart));
            while (stack.Count > 0)
            {
                var current = this.FindNoLock(stack.Pop());
                if (!forward.Add(current)) continue;
                if (_outgoing.TryGetValue(current, out var outs))
                {
                    foreach (var o in outs)
                    {
                        var r = this.FindNoLock(o);
                        if (!forward.Contains(r)) stack.Push(r);
                    }
                }
            }
        }
        var result = new HashSet<EdgeId>();
        var bStack = new Stack<EdgeId>();
        var bVisited = new HashSet<EdgeId>();
        bStack.Push(this.FindNoLock(backwardStart));
        while (bStack.Count > 0)
        {
            var current = this.FindNoLock(bStack.Pop());
            if (!bVisited.Add(current)) continue;
            if (forward.Contains(current)) result.Add(current);
            if (_incoming.TryGetValue(current, out var ins))
            {
                foreach (var i in ins)
                {
                    var r = this.FindNoLock(i);
                    if (!bVisited.Contains(r)) bStack.Push(r);
                }
            }
        }
        return result;
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

    /// <summary>
    /// Diagnostic: returns (canForward, canBackward) one-direction reachability
    /// to <see cref="MainNetworkSentinel"/>. <c>(true,true)</c> matches
    /// <see cref="CanReachMainNetwork"/>; the other combinations expose the
    /// asymmetric cases.
    /// </summary>
    public (bool canForward, bool canBackward) ReachMainNetworkDirections(EdgeId edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            var root = this.FindNoLock(edgeId);
            var sentinel = this.FindNoLock(MainNetworkSentinel);
            if (root == sentinel) return (true, true);

            var visited = new HashSet<EdgeId>();
            var canForward = this.DfsCanReach(root, sentinel, visited, true);
            visited.Clear();
            var canBackward = this.DfsCanReach(root, sentinel, visited, false);
            return (canForward, canBackward);
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Returns the union-find roots this edge's component has outgoing dg links
    /// to (after <see cref="Find"/> canonicalisation). Includes the sentinel if
    /// the component links to it.
    /// </summary>
    public List<EdgeId> GetOutgoingRoots(EdgeId edgeId)
    {
        var result = new List<EdgeId>();
        _lock.EnterReadLock();
        try
        {
            if (!_parent.ContainsKey(edgeId)) return result;
            var root = this.FindNoLock(edgeId);
            if (_outgoing.TryGetValue(root, out var outs))
            {
                var seen = new HashSet<EdgeId>();
                foreach (var o in outs)
                {
                    var r = this.FindNoLock(o);
                    if (r == root) continue;
                    if (seen.Add(r)) result.Add(r);
                }
            }
        }
        finally { _lock.ExitReadLock(); }
        return result;
    }

    /// <summary>
    /// Returns the union-find roots this edge's component has incoming dg links
    /// from (after <see cref="Find"/> canonicalisation). Includes the sentinel
    /// if the sentinel links to the component.
    /// </summary>
    public List<EdgeId> GetIncomingRoots(EdgeId edgeId)
    {
        var result = new List<EdgeId>();
        _lock.EnterReadLock();
        try
        {
            if (!_parent.ContainsKey(edgeId)) return result;
            var root = this.FindNoLock(edgeId);
            if (_incoming.TryGetValue(root, out var ins))
            {
                var seen = new HashSet<EdgeId>();
                foreach (var i in ins)
                {
                    var r = this.FindNoLock(i);
                    if (r == root) continue;
                    if (seen.Add(r)) result.Add(r);
                }
            }
        }
        finally { _lock.ExitReadLock(); }
        return result;
    }

    /// <summary>
    /// Returns the union-find roots that this edge's component is directionally
    /// linked to (outgoing ∪ incoming), excluding the sentinel. Used by the
    /// edge-frontier BFS to keep expanding through already-processed edges:
    /// the dg knows their links from prior calls' processing, so the BFS can
    /// continue without re-doing the merge work.
    /// </summary>
    public List<EdgeId> GetLinkedNeighbourRoots(EdgeId edgeId)
    {
        var result = new List<EdgeId>();
        _lock.EnterReadLock();
        try
        {
            if (!_parent.ContainsKey(edgeId)) return result;
            var root = this.FindNoLock(edgeId);
            var sentinel = this.FindNoLock(MainNetworkSentinel);
            if (_outgoing.TryGetValue(root, out var outs))
            {
                foreach (var o in outs)
                {
                    var r = this.FindNoLock(o);
                    if (r != sentinel) result.Add(r);
                }
            }
            if (_incoming.TryGetValue(root, out var ins))
            {
                foreach (var i in ins)
                {
                    var r = this.FindNoLock(i);
                    if (r != sentinel && !result.Contains(r)) result.Add(r);
                }
            }
        }
        finally { _lock.ExitReadLock(); }
        return result;
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
