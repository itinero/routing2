using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using Itinero.Geo.Directions;
using Itinero.Network.Attributes;

namespace Itinero.Routes
{
    /// <summary>
    /// Represents a route.
    /// </summary>
    public partial class Route : IEnumerable<RoutePosition>
    {
        /// <summary>
        /// Gets or sets the shape.
        /// </summary>
        public List<(double longitude, double latitude)> Shape { get; set; } = new List<(double longitude, double latitude)>();

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        public IEnumerable<(string key, string value)> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the stops.
        /// </summary>
        public List<Stop> Stops { get; set; } = new List<Stop>();

        /// <summary>
        /// Gets or sets the meta data.
        /// </summary>
        public List<Meta> ShapeMeta { get; set; } = new List<Meta>();

        /// <summary>
        /// Represents a stop.
        /// </summary>
        public class Stop
        {
            /// <summary>
            /// Gets or sets the shape index.
            /// </summary>
            public int Shape { get; set; }

            /// <summary>
            /// Gets or sets the coordinates.
            /// </summary>
            public (double longitude, double latitude) Coordinate { get; set; }

            /// <summary>
            /// Gets or sets the attributes.
            /// </summary>
            public IEnumerable<(string key, string value)>  Attributes { get; set; }

            /// <summary>
            /// Creates a clone of this object.
            /// </summary>
            public Stop Clone()
            {
                IEnumerable<(string key, string value)> attributes = null;
                if (this.Attributes != null)
                {
                    attributes = new List<(string key, string value)>(this.Attributes);
                }
                return new Stop()
                {
                    Attributes = attributes,
                    Shape = this.Shape,
                    Coordinate = this.Coordinate,
                    Distance = this.Distance,
                    Time = this.Time
                };
            }

            /// <summary>
            /// The distance in meter.
            /// </summary>
            public double Distance { get; set; }

            /// <summary>
            /// The time in seconds.
            /// </summary>
            public double Time { get; set; }

            /// <summary>
            /// Returns a description of this stop.
            /// </summary>
            public override string ToString()
            {
                return $"{this.Attributes}@{this.Coordinate} ({this.Distance}m {this.Time}s)";
            }
        }

        /// <summary>
        /// Represents meta-data about a part of this route.
        /// </summary>
        public class Meta
        {
            /// <summary>
            /// The index of the last Shape where this Meta-information is valid
            /// </summary>
            /// <remarkss>
            /// A route has a list of coordinates named 'Shape'.
            /// This meta-object gives information about a part of the route,
            /// namely from the previous point till 'Shape' (shape is included)
            /// </remarks>
            public int Shape { get; set; }

            /// <summary>
            /// The attributes of the described segment from the underlying datasource.
            /// </summary>
            /// <remarks>
            /// These are thus the OpenStreetMap-tags (if the underlying data source is OSM)
            /// </remarks>
            public IEnumerable<(string key, string value)> Attributes { get; set; }

            /// <summary>
            /// Gets or sets the relative direction flag of the attributes.
            /// </summary>
            /// <remarks>
            /// One can travel in two directions over an edge; forward and backward.
            /// The attributes however are always defined in the forward direction.
            /// If this flag is false, the attributes might have to be interpreted differently
            /// </remarks>
            public bool AttributesAreForward { get; set; }

            /// <summary>
            /// The name of the profile used for routeplanning this segment.
            /// </summary>
            /// <remarks>In most cases, this will always be identical for the entire route, but might be different for different segments in multimodal routes</remarks>
            public string Profile { get; set; }

            /// <summary>
            /// Creates a clone of this meta-object.
            /// </summary>
            /// <returns></returns>
            public Meta Clone()
            {
                IEnumerable<(string key, string value)> attributes = null;
                if (this.Attributes != null)
                {
                    attributes = new List<(string key, string value)>(this.Attributes);
                }
                return new Meta()
                {
                    Attributes = attributes,
                    Shape = this.Shape,
                    Distance = this.Distance,
                    Time = this.Time,
                    Profile = this.Profile
                };
            }

            /// <summary>
            /// The length of the described segment
            /// </summary>
            public double Distance { get; set; }

