using System;
using Itinero.Algorithms;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;
using Itinero.Geo;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;

namespace Itinero
{
    /// <summary>
    /// Extension methods related to snapping and snap points.
    /// </summary>
    public static class SnapPointExtensions
    {
        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="location">The location.</param>
        /// <param name="maxOffsetInMeter">The maximum offset in meter.</param>
        /// <param name="profile">The profile to snap for.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RouterDb routerDb, (double longitude, double latitude) location, float maxOffsetInMeter = 1000,
            Profile profile = null)
        {
            // TODO: convert this to something using snap settings.
            
            ProfileHandler profileHandler = null;
            if (profile != null) profileHandler = routerDb.GetProfileHandler(profile);
            
            var offset = 100;
            while (offset < maxOffsetInMeter)
            {
                // calculate search box.
                var offsets = location.OffsetWithDistances(maxOffsetInMeter);
                var latitudeOffset = System.Math.Abs(location.latitude - offsets.latitude);
                var longitudeOffset = System.Math.Abs(location.longitude - offsets.longitude);
                var box = ((location.longitude - longitudeOffset, location.latitude + latitudeOffset), 
                    (location.longitude + longitudeOffset, location.latitude - latitudeOffset));

                // make sure data is loaded.
                routerDb.UsageNotifier?.NotifyBox(box);
                
                // snap to closest edge.
                var snapPoint = routerDb.SnapInBox(box, (eEnum) =>
                {
                    if (profileHandler == null) return true;

                    profileHandler.MoveTo(eEnum);
                    var canStop = profileHandler.CanStop;

                    return canStop;
                });
                if (snapPoint.EdgeId != EdgeId.Empty) return snapPoint;

                offset *= 2;
            }
            return new Result<SnapPoint>($"Could not snap to location: {location.longitude},{location.latitude}");
        }

        /// <summary>
        /// Snaps to the given vertex.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="vertexId">The vertex to snap to.</param>
        /// <param name="edgeId">The edge to prefer if any.</param>
        /// <returns>The result if any. Snapping will fail if a vertex has no edges.</returns>
        public static Result<SnapPoint> Snap(this RouterDb routerDb, VertexId vertexId, EdgeId? edgeId = null)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            if (!enumerator.MoveTo(vertexId)) return new Result<SnapPoint>($"Vertex {vertexId} not found.");

            while (enumerator.MoveNext())
            {
                if (edgeId != null &&
                    enumerator.Id != edgeId.Value) continue;

                if (enumerator.Forward)
                {
                    return new Result<SnapPoint>(new SnapPoint(enumerator.Id, 0));
                }
                return new Result<SnapPoint>(new SnapPoint(enumerator.Id, ushort.MaxValue));
            }

            if (edgeId.HasValue)
            {
                return new Result<SnapPoint>($"Edge {edgeId.Value} not found for vertex {vertexId}");
            }
            return new Result<SnapPoint>("Cannot snap to a vertex that has no edges.");
        }
        
        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The location on the network.</returns>
        public static (double longitude, double latitude) LocationOnNetwork(this RouterDb routerDb, SnapPoint snapPoint)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveToEdge(snapPoint.EdgeId);

            return enumerator.LocationOnEdge(snapPoint.Offset);
        }
    }
}