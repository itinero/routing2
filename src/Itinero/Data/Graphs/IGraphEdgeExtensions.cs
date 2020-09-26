using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs.Reading;
using Itinero.Geo;

namespace Itinero.Data.Graphs
{
    public static class IGraphEdgeExtensions
    {
        /// <summary>
        /// Gets the complete shape, including start end end vertices.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The complete shape.</returns>
        public static IEnumerable<(double longitude, double latitude)> GetCompleteShape<T,S>(this T enumerator)
            where T : IGraphEdge<S>
            where S : IGraphEdgeEnumerable
        {
            yield return enumerator.FromLocation;
            
            var shape = enumerator.Shape;
            foreach (var s in shape)
            {
                yield return s;
            }

            yield return enumerator.ToLocation;
        }

        /// <summary>
        /// Returns the part of the edge between the two offsets not including the vertices at the start or the end.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset1">The start offset.</param>
        /// <param name="offset2">The end offset.</param>
        /// <param name="includeVertices">Include vertices in case the range start at min offset or ends at max.</param>
        /// <returns>The shape points between the given offsets. Includes the vertices by default when offsets at min/max.</returns>
        internal static IEnumerable<(double longitude, double latitude)> GetShapeBetween<T,S>(this T enumerator,
            ushort offset1 = 0, ushort offset2 = ushort.MaxValue, bool includeVertices = true)
            where T : IGraphEdge<S>
            where S : IGraphEdgeEnumerable
        {
            if (offset1 > offset2) throw new ArgumentException($"{nameof(offset1)} has to smaller than or equal to {nameof(offset2)}");

            // get edge and shape details.
            var shape = enumerator.Shape.ToList();
            
            // return the entire edge if requested.
            if (offset1 == 0 && offset2 == ushort.MaxValue)
            {
                foreach (var s in enumerator.GetCompleteShape<T, S>())
                {
                    yield return s;
                }
                yield break;
            }

            // calculate offsets in meters.
            var edgeLength = enumerator.EdgeLength<T,S>();
            var offset1Length = (offset1/(double)ushort.MaxValue) * edgeLength;
            var offset2Length = (offset2/(double)ushort.MaxValue) * edgeLength;

            // TODO: can we make this easier with the complete shape enumeration?
            // calculate coordinate shape.
            var before = offset1 > 0; // when there is a start offset.
            var length = 0.0;
            var previous = enumerator.FromLocation;
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
                    next = enumerator.ToLocation;
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
        internal static double EdgeLength<T,S>(this T enumerator)
            where T : IGraphEdge<S>
            where S : IGraphEdgeEnumerable
        {
            return enumerator.GetCompleteShape<T,S>().DistanceEstimateInMeter();
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The location on the network.</returns>
        internal static (double longitude, double latitude) LocationOnEdge<T,S>(this T enumerator, in ushort offset)
            where T : IGraphEdge<S>
            where S : IGraphEdgeEnumerable
        {
            // TODO: this can be optimized, build a performance test.
            var shape = enumerator.GetShapeBetween<T,S>().ToList();
            var length = enumerator.EdgeLength<T,S>();
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

        internal static IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo<T,S>(this T enumerator, 
            IEnumerable<(EdgeId edge, byte? turn)> previousEdges)
            where T : IGraphEdge<S>
            where S : IGraphEdgeEnumerable
        {
            using var previousEdgesEnumerator = previousEdges.GetEnumerator();
            if (!previousEdgesEnumerator.MoveNext()) return Enumerable.Empty<(uint turnCostType, uint cost)>();
            var fromOrder = previousEdgesEnumerator.Current.turn;
            if (fromOrder == null) return Enumerable.Empty<(uint turnCostType, uint cost)>();

            return enumerator.GetTurnCostTo(fromOrder.Value);
        }
    }
}