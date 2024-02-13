using System.Collections.Generic;
using Itinero.Network.Search.Islands;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    // TODO: refactor this to something proper later after we have a better idea of how islands can work!
    internal readonly Dictionary<string, IslandLabels> _islandLabels = new();
}
