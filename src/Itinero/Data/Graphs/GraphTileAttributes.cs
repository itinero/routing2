//using System.Collections.Generic;
//using Reminiscence.Arrays;
//
//namespace Itinero.Data.Graphs
//{
//    internal partial class GraphTile
//    {
//        /// <summary>
//        /// Stores the attributes, starting with the number of attributes and then alternating key-value pairs.
//        /// </summary>
//        private ArrayBase<byte> _attributes;
//
//        private uint _nextAttributePointer = 0;
//        
//        /// <summary>
//        /// Stores each string once.
//        /// </summary>
//        private ArrayBase<string> _strings;
//
//        private uint _nextStringId = 0;
//
//        /// <summary>
//        /// Stores the edge attributes.
//        /// </summary>
//        /// <param name="attributes">The attributes.</param>
//        private uint SetAttributes(IEnumerable<(string key, string value)> attributes)
//        {
//            var start = _nextAttributePointer;
//
//            long cPos = start;
//            long p = start + 1;
//            var c = 0;
//            foreach (var (key, value) in attributes)
//            {
//                var id = AddOrGetString(key);
//                p += _attributes.SetDynamicUInt32(p, id);
//                id = AddOrGetString(value);
//                p += _attributes.SetDynamicUInt32(p, id);
//                
//                c++;
//                if (c == 255)
//                {
//                    _attributes[cPos] = 255;
//                    c = 0;
//                    cPos = p;
//                    p++;
//                }
//            }
//            
//            _nextAttributePointer = (uint)p;
//
//            return start;
//        }
//
//        private IEnumerable<(string key, string value)> GetEdgeAttributes(uint localEdgeId)
//        {
//            
//        }
//
//        private uint AddOrGetString(string s)
//        {
//            for (uint i = 0; i < _nextStringId; i++)
//            {
//                var existing = _strings[i];
//                if (existing == s) return i;
//            }
//
//            if (_strings.Length <= _nextStringId)
//            {
//                _strings.Resize(_strings.Length + 256);
//            }
//
//            var id = _nextStringId;
//            _nextStringId++;
//            
//            _strings[id] = s;
//            return id;
//        }
//    }
//}