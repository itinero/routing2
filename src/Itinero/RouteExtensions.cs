using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Algorithms;
using Itinero.Data;
using Itinero.Geo;

namespace Itinero
{
    /// <summary>
    /// Contains extensions for the route object.
    /// </summary>
    public static class RouteExtensions
    {
        /// <summary>
        /// Concatenates two routes.
        /// </summary>
        public static Route Concatenate(this Route route1, Route route2)
        {
            return route1.Concatenate(route2, true);
        }

        /// <summary>
        /// Concatenates two routes.
        /// </summary>
        public static Route Concatenate(this Route route1, Route route2, bool clone)
        {
            if (route1 == null) return route2;
            if (route2 == null) return route1;
            if (route1.Shape == null || route1.Shape.Count == 0) return route2;
            if (route2.Shape == null || route2.Shape.Count == 0) return route1;

            var timeoffset = route1.TotalTime;
            var distanceoffset = route1.TotalDistance;
            var shapeoffset = route1.Shape.Count - 1;

            // merge shape.
            var shapeLength = route1.Shape.Count + route2.Shape.Count - 1;
            var shape = new (double longitude, double latitude)[route1.Shape.Count + route2.Shape.Count - 1];
            route1.Shape.CopyTo(shape, 0);
            route2.Shape.CopyTo(shape, route1.Shape.Count - 1);

            // merge metas.
            var metas1 = route1.ShapeMeta?.Count ?? 0;
            var metas2 = route2.ShapeMeta?.Count ?? 0;
            Route.Meta[] metas = null;
            if (metas1 + metas2 - 1 > 0)
            {
                metas = new Route.Meta[metas1 + metas2 - 1];
                if (route1.ShapeMeta != null)
                {
                    for (var i = 0; i < route1.ShapeMeta.Count; i++)
                    {
                        metas[i] = new Route.Meta()
                        {
                            Attributes = new List<(string key, string value)>(route1.ShapeMeta[i].Attributes),
                            Shape = route1.ShapeMeta[i].Shape
                        };
                    }
                }
                if (route2.ShapeMeta != null)
                {
                    for (var i = 1; i < route2.ShapeMeta.Count; i++)
                    {
                        metas[metas1 + i - 1] = new Route.Meta()
                        {
                            Attributes = new List<(string key, string value)>(route2.ShapeMeta[i].Attributes),
                            Shape = route2.ShapeMeta[i].Shape + shapeoffset,
                            Distance = route2.ShapeMeta[i].Distance + distanceoffset,
                            Time = route2.ShapeMeta[i].Time + timeoffset
                        };
                    }
                }
            }

            // merge stops.
            var stops = new List<Route.Stop>();
            if (route1.Stops != null)
            {
                for (var i = 0; i < route1.Stops.Count; i++)
                {
                    var stop = route1.Stops[i];
                    stops.Add(new Route.Stop()
                    {
                        Attributes = new List<(string key, string value)>(stop.Attributes),
                            Coordinate = stop.Coordinate,
                            Shape = stop.Shape
                    });
                }
            }
            if (route2.Stops != null)
            {
                for (var i = 0; i < route2.Stops.Count; i++)
                {
                    var stop = route2.Stops[i];
                    if (i == 0 && stops.Count > 0)
                    { // compare with last stop to remove duplicates.
                        var existing = stops[stops.Count - 1];
                        if (existing.Shape == route1.Shape.Count - 1 &&
                            Math.Abs(existing.Coordinate.latitude - stop.Coordinate.latitude) < double.Epsilon &&
                            Math.Abs(existing.Coordinate.longitude - stop.Coordinate.longitude) < double.Epsilon &&
                            existing.Attributes.ContainsSame(stop.Attributes, "time", "distance"))
                        { // stop are identical, stop this one.
                            continue;
                        }
                    }
                    stops.Add(new Route.Stop()
                    {
                        Attributes = new List<(string key, string value)>(stop.Attributes),
                            Coordinate = stop.Coordinate,
                            Shape = stop.Shape + shapeoffset
                    });
                    stops[stops.Count - 1].Distance = stop.Distance + distanceoffset;
                    stops[stops.Count - 1].Time = stop.Time + timeoffset;
                }
            }

            // merge branches.
            var branches1 = route1.Branches?.Length ?? 0;
            var branches2 = route2.Branches?.Length ?? 0;
            var branches = new Route.Branch[branches1 + branches2];
            if (branches1 + branches2 > 0)
            {
                if (route1.Branches != null)
                {
                    for (var i = 0; i < route1.Branches.Length; i++)
                    {
                        var branch = route1.Branches[i];
                        branches[i] = new Route.Branch()
                        {
                            Attributes = new List<(string key, string value)>(branch.Attributes),
                            Coordinate = branch.Coordinate,
                            Shape = branch.Shape
                        };
                    }
                }
                if (route2.Branches != null)
                {
                    for (var i = 0; i < route2.Branches.Length; i++)
                    {
                        var branch = route2.Branches[i];
                        branches[branches1 + i] = new Route.Branch()
                        {
                            Attributes = new List<(string key, string value)>(branch.Attributes),
                            Coordinate = branch.Coordinate,
                            Shape = branch.Shape + shapeoffset
                        };
                    }
                }
            }

            // merge attributes.
            var attributes = new List<(string key, string value)>(route1.Attributes);
            attributes.AddOrReplace(route2.Attributes);
            var profile = route1.Profile;
            if (route2.Profile != profile)
            {
                attributes.RemoveKey("profile");
            }

            // update route.
            var route = new Route
            {
                Attributes = attributes,
                Branches = branches,
                Shape = shape.ToList(),
                ShapeMeta = metas.ToList(),
                Stops = stops,
                TotalDistance = route1.TotalDistance + route2.TotalDistance,
                TotalTime = route1.TotalTime + route2.TotalTime
            };
            return route;
        }

