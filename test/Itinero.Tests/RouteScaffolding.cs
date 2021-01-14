using System.Collections.Generic;
using System.Linq;
using Itinero.Routes;

namespace Itinero.Tests {
    public class RouteScaffolding {
        public static (double lon, double lat)[] P(params (double lon, double lat)[] parts) {
            return parts;
        }

        public static Route GenerateRoute(
            params ((double lon, double lat)[] coordinates, List<(string, string)> segmentAttributes)[] parts) {
            return GenerateRoute(new List<Route.Branch>(), parts);
        }

        /**
         * Generates a route object to use in testing.
         * Note: the last coordinate of each segments SHOULD NOT BE included (except for the last segment)
         */
        public static Route GenerateRoute(
            List<Route.Branch> branches,
            params ((double lon, double lat)[] coordinates, List<(string, string)> segmentAttributes)[] parts) {
            var meta = parts.Select(
                (part, i) => new Route.Meta {
                    Shape = i,
                    Attributes = part.segmentAttributes
                }).ToList();

            var allCoordinates = new List<(double longitude, double latitude)>();
            foreach (var part in parts) {
                allCoordinates.AddRange(part.coordinates);
            }

            Route.Branch[] branchesArr = branches?.ToArray() ?? System.Array.Empty<Route.Branch>();
            
            return new Route {
                ShapeMeta = meta,
                Shape = allCoordinates,
                Branches = branchesArr
            };
        }
    }
}