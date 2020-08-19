using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;

namespace Itinero.Data.Graphs.TurnCosts
{
     /// <summary>
    /// A graph type index.
    ///
    /// This has a:
    /// - A function to determine turn cost type.
    /// - A graph turn cost type collection.
    /// </summary>
    internal class GraphTurnCostTypeIndex
    {
        private readonly GraphTurnCostTypeFunc _func;
        private readonly GraphTurnCostTypeCollection _turnCostTypes;

        public GraphTurnCostTypeIndex()
        {
            _func = new GraphTurnCostTypeFunc();
            _turnCostTypes = new GraphTurnCostTypeCollection();
        }

        public int Id => _func.Id;

        public GraphTurnCostTypeFunc Func => _func;

        internal GraphTurnCostTypeCollection TurnCostTypeCollection => _turnCostTypes;

        private GraphTurnCostTypeIndex(GraphTurnCostTypeCollection turnCostTypes, GraphTurnCostTypeFunc func)
        {
            _turnCostTypes = turnCostTypes;
            _func = func;
        }

        public uint Get(IEnumerable<(string key, string value)> attributes)
        {
            var edgeType = _func.ToEdgeType(attributes);
            return _turnCostTypes.Get(edgeType);
        }
        
        public IEnumerable<(string key, string value)> GetById(uint edgeTypeId)
        {
            return _turnCostTypes.GetById(edgeTypeId);
        }

        public GraphTurnCostTypeIndex Next(GraphTurnCostTypeFunc func)
        {
            return new GraphTurnCostTypeIndex(_turnCostTypes, func);
        }

        internal void Serialize(Stream stream)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write id.
            stream.WriteVarInt32(this.Id);
            
            // write turn cost types.
            _turnCostTypes.Serialize(stream);
        }

        internal static GraphTurnCostTypeIndex Deserialize(Stream stream, 
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? func)
        {
            // get version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unexpected version #.");
            
            // read id.
            var id = stream.ReadVarInt32();
            GraphTurnCostTypeFunc turnCostTypeFunc;
            if (id == 0)
            {
                // empty edge type function, it has id 0.
                turnCostTypeFunc = new GraphTurnCostTypeFunc();
            }
            else
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func),
                        "Turn cost type function is null but it's ID indicates a function should be present.");
                turnCostTypeFunc = new GraphTurnCostTypeFunc(id, func);
            }
            
            // read turn cost types.
            var turnCostTypes = GraphTurnCostTypeCollection.Deserialize(stream);
            
            return new GraphTurnCostTypeIndex(turnCostTypes, turnCostTypeFunc);
        }
    }
}