        /// <summary>
        /// Concatenates all the given routes or returns an error when one of the routes cannot be concatenated.
        /// </summary>
        /// <param name="routes"></param>
        /// <returns></returns>
        public static Result<Route> Concatenate(this IEnumerable<Result<Route>> routes)
        {
            Route route = null;
            var r = 0;
            foreach (var localRoute in routes)
            {
                if (localRoute.IsError)
                {
                    return new Result<Route>($"Route at index {r} is in error: {localRoute.ErrorMessage}");
                }
                route = route == null ? localRoute.Value : route.Concatenate(localRoute.Value);

                r++;
            }
            return new Result<Route>(route);
        }

//        /// <summary>
//        /// Calculates the position on the route after the given distance from the starting point.
//        /// </summary>
//        public static Coordinate? PositionAfter(this Route route, float distanceInMeter)
//        {
//            var distanceMeter = 0.0;
//            if (route.Shape == null)
//            {
//                return null;
//            }
//
//            for (var i = 0; i < route.Shape.Count - 1; i++)
//            {
//                var currentDistance = Coordinate.DistanceEstimateInMeter(route.Shape[i], route.Shape[i + 1]);
//                if (distanceMeter + currentDistance >= distanceInMeter)
//                {
//                    var segmentDistance = distanceInMeter - distanceMeter;
//                    var diffLat = route.Shape[i + 1].Latitude - route.Shape[i].Latitude;
//                    var diffLon = route.Shape[i + 1].Longitude - route.Shape[i].Longitude;
//                    var lat = route.Shape[i].Latitude + diffLat * (segmentDistance / currentDistance);
//                    var lon = route.Shape[i].Longitude + diffLon * (segmentDistance / currentDistance);
//                    if (!route.Shape[i].Elevation.HasValue || !route.Shape[i + 1].Elevation.HasValue)
//                        return new Coordinate(lat, lon);
//                    var s = route.Shape[i + 1].Elevation;
//                    if (s == null) return new Coordinate(lat, lon);
//                    var diffElev = s.Value - route.Shape[i].Elevation.Value;
//                    short? elevation = (short) (route.Shape[i].Elevation.Value + diffElev * (segmentDistance / currentDistance));
//                    return new Coordinate(lat, lon, elevation.Value);
//                }
//                distanceMeter += currentDistance;
//            }
//            return null;
//        }

        /// <summary>
        /// Distance and time a the given shape index.
        /// </summary>
        public static void DistanceAndTimeAt(this Route route, int shape, out double distance, out double time)
        {
            route.SegmentFor(shape, out var segmentStart, out var segmentEnd);

            if (shape == segmentStart)
            {
                if (shape == 0)
                {
                    distance = 0;
                    time = 0;
                    return;
                }
                else
                {
                    var shapeMeta = route.ShapeMetaFor(shape);
                    distance = shapeMeta.Distance;
                    time = shapeMeta.Time;
                    return;
                }
            }
            if (shape == segmentEnd)
            {
                if (shape == route.Shape.Count - 1)
                {
                    distance = route.TotalDistance;
                    time = route.TotalTime;
                    return;
                }
                else
                {
                    var shapeMeta = route.ShapeMetaFor(shape);
                    distance = shapeMeta.Distance;
                    time = shapeMeta.Time;
                    return;
                }
            }
            var startDistance = 0.0;
            var startTime = 0.0;
            if (segmentStart == 0)
            {
                startDistance = 0;
                startTime = 0;
            }
            else
            {
                var shapeMeta = route.ShapeMetaFor(segmentStart);
                startDistance = shapeMeta.Distance;
                startTime = shapeMeta.Time;
            }
            var endDistance = 0.0;
            var endTime = 0.0;
            if (segmentEnd == route.Shape.Count - 1)
            {
                endDistance = route.TotalDistance;
                endTime = route.TotalTime;
            }
            else
            {
                var shapeMeta = route.ShapeMetaFor(segmentEnd);
                endDistance = shapeMeta.Distance;
                endTime = shapeMeta.Time;
            }
            var distanceToShape = 0.0;
            var distanceOfSegment = 0.0;
            for (var i = segmentStart; i < segmentEnd; i++)
            {
                if (i == shape)
                {
                    distanceToShape = distanceOfSegment;
                }
                distanceOfSegment += (route.Shape[i].longitude, route.Shape[i].latitude).DistanceEstimateInMeter(
                    (route.Shape[i + 1].longitude, route.Shape[i + 1].latitude));
            }
            var ratio = distanceToShape / distanceOfSegment;
            distance = ((endDistance - startDistance) * ratio) + startDistance;
            time = ((endTime - startTime) * ratio) + startTime;
        }

