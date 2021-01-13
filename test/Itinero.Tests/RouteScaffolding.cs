using System.Collections.Generic;
using System.Linq;
using Itinero.Routes;

namespace Itinero.Tests {
    public class RouteScaffolding {
        public static (double lon, double lat)[] P(params (double lon, double lat)[] parts) {
            return parts;
        }

        /**
         * Generates a route object to use in testing.
         * Note: the last coordinate of each segments SHOULD NOT BE included (except for the last segment)
         */
        public static Route GenerateRoute(
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

            Route.Branch[] branches = System.Array.Empty<Route.Branch>();
            
            return new Route {
                ShapeMeta = meta,
                Shape = allCoordinates,
                Branches = branches
            };
        }
    }
}