            /// <summary>
            /// The time needed to travel over this segment.
            /// The speed on this segment can be calculated by using this.Distance/this.Time
            /// </summary>
            public double Time { get; set; }
        }

        /// <summary>
        /// The list of edges where the traveller passes by
        /// </summary>
        public Branch[] Branches { get; set; }

        /// <summary>
        /// A branch is a which the traveller passes by when following the route.
        /// It are thus the roads not taken.
        /// </summary>
        public class Branch
        {
            /// <summary>
            /// The index of the coordinate where this branch branches of
            /// </summary>
            public int Shape { get; set; }

            /// <summary>
            /// The end-coordinate of the branch.
            /// </summary>
            /// <remarks>
            /// The start-coordinate of the branch can be obtained with route.Shape[this.Shape]
            /// </remarks>
            public (double longitude, double latitude) Coordinate { get; set; }

            /// <summary>
            /// Gets or sets the attributes.
            /// </summary>
            public IEnumerable<(string key, string value)> Attributes { get; set; }

            /// <summary>
            /// If walking from the route onto the branch, the attributes are interpreted in a forward manner if this flag is true
            /// </summary>
            public bool AttributesAreForward { get; set; }

            /// <summary>
            /// Creates a clone of this object.
            /// </summary>
            public Branch Clone()
            {
                IEnumerable<(string key, string value)> attributes = null;
                if (this.Attributes != null)
                {
                    attributes = new List<(string key, string value)>(this.Attributes);
                }
                return new Branch()
                {
                    Attributes = attributes,
                    Shape = this.Shape,
                    Coordinate = this.Coordinate
                };
            }
        }

        /// <summary>
        /// The distance in meter.
        /// </summary>
        public double TotalDistance { get; set; }

        /// <summary>
        /// The time in seconds.
        /// </summary>
        public double TotalTime { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<RoutePosition> GetEnumerator()
        {
            return new RouteEnumerator(this);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new RouteEnumerator(this);
        }
    }

    /// <summary>
    /// Represents a route enumerator.
    /// </summary>
    internal class RouteEnumerator : IEnumerator<RoutePosition>
    {
        private readonly Route _route;

        /// <summary>
        /// Creates a new route enumerator.
        /// </summary>
        internal RouteEnumerator(Route route)
        {
            _route = route;
        }

        private RoutePosition _current;

        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        public void Reset()
        {
            _current = new RoutePosition(_route,
                -1, -1, -1, -1);
        }

        /// <summary>
        /// Returns the current object.
        /// </summary>
        public RoutePosition Current => _current;

        /// <summary>
        /// Returns the current object.
        /// </summary>
        object IEnumerator.Current => _current;

        /// <summary>
        /// Move next.
        /// </summary>
        public bool MoveNext()
        {
            if (_current.Route == null)
            {
                this.Reset();
            }
            return _current.MoveNext();
        }

        /// <summary>
        /// Disposes native resources associated with this enumerator.
        /// </summary>
        public void Dispose()
        {

        }
    }

    /// <summary>
    /// Abstract representation of a route position.
    /// </summary>
    public struct RoutePosition
    {
        /// <summary>
        /// Creates a new route position.
        /// </summary>
        public RoutePosition(Route route, int shape, int stopIndex, 
            int metaIndex, int branchIndex)
        {
            this.Route = route;
            this.Shape = shape;
            this.StopIndex = stopIndex;
            this.MetaIndex = metaIndex;
            this.BranchIndex = branchIndex;
        }

        /// <summary>
        /// Gets the route.
        /// </summary>
        public Route Route { get; private set; }

        /// <summary>
        /// Gets the shape index.
        /// </summary>
        public int Shape { get; private set; }

        /// <summary>
        /// Gets the stop index.
        /// </summary>
        public int StopIndex { get; private set; }

        /// <summary>
        /// Gets the meta index.
        /// </summary>
        public int MetaIndex { get; private set; }

        /// <summary>
        /// Gets the branch index.
        /// </summary>
        public int BranchIndex { get; private set; }

