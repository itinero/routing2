/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System.Collections.Generic;
using Itinero.Data;
using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.Data.Shapes;
using Itinero.LocalGeo;

namespace Itinero
{
    public class RouterDb
    {
        private readonly Network _network;
        private readonly MappedAttributesIndex _edgesMeta;

        public RouterDb(int zoom = 14)
        {
            _network = new Network(zoom);
            _edgesMeta = new MappedAttributesIndex();
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _network.AddVertex(longitude, latitude);
        }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes associated with this edge.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public uint AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<Attribute> attributes = null,
            IEnumerable<Coordinate> shape = null)
        {
            var edgeId = _network.AddEdge(vertex1, vertex2, shape: shape);

            _edgesMeta[edgeId] = new AttributeCollection(attributes);
            
            return edgeId;
        }

        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public Graph.EdgeEnumerator GetEdgeEnumerator()
        {
            return _network.GetEdgeEnumerator();
        }

        /// <summary>
        /// Gets the shape for the given edge, if any.
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The shape.</returns>
        public ShapeBase GetShape(uint edgeId)
        {
            return _network.GetShape(edgeId);
        }

        /// <summary>
        /// Gets the attributes for the given edge, if any.
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The attributes.</returns>
        public IAttributeCollection GetAttributes(uint edgeId)
        {
            return _edgesMeta[edgeId];
        }
    }
}