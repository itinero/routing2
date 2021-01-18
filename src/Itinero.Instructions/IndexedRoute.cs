using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Itinero.Routes;

namespace Itinero.Instructions
{
    /**
     * Allows easy access between shapes and meta of a route
     */
    public class IndexedRoute
    {
        public readonly Route Route;

        public List<(double longitude, double latitude)> Shape => Route.Shape;
        public int Last => Shape.Count - 1;

        /// <summary>
        /// A one-on-one list, where every meta is matched with every shape.
        /// (thus: Meta[i] will correspond with the meta for Shape[i]
        /// </summary>
        public readonly List<Route.Meta> Meta;

        public readonly List<List<Route.Branch>> Branches;


        public IndexedRoute(Route route)
        {
            Route = route;
            Meta = BuildMetaList(route);
            Branches = BuildBranchesList(route);
        }


        private List<Route.Meta> BuildMetaList(Route route)
        {
            var metas = new List<Route.Meta>();

            var currentMeta = route.ShapeMeta[0];
            var currentMetaIndex = 0;
            for (var i = 0; i < route.Shape.Count; i++)
            {
                if (route.ShapeMeta.Count > (currentMetaIndex + 1)
                    && route.ShapeMeta[currentMetaIndex + 1].Shape == i)
                {
                    currentMetaIndex++;
                    currentMeta = route.ShapeMeta[currentMetaIndex];
                }

                metas.Add(currentMeta);
            }

            return metas;
        }

        private static List<List<Route.Branch>> BuildBranchesList(Route route)
        {
            var branches = new List<List<Route.Branch>>();
            if (route.Branches == null) {
                return branches;
            }

            foreach (var _ in route.Shape)
            {
                branches.Add(new List<Route.Branch>());
            }

            foreach (var branch in route.Branches)
            {
                branches[branch.Shape].Add(branch);
            }

            return branches;
        }

        public double DistanceToNextPoint(int offset)
        {
            (double longitude, double latitude) prevPoint;
            if (offset == -1)
            {
                prevPoint = Route.Stops[0].Coordinate;
            }
            else
            {
                prevPoint = Shape[offset];
            }
            (double, double) nextPoint;
            if (Last == offset)
            {
                nextPoint = Route.Stops[Route.Stops.Count - 1].Coordinate;
            }
            else
            {
                nextPoint = Shape[offset + 1];
            }

            return Utils.DistanceEstimateInMeter(prevPoint, nextPoint);

        }

        public double DepartingDirectionAt(int offset)
        {
            (double, double) nextPoint;
            if (Last == offset)
            {
                nextPoint = Route.Stops[Route.Stops.Count - 1].Coordinate;
            }
            else
            {
                nextPoint = Shape[offset + 1];
            }
            


            return Utils.AngleBetween(Shape[offset], nextPoint);
        }

        /// <summary>
        /// The absolute angle when arriving
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public double ArrivingDirectionAt(int offset)
        {
            (double longitude, double latitude) prevPoint;
            if (offset == 0)
            {
                prevPoint = Route.Stops[0].Coordinate;
            }
            else
            {
                prevPoint = Shape[offset-1];
            }

            return Utils.AngleBetween(prevPoint, Shape[offset]);
        }

        /// <summary>
        /// The direction change at a given shape index.
        /// Going straight on at this shape will result in 0° here.
        ///
        /// Making a perfectly right turn, results in -90°
        /// Making a perfectly left turn results in +90°
        ///
        /// Value will always be between +180 and -180
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public int DirectionChangeAt(int shape)
        {
            return (DepartingDirectionAt(shape) - ArrivingDirectionAt(shape)).NormalizeDegrees();
        }

        public string GeojsonPoints() {
            var parts = this.Shape.Select(
                (s, i) => "{ \"type\": \"Feature\", \"properties\": { \"marker-color\": \"#7e7e7e\", \"marker-size\": \"medium\", \"marker-symbol\": \"\", \"index\": \"" +
                          i + "\" }, \"geometry\": { \"type\": \"Point\", \"coordinates\": [ " + s.longitude + "," +
                          s.latitude + " ] }}");
            return string.Join(",", parts);
        }
        
    }
}