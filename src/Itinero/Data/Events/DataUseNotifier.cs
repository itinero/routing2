using System;
using Itinero.Data.Graphs;

namespace Itinero.Data.Events
{
    /// <summary>
    /// Notifies listeners about data use in a router db.
    /// </summary>
    public class DataUseNotifier
    {
        /// <summary>
        /// Event raised when a vertex was touched.
        /// </summary>
        public event Action<RouterDbInstance, VertexId>? OnVertexTouched;
        
        internal void NotifyVertex(RouterDbInstance routerDbInstance, VertexId vertex)
        {
            OnVertexTouched?.Invoke(routerDbInstance, vertex);
        }

        /// <summary>
        /// Event raised when data within a bounding box was touched.
        /// </summary>
        public event Action<RouterDbInstance, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>? OnBoxTouched;

        internal void NotifyBox(RouterDbInstance routerDbInstance, 
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            OnBoxTouched?.Invoke(routerDbInstance, box);
        }
    }
}