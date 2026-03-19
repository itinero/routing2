using System.Collections.Generic;
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

    public IslandDirectedGraph()
    {
        _parent[MainNetworkSentinel] = MainNetworkSentinel;
        _rank[MainNetworkSentinel] = int.MaxValue;
        _size[MainNetworkSentinel] = int.MaxValue;
    }

    public void AddVertex(EdgeId edgeId)
    {
        if (_parent.ContainsKey(edgeId)) return;
        _parent[edgeId] = edgeId;
        _rank[edgeId] = 0;
        _size[edgeId] = 1;
        _members[edgeId] = new List<EdgeId> { edgeId };
    }

    public bool IsInGraph(EdgeId edgeId) => _parent.ContainsKey(edgeId);
    public bool IsProcessed(EdgeId edgeId) => _processed.Contains(edgeId);
    public void SetProcessed(EdgeId edgeId) => _processed.Add(edgeId);

    public bool IsNotIsland(EdgeId edgeId)
    {
        if (!_parent.ContainsKey(edgeId)) return false;
        return this.Find(edgeId) == this.Find(MainNetworkSentinel);
    }

    public int GetSize(EdgeId edgeId) => _size[this.Find(edgeId)];

    public List<EdgeId>? GetMembers(EdgeId edgeId)
    {
        var root = this.Find(edgeId);
        return _members.TryGetValue(root, out var m) ? m : null;
    }

    public void AddDirectedLink(EdgeId from, EdgeId to)
    {
        var fromRoot = this.Find(from);
        var toRoot = this.Find(to);
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

    public bool HasDirectedLink(EdgeId from, EdgeId to)
    {
        var fromRoot = this.Find(from);
        var toRoot = this.Find(to);
        return _outgoing.TryGetValue(fromRoot, out var targets) && targets.Contains(toRoot);
    }

    public void Merge(EdgeId a, EdgeId b)
    {
        var rootA = this.Find(a);
        var rootB = this.Find(b);
        if (rootA == rootB) return;

        // sentinel always wins
        var sentinelRoot = this.Find(MainNetworkSentinel);
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
                var tRoot = this.Find(t);
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
                var sRoot = this.Find(s);
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
        root = this.Find(root);
        if (root == this.Find(MainNetworkSentinel)) return;
        this.Merge(MainNetworkSentinel, root);
    }

    public void RemoveEdge(EdgeId edgeId)
    {
        var root = this.Find(edgeId);

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
                        if (_incoming.TryGetValue(this.Find(t), out var tInc))
                            tInc.Remove(root);
                    }
                    _outgoing.Remove(root);
                }

                if (_incoming.TryGetValue(root, out var sources))
                {
                    foreach (var s in sources)
                    {
                        if (_outgoing.TryGetValue(this.Find(s), out var sOut))
                            sOut.Remove(root);
                    }
                    _incoming.Remove(root);
                }
            }
        }

        _parent.Remove(edgeId);
        _processed.Remove(edgeId);
    }

    /// <summary>
    /// O(1) dead-end check using incoming index.
    /// </summary>
    public bool IsDeadEnd(EdgeId edgeId)
    {
        var root = this.Find(edgeId);
        if (root == this.Find(MainNetworkSentinel)) return false;

        var hasOutgoing = _outgoing.TryGetValue(root, out var targets) && targets.Count > 0;
        if (!hasOutgoing) return true;

        var hasIncoming = _incoming.TryGetValue(root, out var sources) && sources.Count > 0;
        return !hasIncoming;
    }

    /// <summary>
    /// Checks if this edge can reach the sentinel in BOTH directions.
    /// </summary>
    public bool CanReachMainNetwork(EdgeId edgeId)
    {
        var root = this.Find(edgeId);
        var sentinel = this.Find(MainNetworkSentinel);
        if (root == sentinel) return true;

        var visited = new HashSet<EdgeId>();
        var canForward = this.DfsCanReach(root, sentinel, visited, true);
        if (!canForward) return false;

        visited.Clear();
        return this.DfsCanReach(root, sentinel, visited, false);
    }

    private bool DfsCanReach(EdgeId current, EdgeId target, HashSet<EdgeId> visited, bool forward)
    {
        current = this.Find(current);
        if (current == target) return true;
        if (!visited.Add(current)) return false;

        var adj = forward
            ? (_outgoing.TryGetValue(current, out var o) ? o : null)
            : (_incoming.TryGetValue(current, out var i) ? i : null);

        if (adj == null) return false;

        foreach (var next in adj)
        {
            if (this.DfsCanReach(this.Find(next), target, visited, forward)) return true;
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
        // build the set of roots to consider
        var rootSet = new HashSet<EdgeId>();
        foreach (var c in candidateRoots)
        {
            var r = this.Find(c);
            if (r != this.Find(MainNetworkSentinel))
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
                this.Merge(scc[0], scc[i]);
            }
            merged = true;
        }

        return merged;
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
                var w = this.Find(t);
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

    public EdgeId Find(EdgeId x)
    {
        if (!_parent.ContainsKey(x)) return x;
        if (_parent[x] != x) _parent[x] = this.Find(_parent[x]);
        return _parent[x];
    }
}
