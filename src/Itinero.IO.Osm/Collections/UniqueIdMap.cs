/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System.Collections.Generic;

namespace Itinero.IO.Osm.Collections;

/// <summary>
/// A unique id map, only vertex per id.
/// </summary>
public class UniqueIdMap<T>
    where T : struct
{
    private readonly Dictionary<long, Block> _blocks;
    private readonly int _blockSize;
    private static T _defaultValue;

    /// <summary>
    /// Creates a new id map.
    /// </summary>
    public UniqueIdMap(T defaultValue, int blockSize = 32)
    {
        _blocks = new Dictionary<long, Block>();
        _blockSize = blockSize;
        _defaultValue = defaultValue;
    }

    /// <summary>
    /// Sets a tile id.
    /// </summary>
    public void Set(long id, T vertex)
    {
        var blockIdx = id / _blockSize;
        var offset = id - blockIdx * _blockSize;

        if (!_blocks.TryGetValue(blockIdx, out var block))
        {
            block = new Block
            {
                Start = (uint)offset,
                End = (uint)offset,
                Data = new T[] { vertex }
            };
            _blocks[blockIdx] = block;
        }
        else
        {
            block.Set(offset, vertex);
            _blocks[blockIdx] = block;
        }
    }

    /// <summary>
    /// Gets or sets the tile id for the given id.
    /// </summary>
    public T this[long id]
    {
        get => this.Get(id);
        set => this.Set(id, value);
    }

    /// <summary>
    /// Gets a tile id.
    /// </summary>
    public T Get(long id)
    {
        var blockIdx = id / _blockSize;
        var offset = id - blockIdx * _blockSize;

        if (!_blocks.TryGetValue(blockIdx, out var block))
        {
            return _defaultValue;
        }

        return block.Get(offset);
    }

    /// <summary>
    /// An enumerable with the non-default indices in this map.
    /// </summary>
    public IEnumerable<long> NonDefaultIndices => throw new System.NotImplementedException();

    private struct Block
    {
        public uint Start { get; set; }

        public uint End { get; set; }

        public T[] Data { get; set; }

        public T Get(long offset)
        {
            if (this.Start > offset)
            {
                return _defaultValue;
            }
            else if (offset > this.End)
            {
                return _defaultValue;
            }

            return this.Data[offset - this.Start];
        }

        public void Set(long offset, T value)
        {
            if (this.Start > offset)
            { // expand at the beginning.
                var newData = new T[this.End - offset + 1];
                this.Data.CopyTo(newData, (int)(this.Start - offset));
                for (var i = 1; i < this.Start - offset; i++)
                {
                    newData[i] = _defaultValue;
                }

                this.Data = newData;
                this.Start = (uint)offset;
                this.Data[0] = value;
            }
            else if (this.End < offset)
            { // expand at the end.
                var newData = new T[offset - this.Start + 1];
                this.Data.CopyTo(newData, 0);
                for (var i = this.End + 1 - this.Start; i < newData.Length - 1; i++)
                {
                    newData[i] = _defaultValue;
                }

                this.Data = newData;
                this.End = (uint)offset;
                this.Data[offset - this.Start] = value;
            }
            else
            {
                this.Data[offset - this.Start] = value;
            }
        }
    }
}
