using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    public sealed partial class Network
    {
        private NetworkWriter? _writer;

        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;

        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public NetworkWriter GetWriter()
        {
            if (_writer != null)
                throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                    $"Check {nameof(HasWriter)} to check for a current writer.");
            _writer = new NetworkWriter(this);
            return _writer;
        }

        void INetworkWritable.ClearWriter()
        {
            _writer = null;
        }

        /// <summary>
        /// A writer to write to an instance. This writer will never change existing data, only add new data.
        ///
        /// This writer can:
        /// - add new vertices and edges.
        ///
        /// This writer cannot mutate existing data, only add new.
        /// </summary>
        public sealed class NetworkWriter : IDisposable
        {
            private readonly Network _network;
            private readonly Graph.GraphWriter _graphWriter;

            internal NetworkWriter(Network network)
            {
                _network = network;

                _graphWriter = _network.Graph.GetWriter();
            }

            /// <summary>
            /// Adds a new vertex and returns its id.
            /// </summary>
            /// <param name="longitude">The longitude.</param>
            /// <param name="latitude">The latitude.</param>
            /// <returns>The ID of the new vertex.</returns>
            public VertexId AddVertex(double longitude, double latitude)
            {
                return _graphWriter.AddVertex(longitude, latitude);
            }

            /// <summary>
            /// Adds a new edge and returns its id.
            /// </summary>
            /// <param name="vertex1">The first vertex.</param>
            /// <param name="vertex2">The second vertex.</param>
            /// <param name="attributes">The attributes.</param>
            /// <param name="shape">The shape points.</param>
            /// <returns>The edge id.</returns>
            public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
                IEnumerable<(double longitude, double latitude)>? shape = null,
                IEnumerable<(string key, string value)>? attributes = null)
            {
                return _graphWriter.AddEdge(vertex1, vertex2, shape, attributes);
            }

            public void Dispose()
            {
                (_network as INetworkWritable).ClearWriter();
            }
        }
    }

    internal interface INetworkWritable
    {
        void ClearWriter();
    }
}