using System.Collections.Generic;
using Itinero.Network;
using Itinero.Routes.Paths;

namespace Itinero.Tests.Routes.Paths;

internal static class PathScaffolding
{
    public static Path BuildPath(this RoutingNetwork network,
        IEnumerable<(EdgeId edge, bool direction)> edges)
    {
        var path = new Path(network);

        var edgeEnumerator = network.GetEdgeEnumerator();
        foreach (var e in edges)
        {
            edgeEnumerator.MoveTo(e.edge, e.direction);

            path.Append(e.edge, e.direction);
        }

        return path;
    }
}
