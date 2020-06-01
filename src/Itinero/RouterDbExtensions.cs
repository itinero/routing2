using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Geo;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;
using Itinero.Routers;

namespace Itinero
{
    /// <summary>
    /// Extensions related to the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="location">The location.</param>
        /// <returns>The ID of the new vertex.</returns>
        public static VertexId AddVertex(this RouterDb routerDb, (double longitude, double latitude) location)
        {
            return routerDb.AddVertex(location.longitude, location.latitude);
        }
        
        internal static IEnumerable<(string key, string value)> GetAttributes(this RouterDb routerDb, EdgeId edge)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            if (!enumerator.MoveToEdge(edge)) return Enumerable.Empty<(string key, string value)>();

            return enumerator.Attributes;
        }

        /// <summary>
        /// Returns the part of the edge between the two offsets not including the vertices at the start or the end.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset1">The start offset.</param>
        /// <param name="offset2">The end offset.</param>
        /// <param name="includeVertices">Include vertices in case the range start at min offset or ends at max.</param>
        /// <returns>The shape points between the given offsets. Includes the vertices by default when offsets at min/max.</returns>
        internal static IEnumerable<(double longitude, double latitude)> GetShapeBetween(this RouterDbEdgeEnumerator enumerator,
            ushort offset1 = 0, ushort offset2 = ushort.MaxValue, bool includeVertices = true)
        {
            if (offset1 > offset2) throw new ArgumentException($"{nameof(offset1)} has to smaller than or equal to {nameof(offset2)}");

            // get edge and shape details.
            var shape = enumerator.Shape.ToList();
            
            // return the entire edge if requested.
            if (offset1 == 0 && offset2 == ushort.MaxValue)
            {
                foreach (var s in enumerator.GetCompleteShape())
                {
                    yield return s;
                }
                yield break;
            }

            // calculate offsets in meters.
            var edgeLength = enumerator.EdgeLength();
            var offset1Length = (offset1/(double)ushort.MaxValue) * edgeLength;
            var offset2Length = (offset2/(double)ushort.MaxValue) * edgeLength;

            // TODO: can we make this easier with the complete shape enumeration?
            // calculate coordinate shape.
            var before = offset1 > 0; // when there is a start offset.
            var length = 0.0;
            var previous = enumerator.FromLocation();
            if (offset1 == 0 && includeVertices) yield return previous;
            for (var i = 0; i < shape.Count + 1; i++)
            {
                (double longitude, double latitude) next;
                if (i < shape.Count)
                { // the 
                    next = shape[i];
                }
                else
                { // the last location.
                    next = enumerator.ToLocation();
                }

                var segmentLength = previous.DistanceEstimateInMeter(next);
                if (before)
                { // check if offset1 length has exceeded.
                    if (segmentLength + length >= offset1Length && 
                        offset1 > 0)
                    { // we are before, but not we have move to after.
                        var segmentOffset = offset1Length - length;
                        var location = (previous, next).PositionAlongLine(segmentOffset / segmentLength);
                        previous = next;
                        before = false;
                        yield return location;
                    }
                }

                if (!before)
                { // check if offset2 length has exceeded.
                    if (segmentLength + length > offset2Length &&
                        offset2 < ushort.MaxValue)
                    { // we are after but now we are after.
                        var segmentOffset = offset2Length - length;
                        var location = (previous, next).PositionAlongLine(segmentOffset / segmentLength);
                        yield return location;
                        yield break;
                    }

                    // the case where include vertices is false.
                    if (i == shape.Count && !includeVertices) yield break;
                    yield return next;
                }

                // move to the next segment.
                previous = next;
                length += segmentLength;
            }
        }

        /// <summary>
        /// Gets the length of an edge in centimeters.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The length in meters.</returns>
        internal static double EdgeLength(this RouterDbEdgeEnumerator enumerator)
        {
            var distance = 0.0;

            // compose geometry.
            var shape = enumerator.GetCompleteShape();
            var shapeEnumerator = shape.GetEnumerator();
            shapeEnumerator.MoveNext();
            var previous = shapeEnumerator.Current;

            while (shapeEnumerator.MoveNext())
            {
                var current = shapeEnumerator.Current;
                distance += previous.DistanceEstimateInMeter(current);
                previous = current;
            }

            return distance;
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The location on the network.</returns>
        internal static (double longitude, double latitude) LocationOnEdge(this RouterDbEdgeEnumerator enumerator, in ushort offset)
        {
            // TODO: this can be optimized, build a performance test.
            var shape = enumerator.GetShapeBetween().ToList();
            var length = enumerator.EdgeLength();
            var currentLength = 0.0;
            var targetLength = length * (offset / (double)ushort.MaxValue);
            for (var i = 1; i < shape.Count; i++)
            {
                var segmentLength = shape[i - 1].DistanceEstimateInMeter(shape[i]);
                if (segmentLength + currentLength > targetLength)
                {
                    var segmentOffsetLength = segmentLength + currentLength - targetLength;
                    var segmentOffset = 1 - (segmentOffsetLength / segmentLength);
                    short? elevation = null;
//                    if (shape[i - 1].Elevation.HasValue && 
//                        shape[i].Elevation.HasValue)
//                    {
//                        elevation = (short)(shape[i - 1].Elevation.Value + (segmentOffset * (shape[i].Elevation.Value - shape[i - 1].Elevation.Value)));
//                    }
                    return (shape[i - 1].longitude + (segmentOffset * (shape[i].longitude - shape[i - 1].longitude)),
                        shape[i - 1].latitude + (segmentOffset * (shape[i].latitude - shape[i - 1].latitude)));
                }
                currentLength += segmentLength;
            }
            return shape[shape.Count - 1];
        }

        /// <summary>
        /// Configure a router with the given settings.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>A router.</returns>
        public static IRouter Route(this RouterDb routerDb, RoutingSettings settings)
        {
            return new Router(routerDb, settings);
        }

        /// <summary>
        /// Configure a router with the given profile.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <returns>A router.</returns>
        public static IRouter Route(this RouterDb routerDb, Profile profile)
        {
            return routerDb.Route(new RoutingSettings()
            {
                Profile = profile
            });
        }

        internal static ProfileHandler GetProfileHandler(this RouterDb routerDb, Profile profile)
        {
            return new ProfileHandlerDefault(profile);
        }
    }
}