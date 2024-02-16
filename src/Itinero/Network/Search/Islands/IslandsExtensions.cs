using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search.Islands;

internal static class IslandsExtensions
{
    public static bool? IsEdgeOnIsland(this Islands islands, IEdgeEnumerator e, uint tileNotDone = uint.MaxValue)
    {
        var onIsland = islands.IsEdgeOnIsland(e.EdgeId);
        if (onIsland) return true;

        var t = e.Tail.TileId;
        if (!e.Forward) t = e.Head.TileId;

        if (t == tileNotDone) return null;
        if (!islands.GetTileDone(t)) return null;

        // tile is done, we are sure this edge is not on an island.
        return false;
    }
}
