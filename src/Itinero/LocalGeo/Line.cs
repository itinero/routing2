namespace Itinero.LocalGeo
{
    /// <summary>
    /// Represents a line.
    /// </summary>
    public struct Line
    {
        const double E = 0.0000000001;
        private readonly Coordinate _coordinate1;
        private readonly Coordinate _coordinate2;

        /// <summary>
        /// Creates a new line.
        /// </summary>
        public Line(Coordinate coordinate1, Coordinate coordinate2)
        {
            _coordinate1 = coordinate1;
            _coordinate2 = coordinate2;
        }

        /// <summary>
        /// Returns parameter A of an equation describing this line as Ax + By = C
        /// </summary>
        public double A => _coordinate2.Latitude - _coordinate1.Latitude;

        /// <summary>
        /// Returns parameter B of an equation describing this line as Ax + By = C
        /// </summary>
        public double B => _coordinate1.Longitude - _coordinate2.Longitude;

        /// <summary>
        /// Returns parameter C of an equation describing this line as Ax + By = C
        /// </summary>
        public double C => this.A * _coordinate1.Longitude + this.B * _coordinate1.Latitude;

        /// <summary>
        /// Gets the first coordinate.
        /// </summary>
        public Coordinate Coordinate1 => _coordinate1;

        /// <summary>
        /// Gets the second coordinate.
        /// </summary>
        public Coordinate Coordinate2 => _coordinate2;

        /// <summary>
        /// Gets the middle of this line.
        /// </summary>
        /// <returns></returns>
        public Coordinate Middle => new Coordinate((this.Coordinate1.Latitude + this.Coordinate2.Latitude) / 2,
            (this.Coordinate1.Longitude + this.Coordinate2.Longitude) / 2);


        /// <summary>
        /// Gets the length of this line.
        /// </summary>
        public double LengthInMeters => Coordinate.DistanceEstimateInMeter(_coordinate1, _coordinate2);

        /// <summary>
        /// Calculates the intersection point of the given line with this line. 
        /// 
        /// Returns null if the lines have the same direction or don't intersect.
        /// 
        /// Assumes the given line is not a segment and this line is a segment.
        /// </summary>
        public Coordinate? Intersect(Line line)
        {
            var det = (double)(line.A * this.B - this.A * line.B);
            if (System.Math.Abs(det) <= E)
            { // lines are parallel; no intersections.
                return null;
            }
            else
            { // lines are not the same and not parallel so they will intersect.
                var x = (this.B * line.C - line.B * this.C) / det;
                var y = (line.A * this.C - this.A * line.C) / det;

                var coordinate = new Coordinate(x ,y);

                // check if the coordinate is on this line.
                var dist = this.A * this.A + this.B * this.B;
                var line1 = new Line(coordinate, _coordinate1);
                var distTo1 = line1.A * line1.A + line1.B * line1.B;
                if (distTo1 > dist)
                {
                    return null;
                }
                var line2 = new Line(coordinate, _coordinate2);
                var distTo2 = line2.A * line2.A + line2.B * line2.B;
                if (distTo2 > dist)
                {
                    return null;
                }

                if (!_coordinate1.Elevation.HasValue || !_coordinate2.Elevation.HasValue) return coordinate;
                
                if (_coordinate1.Elevation == _coordinate2.Elevation)
                { // don't calculate anything, elevation is identical.
                    coordinate.Elevation = _coordinate1.Elevation;
                }
                else if (System.Math.Abs(this.A) < E && System.Math.Abs(this.B) < E)
                { // tiny segment, not stable to calculate offset
                    coordinate.Elevation = _coordinate1.Elevation;
                }
                else
                { // calculate offset and calculate an estimate of the elevation.
                    if (System.Math.Abs(this.A) > System.Math.Abs(this.B))
                    {
                        var diffLat = System.Math.Abs(this.A);
                        var diffLatIntersection = System.Math.Abs(coordinate.Latitude - _coordinate1.Latitude);

                        coordinate.Elevation = (short)((_coordinate2.Elevation - _coordinate1.Elevation) * (diffLatIntersection / diffLat) +
                                                       _coordinate1.Elevation);
                    }
                    else
                    {
                        var diffLon = System.Math.Abs(this.B);
                        var diffLonIntersection = System.Math.Abs(coordinate.Longitude - _coordinate1.Longitude);

                        coordinate.Elevation = (short)((_coordinate2.Elevation - _coordinate1.Elevation) * (diffLonIntersection / diffLon) +
                                                       _coordinate1.Elevation);
                    }
                }
                return coordinate;
            }
        }

        /// <summary>
        /// Projects for coordinate on this line.
        /// </summary>
        public Coordinate? ProjectOn(Coordinate coordinate)
        {
            // TODO: do we need to calculate the expensive length in meter, this can be done more easily.
            var lengthInMeters = this.LengthInMeters;
            if (lengthInMeters < E)
            { 
                return null;
            }

            // get direction vector.
            var diffLat = (_coordinate2.Latitude - _coordinate1.Latitude) * 100.0;
            var diffLon = (_coordinate2.Longitude - _coordinate1.Longitude) * 100.0;

            // increase this line in length if needed.
            var thisLine = this;
            if (lengthInMeters < 50)
            {
                thisLine = new Line(_coordinate1, new Coordinate(diffLon + coordinate.Longitude, diffLat + coordinate.Latitude));
            }

            // rotate 90°.
            var temp = diffLon;
            diffLon = -diffLat;
            diffLat = temp;

            // create second point from the given coordinate.
            var second = new Coordinate(diffLon + coordinate.Longitude, diffLat + coordinate.Latitude);

            // create a second line.
            var line = new Line(coordinate, second);

            // calculate intersection.
            var projected = thisLine.Intersect(line);

            // check if coordinate is on this line.
            if (!projected.HasValue)
            {
                return null;
            }

            if (thisLine.Equals(this)) return projected;
            
            // check if the coordinate is on this line.
            var dist = this.A * this.A + this.B * this.B;
            var line1 = new Line(projected.Value, _coordinate1);
            var distTo1 = line1.A * line1.A + line1.B * line1.B;
            if (distTo1 > dist)
            {
                return null;
            }
            var line2 = new Line(projected.Value, _coordinate2);
            var distTo2 = line2.A * line2.A + line2.B * line2.B;
            if (distTo2 > dist)
            {
                return null;
            }
            return projected;
        }

        /// <summary>
        /// Returns the distance from the point to this line.
        /// </summary>
        public double? DistanceInMeter(Coordinate coordinate)
        {
            var projected = this.ProjectOn(coordinate);
            if (projected.HasValue)
            {
                return Coordinate.DistanceEstimateInMeter(coordinate, projected.Value);
            }
            return null;
        }

        public override string ToString()
        {
            return $"{this._coordinate1}->{this._coordinate2}";
        }
    }
}