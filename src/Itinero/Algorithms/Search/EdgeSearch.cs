using System;
using Itinero.Data.Graphs;
using System.Linq;
using Itinero.Geo;

namespace Itinero.Algorithms.Search
{
    public static class EdgeSearch
    {
        /// <summary>
        /// Enumerates all edges that have at least one vertex in the given bounding box.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the vertices and their location.</returns>
        public static RouterDbEdgeEnumerator SearchEdgesInBox(this RouterDb routerDb, (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var vertices = routerDb.Network.SearchVerticesInBox(box);
            return new RouterDbEdgeEnumerator(routerDb, new EdgeEnumerator(routerDb.Network, vertices.Select((i) => i.vertex)));
        }

        /// <summary>
        /// Returns the closest edge to the center of the given box that has at least one vertex inside the given box.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="box">The box.</param>
        /// <param name="acceptableFunc">The function to determine if an edge is acceptable or not. If null any edge will be accepted.</param>
        /// <returns>The closest edge to the center of the box inside the given box.</returns>
        public static SnapPoint SnapInBox(this RouterDb routerDb,
            (double minLon, double minLat, double maxLon, double maxLat) box, 
            Func<RouterDbEdgeEnumerator, bool> acceptableFunc = null)
        {
            bool CheckAcceptable(bool? isAcceptable, RouterDbEdgeEnumerator eEnum)
            {
                if (isAcceptable.HasValue) return isAcceptable.Value;
                
                if (acceptableFunc != null &&
                    !acceptableFunc.Invoke(eEnum))
                { // edge cannot be used.
                    return false;
                }

                return true;
            }
            
            var edgeEnumerator = routerDb.SearchEdgesInBox(box);
            var center = ((box.maxLon + box.minLon) / 2,(box.maxLat + box.minLat) / 2);

            const double exactTolerance = 1;
            var bestDistance = double.MaxValue;
            (EdgeId edgeId, ushort offset) bestSnapPoint = (EdgeId.Empty, ushort.MaxValue);
            while (edgeEnumerator.MoveNext())
            {
                if (bestDistance <= 0) break; // break when exact on an edge.
                
                // search for the local snap point that improves the current best snap point.
                (EdgeId edgeId, double offset) localSnapPoint = (EdgeId.Empty, 0); 
                var isAcceptable = (bool?) null;
                var completeShape = edgeEnumerator.GetCompleteShape();
                var length = 0.0;
                using (var completeShapeEnumerator = completeShape.GetEnumerator())
                {
                    completeShapeEnumerator.MoveNext();
                    var previous = completeShapeEnumerator.Current;
                    
                    // start with the first location.
                    var distance = previous.DistanceEstimateInMeter(center);
                    if (distance < bestDistance)
                    {
                        isAcceptable = CheckAcceptable(isAcceptable, edgeEnumerator);
                        if (isAcceptable.HasValue && !isAcceptable.Value) continue;
                        
                        if (distance < exactTolerance) distance = 0;
                        bestDistance = distance;
                        localSnapPoint = (edgeEnumerator.Id, 0);
                    }
                    
                    // loop over all pairs.
                    while (completeShapeEnumerator.MoveNext())
                    {
                        var current = completeShapeEnumerator.Current;
                        
                        var segmentLength = previous.DistanceEstimateInMeter(current);
                        
                        // first check the actual current location, it may be an exact match.
                        distance = current.DistanceEstimateInMeter(center);
                        if (distance < bestDistance)
                        {
                            isAcceptable = CheckAcceptable(isAcceptable, edgeEnumerator);
                            if (isAcceptable.HasValue && !isAcceptable.Value) break;
                        
                            if (distance < exactTolerance) distance = 0;
                            bestDistance = distance;
                            localSnapPoint = (edgeEnumerator.Id, length + segmentLength);
                        }
                        
                        // update length.
                        var startLength = length;
                        length += segmentLength;
                        
                        // check if we even need to check.
                        var previousDistance = previous.DistanceEstimateInMeter(center);
                        var shapePointDistance = current.DistanceEstimateInMeter(center);
                        if (previousDistance + segmentLength > bestDistance &&
                            shapePointDistance + segmentLength > bestDistance)
                        {
                            continue;
                        }
                        
                        // project on line segment.
                        if (bestDistance <= 0) continue;
                        var line = (previous, current);
                        var projected = line.ProjectOn(center);
                        if (!projected.HasValue) continue;
                        
                        distance = projected.Value.DistanceEstimateInMeter(center);
                        if (!(distance < bestDistance)) continue;
                        
                        isAcceptable = CheckAcceptable(isAcceptable, edgeEnumerator);
                        if (isAcceptable.HasValue && !isAcceptable.Value) break;
                                
                        if (distance < exactTolerance) distance = 0;
                        bestDistance = distance;
                        localSnapPoint = (edgeEnumerator.Id, startLength + previous.DistanceEstimateInMeter(projected.Value));
                    }
                }

                // move to the nex edge if no better point was found.
                if (localSnapPoint.edgeId == EdgeId.Empty) continue;
                
                // calculate the actual offset.
                var offset = ushort.MaxValue;
                if (localSnapPoint.offset < length)
                {
                    if (localSnapPoint.offset <= 0)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = (ushort) ((localSnapPoint.offset / length) * ushort.MaxValue);
                    }
                
                    // invert offset if edge is reversed.
                    if (!edgeEnumerator.Forward)
                    {
                        offset = (ushort)(ushort.MaxValue - offset);
                    }
                }

                bestSnapPoint = (localSnapPoint.edgeId, offset);
            }

            return new SnapPoint(bestSnapPoint.edgeId, bestSnapPoint.offset);
        }
    }
}