using System.Collections;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Routing.DataStructures;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// A lazy, struct-based enumerable over previous edges in a path tree.
/// </summary>
/// <remarks>
/// Creating this is allocation-free and <c>foreach</c> on the concrete type uses the struct
/// enumerator. Reaching it through <see cref="IEnumerable{T}"/> is not free, and that was the
/// trap: <c>ICostFunction.Get</c> declared the parameter as the interface, so every call boxed
/// the struct, and every enumeration boxed the enumerator too. In the allocation profile those
/// were 10.4% and 12.9% — **23% of everything the service allocated**, for a sequence whose
/// first element is all anyone reads.
///
/// So the cost function takes this concrete type now. Public for that reason, with an internal
/// constructor: implementations outside Itinero receive one and pass it on, but only the search
/// can create one.
/// </remarks>
public readonly struct PreviousEdgeEnumerable : IEnumerable<(EdgeId edge, byte? turn)>
{
    private readonly PathTree? _tree;
    private readonly uint _pointer;
    private readonly EdgeId _singleEdge;
    private readonly byte? _singleTurn;
    private readonly bool _hasSingle;

    internal PreviousEdgeEnumerable(PathTree tree, uint pointer)
    {
        _tree = tree;
        _pointer = pointer;
    }

    private PreviousEdgeEnumerable(EdgeId edge, byte? turn)
    {
        _pointer = uint.MaxValue;
        _singleEdge = edge;
        _singleTurn = turn;
        _hasSingle = true;
    }

    /// <summary>
    /// A sequence of exactly one previous edge.
    /// </summary>
    /// <remarks>
    /// The callers that need this were writing <c>new[] { (edge, order) }</c> — an array
    /// allocated per call to carry a single element through an interface that then boxed it.
    /// Held inline here instead, so the whole thing stays on the stack.
    /// </remarks>
    public static PreviousEdgeEnumerable ForEdge(EdgeId edge, byte? turn) => new(edge, turn);

    /// <summary>
    /// Returns true if there are no previous edges.
    /// </summary>
    public bool IsEmpty => !_hasSingle && (_tree == null || _pointer == uint.MaxValue);

    /// <summary>
    /// Returns a struct enumerator (no allocation when used via foreach on the concrete type).
    /// </summary>
    public Enumerator GetEnumerator() => new(_tree, _pointer, _singleEdge, _singleTurn, _hasSingle);

    /// <summary>
    /// The most recent previous edge, or null when there is none.
    /// </summary>
    /// <remarks>
    /// Every cost function today wants exactly this and used to get it with
    /// <c>previousEdges.FirstOrDefault()</c> — a LINQ call through the interface, which is what
    /// boxed the enumerator. The full sequence stays available for the prefix comparison the
    /// turn-cost code has a TODO for; this is just the part anyone actually reads.
    /// </remarks>
    public (EdgeId edge, byte? turn)? First
    {
        get
        {
            var enumerator = this.GetEnumerator();

            return enumerator.MoveNext() ? enumerator.Current : null;
        }
    }

    IEnumerator<(EdgeId edge, byte? turn)> IEnumerable<(EdgeId edge, byte? turn)>.GetEnumerator() =>
        new Enumerator(_tree, _pointer, _singleEdge, _singleTurn, _hasSingle);

    IEnumerator IEnumerable.GetEnumerator() =>
        new Enumerator(_tree, _pointer, _singleEdge, _singleTurn, _hasSingle);

    public struct Enumerator : IEnumerator<(EdgeId edge, byte? turn)>
    {
        private readonly PathTree? _tree;
        private uint _pointer;
        private (EdgeId edge, byte? turn) _current;
        private bool _single;

        internal Enumerator(PathTree? tree, uint pointer, EdgeId singleEdge, byte? singleTurn,
            bool hasSingle)
        {
            _tree = tree;
            _pointer = pointer;
            _current = hasSingle ? (singleEdge, singleTurn) : default;
            _single = hasSingle;
        }

        public (EdgeId edge, byte? turn) Current => _current;

        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_single)
            {
                // _current already holds it; yield once and stop.
                _single = false;
                return true;
            }

            if (_tree == null || _pointer == uint.MaxValue) return false;

            var (_, edge, _, head, next) = PathTreeExtensions.GetVisit(_tree, _pointer);
            _current = (edge, head);
            _pointer = next;
            return true;
        }

        public void Reset() { }

        public void Dispose() { }
    }
}
