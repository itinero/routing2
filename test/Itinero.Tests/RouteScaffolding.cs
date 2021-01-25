using System.Collections.Generic;
using System.Linq;
using Itinero.Routes;

namespace Itinero.Tests {
    public class RouteScaffolding {
        public static (double lon, double lat, float? e)[] P(params (double lon, double lat, float? e)[] parts) {
            return parts;
        }

        public static Route GenerateRoute(
            params ((double lon, double lat, float? e)[] coordinates, List<(string, string)> segmentAttributes)[] parts) {
            return GenerateRoute(new List<Route.Branch>(), parts);
        }

        /**
         * Generates a route object to use in testing.
         * Note: the last coordinate of each segments SHOULD NOT BE included (except for the last segment)
         */
        public static Route GenerateRoute(
            List<Route.Branch> branches,
            params ((double lon, double lat, float? e)[] coordinates, List<(string, string)> segmentAttributes)[] parts)  
        {
            var allCoordinates = new List<(double longitude, double latitude, float? e)>();
            var metas = new List<Route.Meta>();
            foreach (var part in parts) {
                allCoordinates.AddRange(part.coordinates);
                var meta = new Route.Meta {
                    Shape = allCoordinates.Count, // This is different from the routebuilder, as that one _does_ include the last coordinate 
                    Attributes = part.segmentAttributes
                };
                metas.Add(meta);
            }

            metas[^1].Shape--;
            Route.Branch[] branchesArr = branches?.ToArray() ?? System.Array.Empty<Route.Branch>();
            
            return new Route {
                ShapeMeta = metas,
                Shape = allCoordinates,
                Branches = branchesArr
            };
        }
    }
}