using System;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero.Algorithms.Dijkstra
{
    public static class SnapPointExtensions
    {
        public static (VertexId vertex, uint edge, float cost) ToDijkstraLocation(this SnapPoint snapPoint,
            RouterDb routerDb, Profile profile)
        {
            var enumerator = routerDb.Network.Graph.GetEnumerator();
            if (!enumerator.MoveToEdge(snapPoint.EdgeId)) throw new ArgumentOutOfRangeException(nameof(snapPoint), 
                $"Edge for snap point {snapPoint} not found!");

            return (enumerator.To, snapPoint.EdgeId, 0);
        }
    }
}