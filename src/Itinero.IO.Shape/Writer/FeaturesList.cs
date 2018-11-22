using System;
using System.Collections;
using System.Collections.Generic;
using GeoAPI.Geometries;
using Itinero.Data.Graphs;
using Itinero.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.IO.Shape.Writer
{
    internal class FeaturesList : IList<IFeature>
    {
        private readonly RouterDb _routerDb;
        private readonly Graph.EdgeEnumerator _edgeEnumerator;

        /// <summary>
        /// Creates a new features list.
        /// </summary>
        public FeaturesList(RouterDb routerDb)
        {
            _routerDb = routerDb;

            _edgeEnumerator = routerDb.GetEdgeEnumerator();
        }

        public IFeature this[int index]
        {
            get => this.BuildFeature(index);
            set => throw new NotSupportedException("List is readonly.");
        }

        public int Count => (int)_routerDb.EdgeCount;

        public bool IsReadOnly => true;

        public void Add(IFeature item)
        {
            throw new NotSupportedException("List is readonly.");
        }

        public void Clear()
        {
            throw new NotSupportedException("List is readonly.");
        }

        public bool Contains(IFeature item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IFeature[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IFeature> GetEnumerator()
        {
            return new Enumerator(this);
        }

        private class Enumerator : IEnumerator<IFeature>
        {
            private readonly FeaturesList _list;

            public Enumerator(FeaturesList list)
            {
                _list = list;
            }

            private int _current = -1;

            public IFeature Current => _list[_current];

            object IEnumerator.Current => _list[_current];

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                _current++;
                return _current < _list.Count;
            }

            public void Reset()
            {
                _current = -1;
            }
        }

        public int IndexOf(IFeature item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, IFeature item)
        {
            throw new NotSupportedException("List is readonly.");
        }

        public bool Remove(IFeature item)
        {
            throw new NotSupportedException("List is readonly.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("List is readonly.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private IFeature BuildFeature(int index)
        {
            if (((index - 1) % 10000) == 0)
            {
                Itinero.Logging.Logger.Log("FeatureList", TraceEventType.Information,
                    "Building feature {0}/{1}.", index - 1, this.Count);
            }

            // get edge details.
            var edgeId = (uint) index;
            if (!_edgeEnumerator.MoveToEdge(edgeId))
            {
                throw new ArgumentException($"Index out of bounds.");
            }
            var vertex1 = _routerDb.GetVertex(_edgeEnumerator.From);
            var vertex2 = _routerDb.GetVertex(_edgeEnumerator.To);

            // compose geometry.
            var coordinates = new List<Coordinate>();
            coordinates.Add(new Coordinate(vertex1.Longitude, vertex1.Latitude));
            var shape = _routerDb.GetShape(edgeId);
            if (shape != null)
            {
                foreach (var shapePoint in shape)
                {
                    coordinates.Add(new Coordinate(shapePoint.Longitude, shapePoint.Latitude));
                }
            }
            coordinates.Add(new Coordinate(vertex2.Longitude, vertex2.Latitude));
            var geometry = new LineString(coordinates.ToArray());

            // compose attributes table.
            var attributesTable = new AttributesTable
            {
                {"vertex1", _edgeEnumerator.From.LocalId},
                {"vertex1_tile", _edgeEnumerator.From.TileId},
                {"vertex2", _edgeEnumerator.To.LocalId},
                {"vertex2_tile", _edgeEnumerator.To.TileId}
            };
            var attributes = _routerDb.GetAttributes(edgeId);
            foreach (var attribute in attributes)
            {
                attributesTable.Add(attribute.Key, attribute.Value);
            }
            
            return new Feature(geometry, attributesTable);
        }
    }
}