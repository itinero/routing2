using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="profile">The profile.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RouterDb routerDb, (double longitude, double latitude) location,
            Profile profile)
        {
            return routerDb.Snap(location, new SnapPointSettings()
            {
                Profiles = new []{profile}
            });
        }

        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="location">The location.</param>
        /// <param name="settings">The snap point settings.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RouterDb routerDb, (double longitude, double latitude) location,
            SnapPointSettings settings = null)
        {
            settings ??= new SnapPointSettings();

            var acceptableFunc = settings.AcceptableFunc(routerDb);

            // calculate search box.
            var box = location.BoxAround(settings.MaxOffsetInMeter);

            // make sure data is loaded.
            routerDb.UsageNotifier?.NotifyBox(box);

            // snap to closest edge.
            var snapPoint = routerDb.SnapInBox(box, acceptableFunc);
            if (snapPoint.EdgeId != EdgeId.Empty) return snapPoint;

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
        /// Snaps to all edge within a given offset around the given location.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="location">The location.</param>
        /// <param name="settings">The snap point settings.</param>
        /// <returns>The snap points.</returns>
        public static Result<IEnumerable<SnapPoint>> SnapAll(this RouterDb routerDb,
            (double longitude, double latitude) location, SnapPointSettings settings = null)
        {
            settings ??= new SnapPointSettings();
            settings.Profiles ??= new Profile[0];


            // calculate search box.
            var box = location.BoxAround(settings.MaxOffsetInMeter);

            // make sure data is loaded.
            routerDb.UsageNotifier?.NotifyBox(box);

            // snap to closest edge.
            return new Result<IEnumerable<SnapPoint>>(routerDb.SnapAllInBox(box, settings.AcceptableFunc(routerDb)));
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The location on the network.</returns>
        public static (double longitude, double latitude) LocationOnNetwork(this SnapPoint snapPoint, RouterDb routerDb)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveToEdge(snapPoint.EdgeId);

            return enumerator.LocationOnEdge(snapPoint.Offset);
        }
    }
}