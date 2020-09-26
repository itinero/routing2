using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Mutation;
using Itinero.Profiles;
using Itinero.Profiles.EdgeTypes;

namespace Itinero
{
    internal sealed class MutableRouterDb : IMutableRouterDb
    {
        private readonly RouterDb _routerDb;
        private readonly RouterDbProfileConfiguration _profileConfiguration;
        private readonly Graph.MutableGraph _mutableNetwork;

        internal MutableRouterDb(RouterDb routerDb)
        {
            _routerDb = routerDb;

            // make a copy of the latest network to write to.
            var latest = _routerDb.Graph;
            _mutableNetwork = latest.GetAsMutable();
            _profileConfiguration = _routerDb.ProfileConfiguration.Clone();
        }

        /// <inheritdoc/>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            return _mutableNetwork.TryGetVertex(vertex, out longitude, out latitude);
        }

        /// <inheritdoc/>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _mutableNetwork.AddVertex(longitude, latitude);
        }
        
        /// <inheritdoc/>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)>? shape = null, 
            IEnumerable<(string key, string value)>? attributes = null)
        {
            return _mutableNetwork.AddEdge(vertex1, vertex2, shape, attributes);
        }

        /// <inheritdoc/>
        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes, 
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            _mutableNetwork.AddTurnCosts(vertex, attributes, edges, costs, prefix);
        }

        RouterDbProfileConfiguration IMutableRouterDb.ProfileConfiguration => _profileConfiguration;

        /// <inheritdoc/>
        public GraphEdgeEnumerator<GraphMutator> GetEdgeEnumerator()
        {
            //return new MutableNetworkEdgeEnumerator(this._mutableNetwork);
            return this._mutableNetwork.GetEdgeEnumerator();
        }

        public void Dispose()
        {
            // update edge type function if needed.
            if (!_routerDb.ProfileConfiguration.HasAll(_profileConfiguration.Profiles))
            {
                var nextVersion = _mutableNetwork.EdgeTypeFunc.NextVersion(attributes =>
                    _profileConfiguration.Profiles.GetEdgeProfileFor(attributes));
                _mutableNetwork.SetEdgeTypeFunc(nextVersion);
            }
            
            // get network
            var network = _mutableNetwork.ToGraph();
            _mutableNetwork.Dispose();
            
            // finish router db mutations.
            var routerDbWriteable = _routerDb as IRouterDbMutations;
            routerDbWriteable.Finish(network, _profileConfiguration);
        }
    }

    internal interface IRouterDbMutations
    {
        void Finish(Graph latest, RouterDbProfileConfiguration profileConfiguration);
    }
}