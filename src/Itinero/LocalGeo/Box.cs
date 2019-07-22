namespace Itinero.LocalGeo
{
    /// <summary>
    /// Represents a box.
    /// </summary>
    public struct Box
    {
        private readonly double _minLat;
        private readonly double _minLon;
        private readonly double _maxLat;
        private readonly double _maxLon;

        /// <summary>
        /// Creates a new box.
        /// </summary>
        public Box(Coordinate coordinate1, Coordinate coordinate2)
            : this(coordinate1.Latitude, coordinate1.Longitude, coordinate2.Latitude, coordinate2.Longitude)
        {

        }

        /// <summary>
        /// Creates a new box.
        /// </summary>
        public Box(double lon1, double lat1, double lon2, double lat2)
        {
            if (lat1 < lat2)
            {
                _minLat = lat1;
                _maxLat = lat2;
            }
            else
            {
                _minLat = lat2;
                _maxLat = lat1;
            }

            if (lon1 < lon2)
            {
                _minLon = lon1;
                _maxLon = lon2;
            }
            else
            {
                _minLon = lon2;
                _maxLon = lon1;
            }
        }

        /// <summary>
        /// Gets the minimum latitude.
        /// </summary>
        public double MinLat => _minLat;

        /// <summary>
        /// Gets the maximum latitude.
        /// </summary>
        public double MaxLat => _maxLat;

        /// <summary>
        /// Gets the minimum longitude.
        /// </summary>
        public double MinLon => _minLon;

        /// <summary>
        /// Gets the maximum longitude.
        /// </summary>
        public double MaxLon => _maxLon;

        /// <summary>
        /// Returns true if this box overlaps the given coordinates.
        /// </summary>
        public bool Overlaps(double lon, double lat)
        {
            return _minLat < lat && lat <= _maxLat &&
                   _minLon < lon && lon <= _maxLon;
        }

        /// <summary>
        /// Gets the exact center of this box.
        /// </summary>
        public Coordinate Center =>
            new Coordinate()
            {
                Latitude = (_maxLat + _minLat) / 2f,
                Longitude = (_minLon + _maxLon) / 2f
            };
    }
}