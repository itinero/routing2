using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Profiles;
using Itinero.Profiles.EdgeTypes;

namespace Itinero
{
    internal sealed class MutableRouterDb : IMutableRouterDb
    {
        private readonly RouterDb _routerDb;
        private readonly RouterDbProfileConfiguration _profileConfiguration;
        private readonly Network.MutableNetwork _mutableNetwork;

        internal MutableRouterDb(RouterDb routerDb)
        {
            _routerDb = routerDb;

            // make a copy of the latest network to write to.
            var latest = _routerDb.Network;
            _mutableNetwork = latest.GetAsMutable();
            _profileConfiguration = _routerDb.ProfileConfiguration.Clone();
        }

        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            return _mutableNetwork.TryGetVertex(vertex, out longitude, out latitude);
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _mutableNetwork.AddVertex(longitude, latitude);
        }
        
        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)>? shape = null, 
            IEnumerable<(string key, string value)>? attributes = null)
        {
            return _mutableNetwork.AddEdge(vertex1, vertex2, shape, attributes);
        }

        RouterDbProfileConfiguration IMutableRouterDb.ProfileConfiguration => _profileConfiguration;

        public void Dispose()
        {
            // update edge type function if needed.
            if (!_routerDb.ProfileConfiguration.HasAll(_profileConfiguration.Profiles))
            {
                _mutableNetwork.SetEdgeTypeFunc(attributes => 
                    _profileConfiguration.Profiles.GetEdgeProfileFor(attributes));
            }
            
            // get network
            var network = _mutableNetwork.ToNetwork();
            _mutableNetwork.Dispose();
            
            // finish router db mutations.
            var routerDbWriteable = _routerDb as IRouterDbMutations;
            routerDbWriteable.Finish(network, _profileConfiguration);
        }
    }

    internal interface IRouterDbMutations
    {
        void Finish(Network latest, RouterDbProfileConfiguration profileConfiguration);
    }
}