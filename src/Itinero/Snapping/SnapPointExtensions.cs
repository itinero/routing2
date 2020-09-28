using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Geo.Directions;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    /// <summary>
    /// Extension methods related to snapping and snap points.
    /// </summary>
    public static class SnapPointExtensions
    {
        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routingNetwork">The routing network.</param>
        /// <param name="location">The location.</param>
        /// <param name="profile">The profile.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RoutingNetwork routingNetwork, (double longitude, double latitude) location,
            Profile profile)
        {
            
        }

        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routingNetwork">The routing network.</param>
        /// <param name="location">The location.</param>
        /// <param name="settings">The snap point settings.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RoutingNetwork routingNetwork, (double longitude, double latitude) location,
            SnapPointSettings? settings = null)
        {
            settings ??= new SnapPointSettings();

            var acceptableFunc = settings.AcceptableFunc(routingNetwork);

            // calculate search box.
            var box = location.BoxAround(settings.MaxOffsetInMeter);

            // make sure data is loaded.
            routingNetwork.RouterDb.UsageNotifier?.NotifyBox(routingNetwork, box);

            // snap to closest edge.
            var snapPoint = routingNetwork.SnapInBox(box, acceptableFunc);
            if (snapPoint.EdgeId != EdgeId.Empty) return snapPoint;

            return new Result<SnapPoint>($"Could not snap to location: {location.longitude},{location.latitude}");
        }

        /// <summary>
        /// Snaps to the given vertex.
        /// </summary>
        /// <param name="routingNetwork">The routing network.</param>
        /// <param name="vertexId">The vertex to snap to.</param>
        /// <param name="edgeId">The edge to prefer if any.</param>
        /// <returns>The result if any. Snapping will fail if a vertex has no edges.</returns>
        public static Result<SnapPoint> Snap(this RoutingNetwork routingNetwork, VertexId vertexId, EdgeId? edgeId = null)
        {
            var enumerator = routingNetwork.GetEdgeEnumerator();
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
        /// <param name="routingNetwork">The network.</param>
        /// <param name="location">The location.</param>
        /// <param name="settings">The snap point settings.</param>
        /// <returns>The snap points.</returns>
        public static Result<IEnumerable<SnapPoint>> SnapAll(this RoutingNetwork routingNetwork,
            (double longitude, double latitude) location, SnapPointSettings? settings = null)
        {
            settings ??= new SnapPointSettings();
            settings.Profiles ??= new Profile[0];

            // calculate search box.
            var box = location.BoxAround(settings.MaxOffsetInMeter);

            // make sure data is loaded.
            routingNetwork.RouterDb.UsageNotifier?.NotifyBox(routingNetwork, box);

            // snap to closest edge.
            return new Result<IEnumerable<SnapPoint>>(routingNetwork.SnapAllInBox(box, settings.AcceptableFunc(routingNetwork)));
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The location on the network.</returns>
        public static (double longitude, double latitude) LocationOnNetwork(this SnapPoint snapPoint, RoutingNetwork routerDb)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveToEdge(snapPoint.EdgeId);

            return enumerator.LocationOnEdge(snapPoint.Offset);
        }

        /// <summary>
        /// Returns the edge direction that best aligns with the direction based on the degrees with the meridian clockwise. 
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="distance">The distance to average the edge angle over in the range of ]0,∞[.</param>
        /// <param name="tolerance">The tolerance in range of ]0,90], default 90, determining what is considered forward or backward.
        /// If the difference in angle is too big null is returned.</param>
        /// <returns>The direction on the edge at the location of the snap point that best matches the given direction.
        /// Returns null if the difference is too big relative to the tolerance or the edge is too small to properly calculate an angle.</returns>
        public static bool? Direction(this SnapPoint snapPoint, RoutingNetwork routerDb, DirectionEnum direction,
            double distance = 10, double tolerance = 90)
        {
            return snapPoint.DirectionFromAngle(routerDb, (double) direction, out _, distance, tolerance);
        }

        /// <summary>
        /// Returns the edge direction that best aligns with the direction based on the degrees with the meridian clockwise. 
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="difference">The difference in degrees in the range of ]-180,180].</param>
        /// <param name="distance">The distance to average the edge angle over in the range of ]0,∞[.</param>
        /// <param name="tolerance">The tolerance in range of ]0,90], default 90, determining what is considered forward or backward.
        /// If the difference in angle is too big null is returned.</param>
        /// <returns>The direction on the edge at the location of the snap point that best matches the given direction.
        /// Returns null if the difference is too big relative to the tolerance or the edge is too small to properly calculate an angle.</returns>
        public static bool? Direction(this SnapPoint snapPoint, RoutingNetwork routerDb, DirectionEnum direction,
            out double difference, double distance = 10, double tolerance = 90)
        {
            return snapPoint.DirectionFromAngle(routerDb, (double) direction, out difference, distance, tolerance);
        }

        /// <summary>
        /// Returns the edge direction that best aligns with the direction based on the degrees with the meridian clockwise. 
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="degreesMeridian">The angle in degrees with the meridian clockwise.</param>
        /// <param name="difference">The difference in degrees in the range of ]-180,180].</param>
        /// <param name="distance">The distance to average the edge angle over in the range of ]0,∞[.</param>
        /// <param name="tolerance">The tolerance in range of ]0,90], default 90, determining what is considered forward or backward.
        /// If the difference in angle is too big null is returned.</param>
        /// <returns>The direction on the edge at the location of the snap point that best matches the given direction.
        /// Returns null if the difference is too big relative to the tolerance or the edge is too small to properly calculate an angle.</returns>
        public static bool? DirectionFromAngle(this SnapPoint snapPoint, RoutingNetwork routerDb, double degreesMeridian, out double difference, double distance = 10,
            double tolerance = 90)
        {
            if (tolerance <= 0 || tolerance > 90) throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance has to be in range of ]0,90]");
            
            var angle = snapPoint.Angle(routerDb, distance);
            if (!angle.HasValue)
            {
                difference = 0;
                return null;
            }
            difference = degreesMeridian - angle.Value;
            if (difference > 180) difference -= 360;
            
            if ((difference >= 0 && difference <= tolerance) || 
                (difference < 0 && difference >= -tolerance))
            {
                // forward, according to the tolerance.
                return true;
            }

            var reverseTolerance = 180 - tolerance;
            if ((difference >= 0 && difference >= reverseTolerance) ||
                (difference < 0 && difference <= reverseTolerance))
            {
                // backward, according to the tolerance.
                return false;
            }
            
            return null;
        }
        
        /// <summary>
        /// Returns the angle in degrees at the given snap point over a given distance. 
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <param name="routerDb">The router db.</param>
        /// <param name="distance">The distance to average the edge angle over in the range of ]0,∞[.</param>
        /// <returns>The angle in degrees with the meridian clockwise.</returns>
        public static double? Angle(this SnapPoint snapPoint, RoutingNetwork routerDb, double distance = 10)
        {
            if (distance <= 0) throw new ArgumentOutOfRangeException(nameof(distance), "The distance has to be in the range ]0,∞[");
            
            var edgeEnumerator = routerDb.GetEdgeEnumerator();
            if (!edgeEnumerator.MoveToEdge(snapPoint.EdgeId, true)) throw new ArgumentException($"Cannot find edge in {nameof(SnapPoint)}: {snapPoint}");

            // determine the first and last point on the edge
            // to calculate the angle for.
            var edgeLength = edgeEnumerator.EdgeLength();
            var distanceOffset = (distance / edgeLength) * ushort.MaxValue;
            var offset1 = (ushort)0;
            var offset2 = ushort.MaxValue;
            if (distanceOffset <= ushort.MaxValue)
            { 
                // not the entire edge.
                // round offsets to beginning/end of edge.
                offset1 = (ushort)System.Math.Max(0,
                    snapPoint.Offset - distanceOffset);
                offset2 = (ushort)System.Math.Min(ushort.MaxValue,
                    snapPoint.Offset + distanceOffset);

                // if both are at the same location make sure to at least
                // convert the smallest possible section of the edge.
                if (offset2 - offset1 == 0)
                {
                    if (offset1 > 0)
                    {
                        offset1--;
                    }
                    if (offset2 < ushort.MaxValue)
                    {
                        offset2++;
                    }
                }
            }

            // calculate the locations.
            var location1 = edgeEnumerator.LocationOnEdge(offset1);
            var location2 = edgeEnumerator.LocationOnEdge(offset2);

            if (location1.DistanceEstimateInMeter(location2) < .1)
            { // distance too small, edge to short.
                return null;
            }

            // calculate and return angle.
            var toNorth =  (location1.longitude, location1.latitude + 0.001f);
            var angleRadians = DirectionCalculator.Angle(location2, location1,  toNorth);
            return angleRadians.ToDegrees().NormalizeDegrees();
        }
    }
}