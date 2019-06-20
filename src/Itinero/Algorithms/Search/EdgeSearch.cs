using System;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using System.Linq;
using Itinero.Data;
using Itinero.LocalGeo;

namespace Itinero.Algorithms.Search
{
    public static class EdgeSearch
    {
        /// <summary>
        /// Enumerates all edges that have at least one vertex in the given bounding box.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the vertices and their location.</returns>
        public static EdgeEnumerator SearchEdgesInBox(this Graph graph, (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var vertices = graph.SearchVerticesInBox(box);
            return new EdgeEnumerator(graph, vertices.Select((i) => i.vertex));
        }

        /// <summary>
        /// Returns the closest edge to the center of the given box that has at least one vertex inside the given box.
        /// </summary>
        /// <param name="network">The network.</param>
        /// <param name="box">The box.</param>
        /// <returns>The closest edge to the center of the box inside the given box.</returns>
        public static SnapPoint SnapInBox(this Network network,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var edges = network.Graph.SearchEdgesInBox(box);
            var center = new Coordinate((box.maxLon + box.minLon) / 2,(box.maxLat + box.minLat) / 2);

            var bestDistance = double.MaxValue;
            const double vertexTolerance = 1;
            (uint edgeId, ushort offset) bestSnapPoint = (uint.MaxValue, ushort.MaxValue);
            while (edges.MoveNext())
            {
                var edgeId = edges.GraphEnumerator.Id;

                // get edge details.
                var from = edges.GraphEnumerator.From;
                var to = edges.GraphEnumerator.To;
                var shape = network.GetShape(edges.GraphEnumerator.Id);
                if (!edges.GraphEnumerator.Forward)
                {
                    var t = from;
                    from = to;
                    to = t;
                    shape = shape?.Reverse();
                }
                
                // search for closest point along edge.
                (uint edgeId, double offset) localSnapPoint = (uint.MaxValue, double.MaxValue);
                
                // try vertex1.
                var vertex1 = network.GetVertex(from);
                var distance = Coordinate.DistanceEstimateInMeter(vertex1, center);
                if (distance < bestDistance)
                {
                    if (distance < vertexTolerance) distance = 0;
                    bestDistance = distance;
                    localSnapPoint = (edgeId, 0);
                }

                // try the shape points and it's segments.
                var length = 0.0;
                Coordinate? projected = null;
                Line line;
                var segmentLength = 0.0;
                var previous = vertex1;
                if (shape != null)
                {
                    if (!edges.GraphEnumerator.Forward) shape.Reverse();
                    foreach (var shapePoint in shape)
                    {
                        segmentLength = Coordinate.DistanceEstimateInMeter(previous, shapePoint);
                        
                        // check the segment.
                        line = new Line(previous, shapePoint);
                        projected = line.ProjectOn(center);
                        if (projected.HasValue)
                        {
                            distance = Coordinate.DistanceEstimateInMeter(projected.Value, center);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                localSnapPoint = (edgeId, length + Coordinate.DistanceEstimateInMeter(previous, projected.Value));
                            }
                        }
                        
                        // add the segment length.
                        length += segmentLength;
                        
                        // check shape point itself.
                        distance = Coordinate.DistanceEstimateInMeter(shapePoint, center);
                        if (!(distance < bestDistance)) continue;
                        
                        bestDistance = distance;
                        localSnapPoint = (edgeId, length);
                    }
                }
                
                var vertex2 = network.GetVertex(to);
                segmentLength = Coordinate.DistanceEstimateInMeter(previous, vertex2);
                
                // check the last segment.
                line = new Line(previous, vertex2);
                projected = line.ProjectOn(center);
                if (projected.HasValue)
                {
                    distance = Coordinate.DistanceEstimateInMeter(projected.Value, center);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        localSnapPoint = (edgeId, length + Coordinate.DistanceEstimateInMeter(previous, projected.Value));
                    }
                }
                        
                // add the segment length.
                length += segmentLength;

                // check the last vertex.
                distance = Coordinate.DistanceEstimateInMeter(vertex2, center);
                if (distance < bestDistance)
                {
                    if (distance < vertexTolerance) distance = 0;
                    bestDistance = distance;
                    localSnapPoint = (edgeId, length);
                }
                
                if (localSnapPoint.edgeId == uint.MaxValue) continue; // did not find a better snap point.
                
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
                }

                bestSnapPoint = (localSnapPoint.edgeId, offset);
            }

            return new SnapPoint(bestSnapPoint.edgeId, bestSnapPoint.offset);
        }
    }
}