using System.Collections;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Routing.DataStructures;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// A lazy, struct-based enumerable over previous edges in a path tree.
/// Creating this struct is allocation-free. An enumerator is only allocated
/// when iterated through the IEnumerable interface (e.g., when passed to ICostFunction).
/// When iterated via foreach on the concrete type, the struct enumerator avoids boxing.
/// </summary>
internal readonly struct PreviousEdgeEnumerable : IEnumerable<(EdgeId edge, byte? turn)>
{
    private readonly PathTree? _tree;
    private readonly uint _pointer;

    public PreviousEdgeEnumerable(PathTree tree, uint pointer)
    {
        _tree = tree;
        _pointer = pointer;
    }

    /// <summary>
    /// Returns true if there are no previous edges.
    /// </summary>
    public bool IsEmpty => _tree == null || _pointer == uint.MaxValue;

    /// <summary>
    /// Returns a struct enumerator (no allocation when used via foreach on the concrete type).
    /// </summary>
    public Enumerator GetEnumerator() => new(_tree, _pointer);

    IEnumerator<(EdgeId edge, byte? turn)> IEnumerable<(EdgeId edge, byte? turn)>.GetEnumerator() =>
        new Enumerator(_tree, _pointer);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_tree, _pointer);

    public struct Enumerator : IEnumerator<(EdgeId edge, byte? turn)>
    {
        private readonly PathTree? _tree;
        private uint _pointer;
        private (EdgeId edge, byte? turn) _current;

        internal Enumerator(PathTree? tree, uint pointer)
        {
            _tree = tree;
            _pointer = pointer;
            _current = default;
        }

        public (EdgeId edge, byte? turn) Current => _current;

        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
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
