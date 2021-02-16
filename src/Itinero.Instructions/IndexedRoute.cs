using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Routes;

[assembly: InternalsVisibleTo("Itinero.Tests.Instructions")]

namespace Itinero.Instructions
{
    /// <summary>
    ///     Helper class to allow easy access on some parts of the route
    /// </summary>
    public class IndexedRoute
    {
        public List<List<Route.Branch>> Branches { get; }

        /// <summary>
        ///     A one-on-one list, where every meta is matched with every shape.
        ///     (thus: Meta[i] will correspond with the meta for the segment starting at Shape[i]
        ///     (Note that Shape[Shape.Count - 1] is thus _not_ defined
        /// </summary>
        public List<Route.Meta> Meta { get; }

        public Route Route { get; }


        internal IndexedRoute(Route route)
        {
            Route = route;
            Meta = this.BuildMetaList(route);
            Branches = BuildBranchesList(route);
        }

        public List<(double longitude, double latitude, float? e)> Shape => Route.Shape;
        public int Last => this.Shape.Count - 1;


        private List<Route.Meta> BuildMetaList(Route route)
        {
            var metas = new List<Route.Meta>();
            if (route.ShapeMeta == null || route.ShapeMeta.Count == 0) {
                throw new ArgumentException("Cannot generate route instructions if metainformation is missing");
            }

            foreach (var meta in route.ShapeMeta) {
                // Meta.shape indicates the last  element in the list
                while (metas.Count < meta.Shape) {
                    metas.Add(meta);
                }
            }
#if DEBUG
            if (metas.Count + 1 != route.Shape.Count) {
                throw new Exception("Length of the meta doesn't match. There are " + route.Shape.Count +
                                    " shapes, but the last meta has an index of " +
                                    route.ShapeMeta[^1].Shape + ", resulting in a metalist of " + metas.Count);
            }
#endif

            return metas;
        }

        private static List<List<Route.Branch>> BuildBranchesList(Route route)
        {
            var branches = new List<List<Route.Branch>>();
            if (route.Branches == null) {
                return branches;
            }

            foreach (var _ in route.Shape) {
                branches.Add(new List<Route.Branch>());
            }

            foreach (var branch in route.Branches) {
                branches[branch.Shape].Add(branch);
            }

            return branches;
        }

        public double DistanceToNextPoint(int offset)
        {
            (double longitude, double latitude, float? e) prevPoint;
            if (offset == -1) {
                prevPoint = Route.Stops[0].Coordinate;
            }
            else {
                prevPoint = this.Shape[offset];
            }

            (double, double, float? e) nextPoint;
            if (this.Last == offset) {
                nextPoint = Route.Stops[Route.Stops.Count - 1].Coordinate;
            }
            else {
                nextPoint = this.Shape[offset + 1];
            }

            return Utils.DistanceEstimateInMeter(prevPoint, nextPoint);
        }

        public double DepartingDirectionAt(int offset)
        {
            (double, double, float?) nextPoint;
            if (this.Last == offset) {
                nextPoint = Route.Stops[^1].Coordinate;
            }
            else {
                nextPoint = this.Shape[offset + 1];
            }


            return Utils.AngleBetween(this.Shape[offset], nextPoint);
        }

        /// <summary>
        ///     The absolute angle when arriving
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public double ArrivingDirectionAt(int offset)
        {
            (double longitude, double latitude, float? e) prevPoint;
            if (offset == 0) {
                prevPoint = Route.Stops[0].Coordinate;
            }
            else {
                prevPoint = this.Shape[offset - 1];
            }

            return Utils.AngleBetween(prevPoint, this.Shape[offset]);
        }
        
 
        /// <summary>
        ///     The direction change at a given shape index.
        ///     Going straight on at this shape will result in 0° here.
        ///     Making a perfectly right turn, results in -90°
        ///     Making a perfectly left turn results in +90°
        ///     Value will always be between +180 and -180
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public int DirectionChangeAt(int shape)
        {
            return (this.DepartingDirectionAt(shape) - this.ArrivingDirectionAt(shape)).NormalizeDegrees();
        }

        // ReSharper disable once UnusedMember.Global
        public string GeojsonPoints()
        {
            var parts = this.Shape.Select(
                (s, i) =>
                    "{ \"type\": \"Feature\", \"properties\": { \"marker-color\": \"#7e7e7e\", \"marker-size\": \"medium\", \"marker-symbol\": \"\", \"index\": \"" +
                    i + "\" }, \"geometry\": { \"type\": \"Point\", \"coordinates\": [ " + s.longitude + "," +
                    s.latitude + " ] }}");
            return string.Join(",\n", parts);
        }


        public string GeojsonLines()
        {
            var parts = new List<string>();
            for (var i = 0; i < this.Shape.Count - 1; i++) {
                var meta = Meta[i].Attributes.Select(attr => $"\"{attr.key}\": \"{attr.value}\"").ToList();
                meta.Add($"\"index\": {i}");
                
                var coordinates =
                    $"[ [{Shape[i].longitude}, {Shape[i].latitude}], [{Shape[i + 1].longitude}, {Shape[i + 1].latitude}] ]";
                var part =
                    "\n{\"type\":\"Feature\"," +
                    "\n   \"properties\":{" + string.Join(",",meta) + "}," +
                    $"\n   \"geometry\":{{\"type\":\"LineString\",\"coordinates\": {coordinates} }}" +
                    "\n}";
 
                parts.Add(part);
            }

            return InGeoJson(string.Join(",\n", parts));
        }

        private static string InGeoJson(string contents)
        {
            return "{\"type\": \"FeatureCollection\",\"features\": [\n\n" + contents + "\n\n]}";
        }
    }
}