using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Algorithms;
using Itinero.Algorithms.DataStructures;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Coders;
using Itinero.Data.Shapes;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Itinero.Profiles.Handlers;

namespace Itinero
{
    /// <summary>
    /// Extensions related to the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="maxOffsetInMeter">The maximum offset in meter.</param>
        /// <param name="profile">The profile to snap for.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> Snap(this RouterDb routerDb, double longitude, double latitude, float maxOffsetInMeter = 1000,
            Profile profile = null)
        {
            ProfileHandler profileHandler = null;
            if (profile != null) profileHandler = routerDb.GetProfileHandler(profile);
            
            var offset = 100;
            while (offset < maxOffsetInMeter)
            {
                // calculate search box.
                var offsets = (new Coordinate(longitude, latitude)).OffsetWithDistances(maxOffsetInMeter);
                var latitudeOffset = System.Math.Abs(latitude - offsets.Latitude);
                var longitudeOffset = System.Math.Abs(longitude - offsets.Longitude);
                var box = (longitude - longitudeOffset, latitude - latitudeOffset, longitude + longitudeOffset,
                    latitude + latitudeOffset);

                // make sure data is loaded.
                routerDb.DataProvider?.TouchBox(box);

                // snap to closest edge.
                var snapPoint = routerDb.SnapInBox(box, (eEnum) =>
                {
                    if (profileHandler == null) return true;

                    profileHandler.MoveTo(eEnum);
                    var canStop = profileHandler.CanStop;

                    return canStop;
                });
                if (snapPoint.EdgeId != uint.MaxValue) return snapPoint;

                offset *= 2;
            }
            return new Result<SnapPoint>($"Could not snap to location: {longitude},{latitude}");
        }

        /// <summary>
        /// Returns the part of the edge between the two offsets not including the vertices at the start or the end.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset1">The start offset.</param>
        /// <param name="offset2">The end offset.</param>
        /// <param name="includeVertices">Include vertices in case the range start at min offset or ends at max.</param>
        /// <returns>The shape points between the given offsets. Includes the vertices by default when offsets at min/max.</returns>
        public static IEnumerable<Coordinate> GetShapeBetween(this RouterDbEdgeEnumerator enumerator,
            ushort offset1 = 0, ushort offset2 = ushort.MaxValue, bool includeVertices = true)
        {
            if (offset1 > offset2) throw new ArgumentException($"{nameof(offset1)} has to smaller than or equal to {nameof(offset2)}");

            // get edge and shape details.
            var shape = enumerator.GetShape();
            if (shape == null)
            {
                shape = new ShapeEnumerable(Enumerable.Empty<Coordinate>());
            }
            
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
                Coordinate next;
                if (i < shape.Count)
                { // the 
                    next = shape[i];
                }
                else
                { // the last location.
                    next = enumerator.ToLocation();
                }

                var segmentLength = Coordinate.DistanceEstimateInMeter(previous, next);
                if (before)
                { // check if offset1 length has exceeded.
                    if (segmentLength + length >= offset1Length && 
                        offset1 > 0)
                    { // we are before, but not we have move to after.
                        var segmentOffset = offset1Length - length;
                        var location = Coordinate.PositionAlongLine(previous, next, (segmentOffset / segmentLength));
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
                        var location = Coordinate.PositionAlongLine(previous, next, (segmentOffset / segmentLength));
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
        internal static uint EdgeLength(this RouterDbEdgeEnumerator enumerator)
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
                distance += Coordinate.DistanceEstimateInMeter(previous, current);
                previous = current;
            }

            return (uint)(distance * 100);
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The location on the network.</returns>
        public static Coordinate LocationOnNetwork(this RouterDb routerDb, SnapPoint snapPoint)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveToEdge(snapPoint.EdgeId);

            return enumerator.LocationOnEdge(snapPoint.Offset);
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The location on the network.</returns>
        public static Coordinate LocationOnEdge(this RouterDbEdgeEnumerator enumerator, in ushort offset)
        {
            // TODO: this can be optimized, build a performance test.
            var shape = enumerator.GetShapeBetween().ToList();
            var length = enumerator.EdgeLength();
            var currentLength = 0.0;
            var targetLength = length * (offset / (double)ushort.MaxValue);
            for (var i = 1; i < shape.Count; i++)
            {
                var segmentLength = Coordinate.DistanceEstimateInMeter(shape[i - 1], shape[i]);
                if (segmentLength + currentLength > targetLength)
                {
                    var segmentOffsetLength = segmentLength + currentLength - targetLength;
                    var segmentOffset = 1 - (segmentOffsetLength / segmentLength);
                    short? elevation = null;
                    if (shape[i - 1].Elevation.HasValue && 
                        shape[i].Elevation.HasValue)
                    {
                        elevation = (short)(shape[i - 1].Elevation.Value + (segmentOffset * (shape[i].Elevation.Value - shape[i - 1].Elevation.Value)));
                    }
                    return new Coordinate()
                    {
                        Latitude = (shape[i - 1].Latitude + (segmentOffset * (shape[i].Latitude - shape[i - 1].Latitude))),
                        Longitude = (shape[i - 1].Longitude + (segmentOffset * (shape[i].Longitude - shape[i - 1].Longitude))),
                        Elevation = elevation
                    };
                }
                currentLength += segmentLength;
            }
            return shape[shape.Count - 1];
        }

        /// <summary>
        /// Calculates a route between two snap points.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="settings">The routing settings.</param>
        /// <param name="source">The source location.</param>
        /// <param name="target">The target location.</param>
        /// <returns>A route.</returns>
        public static Result<Route> Calculate(this RouterDb routerDb, RoutingSettings settings, SnapPoint source, SnapPoint target)
        {
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = routerDb.LocationOnNetwork(source);
            var maxDistance = settings.MaxDistance;
            Box? maxBox = null;
            if (settings.MaxDistance < double.MaxValue)
            {
                maxBox = sourceLocation.BoxAround(settings.MaxDistance);
            }
            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;
                    
                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex.Longitude, vertex.Latitude))
                {
                    return true;
                }
                return false;
            }
            
            // run dijkstra.
            var path = Dijkstra.Default.Run(routerDb, source, target,
                profileHandler.GetForwardWeight, (v) =>
                {
                    routerDb.DataProvider?.TouchVertex(v);
                    return checkMaxDistance(v);
                });

            if (path == null) return new Result<Route>($"Route not found!");
            return RouteBuilder.Default.Build(routerDb, profile, path);
        }

        /// <summary>
        /// Calculates a route between one snap point and multiple targets.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="settings">The routing settings.</param>
        /// <param name="source">The source location.</param>
        /// <param name="targets">The target locations.</param>
        /// <returns>The routes.</returns>
        public static Result<Route>[] Calculate(this RouterDb routerDb, RoutingSettings settings, SnapPoint source, SnapPoint[] targets)
        {
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = routerDb.LocationOnNetwork(source);
            var maxDistance = settings.MaxDistance;
            Box? maxBox = null;
            if (settings.MaxDistance < double.MaxValue)
            {
                maxBox = sourceLocation.BoxAround(settings.MaxDistance);
            }
            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;
                    
                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex.Longitude, vertex.Latitude))
                {
                    return true;
                }
                return false;
            }
            
            var paths = Dijkstra.Default.Run(routerDb, source, targets,
                profileHandler.GetForwardWeight, (v) =>
                {
                    routerDb.DataProvider?.TouchVertex(v);
                    return checkMaxDistance(v);
                });

            var results = new Result<Route>[paths.Length];
            for (var r = 0; r < results.Length; r++)
            {
                var path = paths[r];
                if (path == null)
                {
                    results[r] = new Result<Route>($"Routes not found!");
                }
                else
                {
                    results[r] = RouteBuilder.Default.Build(routerDb, profile, path);
                }
            }
            return results;
        }

        /// <summary>
        /// Calculates a route between multiple snap point and one target.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="settings">The routing settings.</param>
        /// <param name="sources">The source locations.</param>
        /// <param name="target">The target location.</param>
        /// <returns>The routes.</returns>
        public static Result<Route>[] Calculate(this RouterDb routerDb, RoutingSettings settings, SnapPoint[] sources, SnapPoint target)
        {
            var profile = settings.Profile;
            var profileHandler = routerDb.GetProfileHandler(profile);

            // if there is max distance don't search outside the box.
            var sourceLocation = routerDb.LocationOnNetwork(target);
            var maxDistance = settings.MaxDistance;
            Box? maxBox = null;
            if (settings.MaxDistance < double.MaxValue)
            {
                maxBox = sourceLocation.BoxAround(settings.MaxDistance);
            }
            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) return false;
                    
                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex.Longitude, vertex.Latitude))
                {
                    return true;
                }

                return false;
            }
            
            var paths = Dijkstra.Default.Run(routerDb, target, sources,
                profileHandler.GetBackwardWeight, (v) =>
                {
                    routerDb.DataProvider?.TouchVertex(v);
                    return checkMaxDistance(v);
                });

            var results = new Result<Route>[paths.Length];
            for (var r = 0; r < results.Length; r++)
            {
                var path = paths[r];
                if (path == null)
                {
                    results[r] = new Result<Route>($"Routes not found!");
                }
                else
                {
                    results[r] = RouteBuilder.Default.Build(routerDb, profile, path);
                }
            }
            return results;
        }

        internal static ProfileHandler GetProfileHandler(this RouterDb routerDb, Profile profile)
        {
            if (routerDb.EdgeDataLayout.TryGet($"{profile.Name}.weight", out var layout))
            {
                // the weight is encoded on the edges.
                if (layout.dataType == EdgeDataType.UInt32)
                {
                    return new ProfileInlineUInt32WeightHandlerDefault(profile,
                        new EdgeDataCoderUInt32(layout.offset));
                }
            }

            return new ProfileHandlerDefault(profile);
        }
    }
}