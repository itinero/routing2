namespace Itinero.Network.Search.Islands;

internal static class IslandBuilderExtensions
{
    internal static (EdgeId edge, bool forward) Invert(this (EdgeId edge, bool forward) edge)
    {
        return (edge.edge, !edge.forward);
    }

    internal static bool CanBeTraversed(this (EdgeId edge, bool forward) edge,
        (bool forward, bool backward) cost)
    {
        switch (edge.forward)
        {
            case true when cost.forward: // edge is in the forward direction and can be traversed forward.
            case false when cost.backward: // edge is in the backward direction and can be traversed backward.
                return true;
            default:
                return false;
        }
    }
}
