using System;
using Itinero.Network;

namespace Itinero.Data {
    /// <summary>
    /// Notifies listeners about data use in a router db.
    /// </summary>
    public class DataUseNotifier {
        /// <summary>
        /// Event raised when a vertex was touched.
        /// </summary>
        public event Action<RoutingNetwork, VertexId>? OnVertexTouched;

        internal void NotifyVertex(RoutingNetwork network, VertexId vertex) {
            OnVertexTouched?.Invoke(network, vertex);
        }

        /// <summary>
        /// Event raised when data within a bounding box was touched.
        /// </summary>
        public event
            Action<RoutingNetwork, ((double longitude, double latitude, float? e) topLeft, (double longitude, double
                latitude, float? e) bottomRight)>? OnBoxTouched;

        internal void NotifyBox(RoutingNetwork network,
            ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
                bottomRight) box) {
            OnBoxTouched?.Invoke(network, box);
        }
    }
}