using Itinero.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Itinero.Tiled
{
    public class RouterDbTiled
    {
        public RouterDbTiled(uint zoom = 15)
        {
            this.Graph = new GeometricGraph(zoom);
            this.EdgeProfiles = new AttributesIndex(AttributesIndexMode.IncreaseOne
                | AttributesIndexMode.ReverseAll);
        }

        public GeometricGraph Graph { get; private set; }

        public AttributesIndex EdgeProfiles { get; private set; }

        public float MaxEdgeDistance { get; private set; } = Constants.DefaultMaxEdgeDistance;
    }
}