        /// <summary>
        /// Move to the next position.
        /// </summary>
        public bool MoveNext()
        {
            this.Shape++;
            if (this.Route.Shape == null ||
                this.Shape >= this.Route.Shape.Count)
            {
                return false;
            }
            
            if (this.Route.Stops != null)
            {
                if (this.StopIndex == -1)
                {
                    this.StopIndex = 0;
                }
                else
                {
                    while (this.StopIndex < this.Route.Stops.Count &&
                        this.Route.Stops[this.StopIndex].Shape < this.Shape)
                    {
                        this.StopIndex++;
                    }
                }
            }

            if (this.Route.ShapeMeta != null)
            {
                if (this.MetaIndex == -1)
                {
                    this.MetaIndex = 0;
                }
                else
                {
                    while (this.MetaIndex < this.Route.ShapeMeta.Count &&
                        this.Route.ShapeMeta[this.MetaIndex].Shape < this.Shape)
                    {
                        this.MetaIndex++;
                    }
                }
            }

            if (this.Route.Branches != null)
            {
                if (this.BranchIndex == -1)
                {
                    this.BranchIndex = 0;
                }
                else
                {
                    while (this.BranchIndex < this.Route.Branches.Length &&
                        this.Route.Branches[this.BranchIndex].Shape < this.Shape)
                    {
                        this.BranchIndex++;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Move to the next position.
        /// </summary>
        public bool MovePrevious()
        {
            this.Shape--;
            if (this.Route.Shape == null ||
                this.Shape < 0 ||
                this.Shape >= this.Route.Shape.Count)
            {
                return false;
            }

            while (this.Route.Stops != null &&
                this.StopIndex > 0 &&
                this.StopIndex < this.Route.Stops.Count &&
                this.Route.Stops[this.StopIndex].Shape > this.Shape)
            {
                this.StopIndex--;
            }

            while (this.Route.ShapeMeta != null &&
                this.MetaIndex > 0 &&
                this.MetaIndex < this.Route.ShapeMeta.Count &&
                this.Route.ShapeMeta[this.MetaIndex].Shape > this.Shape)
            {
                this.MetaIndex--;
            }

            while (this.Route.Branches != null &&
                this.BranchIndex > 0 &&
                this.BranchIndex < this.Route.Branches.Length &&
                this.Route.Branches[this.BranchIndex].Shape > this.Shape)
            {
                this.BranchIndex--;
            }
            return true;
        }
    }

    /// <summary>
    /// Extension methods for the IRoutePosition-interface.
    /// </summary>
    public static class IRoutePositionExtensions
    {
        /// <summary>
        /// Returns true if this position has stops.
        /// </summary>
        public static bool HasStops(this RoutePosition position)
        {
            return position.Route.Stops != null &&
                position.Route.Stops.Count > position.StopIndex &&
                position.Route.Stops[position.StopIndex].Shape == position.Shape;
        }

        /// <summary>
        /// Returns the stops at this position.
        /// </summary>
        public static IEnumerable<Route.Stop> Stops(this RoutePosition position)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns true if this position has branches.
        /// </summary>
        public static bool HasBranches(this RoutePosition position)
        {
            return position.Route.Branches != null &&
                position.Route.Branches.Length > position.BranchIndex &&
                position.Route.Branches[position.BranchIndex].Shape == position.Shape;
        }

        /// <summary>
        /// Returns the branches at this position.
        /// </summary>
        public static IEnumerable<Route.Branch> Branches(this RoutePosition position)
        {
            var branches = new List<Route.Branch>();
            if (position.Route.Branches != null &&
                position.Route.Branches.Length > position.BranchIndex &&
                position.Route.Branches[position.BranchIndex].Shape == position.Shape)
            {
                var branchIndex = position.BranchIndex;
                while (position.Route.Branches.Length > branchIndex && 
                    position.Route.Branches[branchIndex].Shape == position.Shape)
                {
                    branches.Add(position.Route.Branches[branchIndex]);
                    branchIndex++;
                }
            }
            return branches;
        }

        /// <summary>
        /// Returns true if this position has current meta.
        /// </summary>
        public static bool HasCurrentMeta(this RoutePosition position)
        {
            return position.Route.ShapeMeta != null &&
                position.Route.ShapeMeta.Count > position.MetaIndex &&
                position.Route.ShapeMeta[position.MetaIndex].Shape == position.Shape;
        }

        /// <summary>
        /// Returns the current meta.
        /// </summary>
        public static Route.Meta CurrentMeta(this RoutePosition position)
        {
            if (position.HasCurrentMeta())
            {
                return position.Route.ShapeMeta[position.MetaIndex];
            }
            return null;
        }

        /// <summary>
        /// Returns the meta that applies to this position.
        /// </summary>
        public static Route.Meta Meta(this RoutePosition position)
        {
            if (position.Route.ShapeMeta != null &&
                position.Route.ShapeMeta.Count > position.MetaIndex)
            {
                return position.Route.ShapeMeta[position.MetaIndex];
            }
            return null;
        }

        /// <summary>
        /// Returns true if this position is the first position.
        /// </summary>
        public static bool IsFirst(this RoutePosition position)
        {
            return position.Shape == 0;
        }

        /// <summary>
        /// Returns true if this position is the last position.
        /// </summary>
        public static bool IsLast(this RoutePosition position)
        {
            return position.Route.Shape.Count - 1 == position.Shape;
        }

        /// <summary>
        /// Gets the previous location.
        /// </summary>
        public static (double longitude, double latitude) PreviousLocation(this RoutePosition position)
        {
            return position.Route.Shape[position.Shape - 1];
        }

        /// <summary>
        /// Gets the next location.
        /// </summary>
        public static (double longitude, double latitude) NextLocation(this RoutePosition position)
        {
            return position.Route.Shape[position.Shape + 1];
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public static (double longitude, double latitude) Location(this RoutePosition position)
        {
            return position.Route.Shape[position.Shape];
        }

        /// <summary>
        /// Gets the direction at this position.
        /// </summary>
        public static DirectionEnum Direction(this RoutePosition position)
        {
            return DirectionCalculator.Calculate(position.Location(), position.NextLocation());
        }

        /// <summary>
        /// Gets the meta attribute for route at the current position.
        /// </summary>
        public static string GetMetaAttribute(this RoutePosition position, string key)
        {
            var meta = position.Meta();
            if (meta?.Attributes == null)
            {
                return string.Empty;
            }
            if (!meta.Attributes.TryGetValue(key, out var value))
            {
                return string.Empty;
            }
            return value;
        }

        /// <summary>
        /// Returns true if the meta attribute for the route at the current position contains the given attribute.
        /// </summary>
        public static bool ContainsMetaAttribute(this RoutePosition position, string key, string value)
        {
            var meta = position.Meta();
            if (meta?.Attributes == null)
            {
                return false;
            }
            return meta.Attributes.Contains((key, value));
        }

        /// <summary>
        /// Gets the next route position.
        /// </summary>
        public static RoutePosition? Next(this RoutePosition position)
        {
            if(position.MoveNext())
            {
                return position;
            }
            return null;
        }

        /// <summary>
        /// Gets the previous route position.
        /// </summary>
        public static RoutePosition? Previous(this RoutePosition position)
        {
            if (position.MovePrevious())
            {
                return position;
            }
            return null;
        }

        /// <summary>
        /// Gets the next position until a given stop condition is met.
        /// </summary>
        public static RoutePosition? GetNextUntil(this RoutePosition position, Func<RoutePosition, bool> stopHere)
        {
            var next = position.Next();
            while (next != null)
            {
                if (stopHere(next.Value))
                {
                    return next;
                }
                next = position.Next();
            }
            return null;
        }

        /// <summary>
        /// Gets the previous position until a given stop condition is met.
        /// </summary>
        public static RoutePosition? GetPreviousUntil(this RoutePosition position, Func<RoutePosition, bool> stopHere)
        {
            var next = position.Previous();
            while (next != null)
            {
                if (stopHere(next.Value))
                {
                    return next;
                }

                next = position.Previous();
            }
            return null;
        }
    }
}