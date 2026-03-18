using System.Collections.Generic;
using Itinero.Routes.Paths;

namespace Itinero.MapMatching.Model;

public class GraphEdge
{
    public int Node1 { get; set; }

    public int Node2 { get; set; }

    public double Cost { get; set; }

    public List<(string key, string value)>? Attributes { get; set; }

    /// <summary>
    /// The cached route path between the two snap points, if available.
    /// </summary>
    public Path? CachedPath { get; set; }

    public override string ToString()
    {
        return $"{this.Node1} -> {this.Node2} with c={this.Cost}";
    }
}
