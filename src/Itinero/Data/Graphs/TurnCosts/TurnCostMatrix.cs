using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Graphs.TurnCosts
{
    internal class TurnCostMatrix : IEquatable<TurnCostMatrix>
    {
        private readonly uint[] _costs;
        private readonly int[] _rows;
        private readonly int[] _columns;
        
        private readonly int _n;

        public TurnCostMatrix(uint[] costs)
        {
            _costs = costs;
            
            _n = (int)System.Math.Sqrt(costs.Length);
            _rows = new int[_n];
            for (var r = 0; r < _n; r++)
            {
                _rows[r] = r;
            }
            _columns = new int[_n];
            for (var c = 0; c < _n; c++)
            {
                _columns[c] = c;
            }
        }

        public int OriginalRow(int r)
        {
            return _rows[r];
        }

        public int OriginalColumn(int c)
        {
            return _columns[c];
        }

        public uint Get(int r, int c)
        {
            return _costs[(_n * r) + c];
        }

        private void Set(int r, int c, uint value)
        {
            _costs[(_n * r) + c] = value;
        }

        public void SwitchRow(int r1, int r2)
        {
            if (r1 == r2) return;
            
            // copy row1.
            var row1 = this.GetRow(r1).ToList();

            // copy over row1.
            var row2 = this.GetRow(r2);
            for (var c = 0; c < row2.Count; c++)
            {
                this.Set(r1, c, row2[c]);
            }
            
            // copy over row2.
            for (var c = 0; c < row1.Count; c++)
            {
                this.Set(r2, c, row1[c]);
            }

            // keep books on original indexes.
            var r1original = _rows[r1];
            _rows[r1] = _rows[r2];
            _rows[r2] = r1original;
        }

        public void SwitchColumn(int c1, int c2)
        {
            if (c1 == c2) return;
            
            // copy column1.
            var column1 = this.GetColumn(c1).ToList();

            // copy over column1.
            var column2 = this.GetColumn(c2);
            for (var r = 0; r < column2.Count; r++)
            {
                this.Set(r, c1, column2[r]);
            }
            
            // copy over column2.
            for (var r = 0; r < column1.Count; r++)
            {
                this.Set(r, c2, column1[r]);
            }

            // keep books on original indexes.
            var original = _columns[c1];
            _columns[c1] = _columns[c2];
            _columns[c2] = original;
        }

        public int N => _n;
        
        public Row GetRow(int r) => new Row(_costs, r);
        
        public Column GetColumn(int c) => new Column(_costs, c);
        
        internal class Row : IReadOnlyList<uint>
        {
            private readonly uint[] _costs;
            private readonly int _r;
            private readonly int _offset;

            public Row(uint[] costs, int r, int offset = 0)
            {
                _costs = costs;
                _r = r;
                _offset = offset;
                
                this.Count = (int)System.Math.Sqrt(costs.Length) - _offset;
            }

            public IEnumerator<uint> GetEnumerator()
            {
                for (var i = 0; i < this.Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count { get; }

            public uint this[int index] => _costs[(this.Count * _r) + index + _offset];
        }
        
        internal class Column : IReadOnlyList<uint>
        {
            private readonly uint[] _costs;
            private readonly int _c;
            private readonly int _offset;

            public Column(uint[] costs, int c, int offset = 0)
            {
                _costs = costs;
                _c = c;
                _offset = offset;
                
                this.Count = (int)System.Math.Sqrt(costs.Length) - _offset;
            }

            public IEnumerator<uint> GetEnumerator()
            {
                for (var i = 0; i < this.Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count { get; }

            public uint this[int index] => _costs[(this.Count * index) + _c - _offset];
        }

        public bool Equals(TurnCostMatrix? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_n != other._n) return false;
            
            for (var i = 0; i < _n; i++)
            {
                if (this._columns[i] != other._columns[i]) return false;
            }
            for (var i = 0; i < _n; i++)
            {
                if (this._rows[i] != other._rows[i]) return false;
            }
            for (var i = 0; i < this._costs.Length; i++)
            {
                if (this._costs[i] != other._costs[i]) return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TurnCostMatrix) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _n.GetHashCode();
                hashCode = (hashCode * 397) ^ _rows.Length;
                hashCode = _rows.Aggregate(hashCode, (current, t) => (current * 397) ^ t);
                hashCode = (hashCode * 397) ^ _columns.Length;
                hashCode = _columns.Aggregate(hashCode, (current, t) => (current * 397) ^ t);
                hashCode = (hashCode * 397) ^ _costs.Length;
                hashCode = _costs.Aggregate(hashCode, (current, t) => (current * 397) ^ t.GetHashCode());
                return hashCode;
            }
        }
    }
}