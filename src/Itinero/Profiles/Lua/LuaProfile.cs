using System;
using Itinero.Data.Attributes;

namespace Itinero.Profiles.Lua
{
    /// <summary>
    /// Represents a dynamic routing profile that is based on a lua function.
    /// </summary>
    internal class LuaProfile : Profile
    {
        private readonly Script _script;
        private readonly Table _attributesTable;
        private readonly Table _resultsTable;
        
        /// <summary>
        /// Creates a new dynamic profile.
        /// </summary>
        internal LuaProfile(Script script)
        {
            _script = script;
            
            _attributesTable = new Table(_script);
            _resultsTable = new Table(_script);
            
            var dynName = _script.Globals.Get("name");
            if (dynName == null)
            {
                throw new Exception("Dynamic profile doesn't define a name.");
            }
            this.Name = dynName.String;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Get a function to calculate properties for a set given edge attributes.
        /// </summary>
        /// <returns></returns>
        public sealed override EdgeFactor Factor(IAttributeCollection attributes)
        {
            lock (_script)
            {
                // build lua table.
                _attributesTable.Clear();
                if (attributes == null || attributes.Count == 0)
                {
                    return EdgeFactor.NoFactor;
                }
                foreach (var attribute in attributes)
                {
                    _attributesTable.Set(attribute.Key, DynValue.NewString(attribute.Value));
                }

                // call factor_and_speed function.
                _resultsTable.Clear();
                _script.Call(_script.Globals["factor"], _attributesTable, _resultsTable);

                // get the results.
                if (!_resultsTable.TryGetFloat("forward", out var forwardFactor))
                {
                    forwardFactor = 0;
                }
                if (!_resultsTable.TryGetFloat("backward", out var backwardFactor))
                {
                    backwardFactor = 0;
                }
                if (!_resultsTable.TryGetBool("canstop", out var canstop))
                {
                    canstop = true;
                }
                if (!_resultsTable.TryGetFloat("forward_speed", out var speedForward))
                {
                    speedForward = 0;
                    if (forwardFactor > 0)
                    {
                        speedForward =  1.0f / (forwardFactor / 3.6f); // 1/m/s
                    }
                }
                if (!_resultsTable.TryGetFloat("backward_speed", out var speedBackward))
                {
                    speedBackward = 0;
                    if (backwardFactor > 0)
                    {
                        speedBackward = 1.0f / (backwardFactor / 3.6f); // 1/m/s
                    }
                }

                return new EdgeFactor((uint)(forwardFactor * 100), (uint)(backwardFactor * 100), 
                    (uint)speedForward, (uint)speedBackward, canstop);
            }
        }
    }
}