using System.Collections.Generic;

namespace Itinero.Instructions
{
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

        public int DirectionChangeAt(int offset)
        {
            return (DepartingDirectionAt(offset) - ArrivingDirectionAt(offset)).NormalizeDegrees();
        }
        
    }
}