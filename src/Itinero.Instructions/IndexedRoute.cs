using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Generators;
using Itinero.Instructions.ToText;
using Itinero.Routes;

namespace Itinero.Instructions
{
    /**
     * Allows easy access between shapes and meta of a route
     */
    public class IndexedRoute
    {
        public readonly List<List<Route.Branch>> Branches;

        /// <summary>
        ///     A one-on-one list, where every meta is matched with every shape.
        ///     (thus: Meta[i] will correspond with the meta for the segment starting at Shape[i]
        /// (Note that Shape[Shape.Count - 1] is thus _not_ defined
        /// 
        /// </summary>
        public readonly List<Route.Meta> Meta;

        public readonly Route Route;


        public IndexedRoute(Route route)
        {
            Route = route;
            Meta = BuildMetaList(route);
            Branches = BuildBranchesList(route);
        }

        public List<(double longitude, double latitude, float? e)> Shape => Route.Shape;
        public int Last => Shape.Count - 1;


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
                prevPoint = Shape[offset];
            }

            (double, double, float? e) nextPoint;
            if (Last == offset) {
                nextPoint = Route.Stops[Route.Stops.Count - 1].Coordinate;
            }
            else {
                nextPoint = Shape[offset + 1];
            }

            return Utils.DistanceEstimateInMeter(prevPoint, nextPoint);
        }

        public double DepartingDirectionAt(int offset)
        {
            (double, double, float?) nextPoint;
            if (Last == offset) {
                nextPoint = Route.Stops[^1].Coordinate;
            }
            else {
                nextPoint = Shape[offset + 1];
            }


            return Utils.AngleBetween(Shape[offset], nextPoint);
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
                prevPoint = Shape[offset - 1];
            }

            return Utils.AngleBetween(prevPoint, Shape[offset]);
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
            return (DepartingDirectionAt(shape) - ArrivingDirectionAt(shape)).NormalizeDegrees();
        }

        // ReSharper disable once UnusedMember.Global
        public string GeojsonPoints()
        {
            var parts = Shape.Select(
                (s, i) =>
                    "{ \"type\": \"Feature\", \"properties\": { \"marker-color\": \"#7e7e7e\", \"marker-size\": \"medium\", \"marker-symbol\": \"\", \"index\": \"" +
                    i + "\" }, \"geometry\": { \"type\": \"Point\", \"coordinates\": [ " + s.longitude + "," +
                    s.latitude + " ] }}");
            return string.Join(",\n", parts);
        }

        internal string GeojsonLines(IEnumerable<BaseInstruction> instructions, IInstructionToText toText)
        {
            return GeojsonLines(instructions.Select(i => (i.ShapeIndex, i.ShapeIndexEnd, toText.ToText(i))).ToArray());
        }

        public string GeojsonLines((int index, int end, string text)[] instructions)
        {
            var parts = new List<string>();
            var colors = new List<string> {
                "#ff0000", "#00ff00", "#0000ff", "#000000"
            };

            for (var i = 0; i < instructions.Length; i++) {
                var (index, end, text) = instructions[i];
                var coordinates = Route.Shape.GetRange(index, Math.Max(2, 1 + end - index));
                var meta = Meta[index];

                var properties = new List<(string k, string v)> {
                    ("instruction", text),
                    ("shapeStart", "" + index),
                    ("shapeEnd", "" + end),
                    ("distance", "" + meta.Distance),
                    ("time", "" + meta.Time),
                    ("stroke", colors[i % colors.Count]),
                    ("stroke-width", "2"),
                    ("stroke-opacity", "1")
                };
                properties.AddRange(meta.Attributes);
                var part =
                    "{\"type\":\"Feature\",\"properties\":{" +
                    string.Join(", ", properties.Select(t => $"\"{t.k}\":\"{t.v}\"")) +
                    "},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[" +
                    string.Join(", ", coordinates.Select(c => $"[{c.longitude}, {c.latitude}]")) +
                    "]}}";
                parts.Add(part);
            }

            return string.Join(",\n", parts);
        }

        public static string InGeoJson(string contents)
        {
            return "{\"type\": \"FeatureCollection\",\"features\": [" + contents + "]}";
        }
    }
}