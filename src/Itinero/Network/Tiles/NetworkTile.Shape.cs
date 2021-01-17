using System.Collections.Generic;
using System.IO;
using Itinero.IO;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles
{
    internal partial class NetworkTile
    {
        // the shapes.
        private uint _nextShapePointer = 0;
        private readonly ArrayBase<byte> _shapes;
        
        private uint SetShape(IEnumerable<(double longitude, double latitude)> shape)
        {
            const int resolution = (1 << TileResolutionInBits) - 1;
            var originalPointer = _nextShapePointer;
            var blockPointer = originalPointer;
            var pointer = blockPointer + 1;
            
            if (_shapes.Length <= pointer + 8)
            {
                _shapes.Resize(_shapes.Length + DefaultSizeIncrease);
            }

            using var enumerator = shape.GetEnumerator();
            var count = 0;
            (int x, int y) previous = (int.MaxValue, int.MaxValue);
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                var (x, y) =
                    TileStatic.ToLocalTileCoordinates(_zoom, _tileId, current.longitude, current.latitude, resolution);
                if (_shapes.Length <= pointer + 8)
                {
                    _shapes.Resize(_shapes.Length + DefaultSizeIncrease);
                }
                if (count == 0)
                {
                    // first coordinate.
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, x);
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, y);
                }
                else
                {
                    // calculate diff and then store.
                    var diffX = x - previous.x;
                    var diffY = y - previous.y;
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, diffX);
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, diffY);
                }

                if (count == 255)
                {
                    // start a new block, assign 255.
                    _shapes[blockPointer] = 255;
                    blockPointer = pointer;
                    pointer = blockPointer + 1;
                    count = 0;
                }
                else
                {
                    count++;
                }

                previous = (x, y);
            }

            // a block is still open, close it.
            _shapes[blockPointer] = (byte) count;
            _nextShapePointer = pointer;

            return originalPointer;
        }

        internal IEnumerable<(double longitude, double latitude)> GetShape(uint? pointer)
        {
            if (pointer == null) yield break;
            var p = pointer.Value;
            
            const int resolution = (1 << TileResolutionInBits) - 1;
            var count = -1;
            (int x, int y) previous = (int.MaxValue, int.MaxValue); 
            do
            {
                count = _shapes[p];
                p++;
                
                for (var i = 0; i < count; i++)
                {
                    p += (uint)_shapes.GetDynamicInt32(p, out var x);
                    p += (uint)_shapes.GetDynamicInt32(p, out var y);

                    if (previous.x != int.MaxValue)
                    {
                        x = previous.x + x;
                        y = previous.y + y;
                    }
                    
                    TileStatic.FromLocalTileCoordinates(_zoom, _tileId, x, y, resolution, out var longitude, out var latitude);
                    yield return (longitude, latitude);

                    previous = (x, y);
                }
            } while (count == 255);
        }

        private void SerializeShapes(Stream stream)
        {
            stream.WriteVarUInt32(_nextShapePointer);
            for (var i = 0; i < _nextShapePointer; i++)
            {
                stream.WriteByte(_shapes[i]);
            }
        }

        private void DeserializeShapes(Stream stream)
        {
            _nextShapePointer = stream.ReadVarUInt32();
            _shapes.Resize(_nextShapePointer);
            for (var i = 0; i < _nextShapePointer; i++)
            {
                _shapes[i] = (byte)stream.ReadByte();
            }
        }
    }
}