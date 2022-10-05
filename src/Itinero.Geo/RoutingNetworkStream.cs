using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Network;
using NetTopologySuite.Features;

namespace Itinero.Geo
{
    /// <summary>
    ///     Converts a routerDB into a stream of geofeatures
    /// </summary>
    internal class RoutingNetworkStream : IEnumerable<IFeature>
    {
        private readonly RoutingNetwork _network;

        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>
            _preprocessEdgeAttributes;

        public RoutingNetworkStream(RoutingNetwork network,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>
                preprocessEdgeAttributes)
        {
            _network = network;
            _preprocessEdgeAttributes = preprocessEdgeAttributes;
        }

        public IEnumerator<IFeature> GetEnumerator()
        {
            return new RoutingNetworkEnumerator(_network, _preprocessEdgeAttributes);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