        /// <summary>
        /// Gets the shape meta for the given shape index.
        /// </summary>
        public static Route.Meta ShapeMetaFor(this Route route, int shape)
        {
            foreach (var shapeMeta in route.ShapeMeta)
            {
                if (shapeMeta.Shape == shape)
                {
                    return shapeMeta;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches the segment the given shape index exists in.
        /// </summary>
        public static void SegmentFor(this Route route, int shape, out int segmentStart, out int segmentEnd)
        {
            segmentStart = 0;
            segmentEnd = route.Shape.Count - 1;
            if (route.ShapeMeta == null)
            {
                return;
            }

            for (var i = 0; i < route.ShapeMeta.Count; i++)
            {
                if (route.ShapeMeta[i].Shape <= shape)
                {
                    if (segmentStart <= route.ShapeMeta[i].Shape &&
                        route.ShapeMeta[i].Shape < route.Shape.Count - 1)
                    {
                        segmentStart = route.ShapeMeta[i].Shape;
                    }
                }
                else if (route.ShapeMeta[i].Shape > shape)
                {
                    segmentEnd = route.ShapeMeta[i].Shape;
                    break;
                }
            }
        }
        
//        /// <summary>
//        /// Calculates the closest point on the route relative to the given coordinate.
//        /// </summary>
//        /// <param name="route">The route.</param>
//        /// <param name="startShape">The shape to start at, relevant for routes with u-turns and navigation.</param>
//        /// <param name="coordinate">The coordinate to project.</param>
//        /// <param name="projected">The projected coordinate on the route.</param>
//        /// <param name="distanceFromStartInMeter">The distance in meter to the projected point from the start of the route.</param>
//        /// <param name="timeFromStartInSeconds">The time in seconds to the projected point from the start of the route.</param>
//        /// <param name="shape">The shape segment of the route the point was projected on to.</param>
//        /// <returns></returns>
//        public static bool ProjectOn(this Route route, int startShape, (double longitude, double latitude) coordinate, out (double longitude, double latitude) projected, out int shape,
//            out double distanceFromStartInMeter, out double timeFromStartInSeconds)
//        {
//            var distance = double.MaxValue;
//            distanceFromStartInMeter = 0;
//            timeFromStartInSeconds = 0;
//            projected = new Coordinate();
//            shape = -1;
//
//            if (route.Shape == null)
//            {
//                return false;
//            }
//
//            Coordinate currentProjected;
//            var currentDistanceFromStart = 0.0;
//            var currentDistance = 0.0;
//            for (var i = startShape; i < route.Shape.Count - 1; i++)
//            {
//                // project on shape and save distance and such.
//                var line = new Line(route.Shape[i], route.Shape[i + 1]);
//                var projectedPoint = line.ProjectOn(coordinate);
//                if (projectedPoint != null)
//                { // there was a projected point.
//                    currentProjected = new Coordinate(projectedPoint.Value.Latitude, projectedPoint.Value.Longitude);
//                    currentDistance = Coordinate.DistanceEstimateInMeter(coordinate, currentProjected);
//                    if (currentDistance < distance)
//                    { // this point is closer.
//                        projected = currentProjected;
//                        shape = i;
//                        distance = currentDistance;
//
//                        // calculate distance.
//                        var localDistance = Coordinate.DistanceEstimateInMeter(currentProjected, route.Shape[i]);
//                        distanceFromStartInMeter = currentDistanceFromStart + localDistance;
//                    }
//                }
//
//                // check first point.
//                currentProjected = route.Shape[i];
//                currentDistance = Coordinate.DistanceEstimateInMeter(coordinate, currentProjected);
//                if (currentDistance < distance)
//                { // this point is closer.
//                    projected = currentProjected;
//                    shape = i;
//                    distance = currentDistance;
//                    distanceFromStartInMeter = currentDistanceFromStart;
//                }
//
//                // update distance from start.
//                currentDistanceFromStart = currentDistanceFromStart + Coordinate.DistanceEstimateInMeter(route.Shape[i], route.Shape[i + 1]);
//            }
//
//            // check last point.
//            currentProjected = route.Shape[route.Shape.Count - 1];
//            currentDistance = Coordinate.DistanceEstimateInMeter(coordinate, currentProjected);
//            if (currentDistance < distance)
//            { // this point is closer.
//                projected = currentProjected;
//                shape = route.Shape.Count - 1;
//                distance = currentDistance;
//                distanceFromStartInMeter = currentDistanceFromStart;
//            }
//
//            // calculate time.
//            if (route.ShapeMeta == null) return true;
//            for (var metaIdx = 0; metaIdx < route.ShapeMeta.Count; metaIdx++)
//            {
//                var meta = route.ShapeMeta[metaIdx];
//                if (meta == null || meta.Shape < shape + 1) continue;
//                var segmentStartTime = 0.0;
//                if (metaIdx > 0 && route.ShapeMeta[metaIdx - 1] != null)
//                {
//                    segmentStartTime = route.ShapeMeta[metaIdx - 1].Time;
//                }
//
//                var segmentDistance = 0.0;
//                var segmentDistanceOffset = 0.0;
//                for (var s = startShape; s < meta.Shape; s++)
//                {
//                    var d = Coordinate.DistanceEstimateInMeter(
//                        route.Shape[s], route.Shape[s + 1]);
//                    if (s < shape)
//                    {
//                        segmentDistanceOffset += d;
//                    }
//                    else if (s == shape)
//                    {
//                        segmentDistanceOffset += Coordinate.DistanceEstimateInMeter(
//                            route.Shape[s], projected);
//                    }
//                    segmentDistance += d;
//                }
//
//                if (Math.Abs(segmentDistance) < double.Epsilon)
//                {
//                    break;
//                }
//                timeFromStartInSeconds = segmentStartTime + (meta.Time -
//                                                             segmentStartTime) * (segmentDistanceOffset / segmentDistance);
//                break;
//            }
//            return true;
//        }
//
//        /// <summary>
//        /// Returns the turn direction for the shape point at the given index.
//        /// </summary>
//        public static RelativeDirection RelativeDirectionAt(this Route route, int i, float toleranceInMeters = 1)
//        {
//            if (i < 0 || i >= route.Shape.Count) { throw new ArgumentOutOfRangeException(nameof(i)); }
//
//            if (i == 0 || i == route.Shape.Count - 1)
//            { // not possible to calculate a relative direction for the first or last segment.
//                throw new ArgumentOutOfRangeException(nameof(i), "It's not possible to calculate a relative direction for the first or last segment.");
//            }
//
//            var h = i - 1;
//            while (h > 0 && Coordinate.DistanceEstimateInMeter(route.Shape[h].Latitude, route.Shape[h].Longitude,
//                    route.Shape[i].Latitude, route.Shape[i].Longitude) < toleranceInMeters)
//            { // work backward from i to make sure we don't use an identical coordinate or one that's too close to be useful.
//                h--;
//            }
//            var j = i + 1;
//            while (j < route.Shape.Count - 1 && Coordinate.DistanceEstimateInMeter(route.Shape[j].Latitude, route.Shape[j].Longitude,
//                    route.Shape[i].Latitude, route.Shape[i].Longitude) < toleranceInMeters)
//            { // work forward from i to make sure we don't use an identical coordinate or one that's too close to be useful.
//                j++;
//            }
//
//            var dir = DirectionCalculator.Calculate(
//                new Coordinate(route.Shape[h].Latitude, route.Shape[h].Longitude),
//                new Coordinate(route.Shape[i].Latitude, route.Shape[i].Longitude),
//                new Coordinate(route.Shape[j].Latitude, route.Shape[j].Longitude));
//            if (double.IsNaN(dir.Angle))
//            { // it's possible the angle doesn't make sense, best to not return anything in that case.
//                return null;
//            }
//            return dir;
//        }
//
//        /// <summary>
//        /// Returns the direction to the next shape segment.
//        /// </summary>
//        public static DirectionEnum DirectionToNext(this Route route, int i)
//        {
//            if (i < 0 || i >= route.Shape.Count - 1) { throw new ArgumentOutOfRangeException(nameof(i)); }
//
//            return DirectionCalculator.Calculate(
//                new Coordinate(route.Shape[i].Latitude, route.Shape[i].Longitude),
//                new Coordinate(route.Shape[i + 1].Latitude, route.Shape[i + 1].Longitude));
//        }
    }
}