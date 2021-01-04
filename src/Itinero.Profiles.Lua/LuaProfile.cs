using System;
using System.Collections.Generic;

namespace Itinero.Profiles.Lua
{
    /// <summary>
    /// Represents a dynamic routing profile that is based on a lua function.
    /// </summary>
    public class LuaProfile : Profile
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
            this.Name = dynName.String ?? throw new Exception("Dynamic profile doesn't define a name.");
        }

        /// <summary>
        /// Load profile from a raw lua script.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns>The profile.</returns>
        public static LuaProfile Load(string script)
        {
            var s = new Script();
            s.DoString(script);
            return new LuaProfile(s);
        }

        /// <inheritdoc/>
        public override string Name { get; }

        /// <inheritdoc/>
        public sealed override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
        {
            lock (_script)
            {
                // build lua table.
                _attributesTable.Clear();
                if (attributes == null)
                {
                    return EdgeFactor.NoFactor;
                }
                var hasValue = false;
                foreach (var attribute in attributes)
                {
                    hasValue = true;
                    _attributesTable.Set(attribute.key, DynValue.NewString(attribute.value));
                }
                if (!hasValue) return EdgeFactor.NoFactor;

                // call factor function.
                _resultsTable.Clear();
                _script.Call(_script.Globals["factor"], _attributesTable, _resultsTable);

                // get the results.
                if (!_resultsTable.TryGetDouble("forward", out var forwardFactor))
                {
                    forwardFactor = 0;
                }
                if (!_resultsTable.TryGetDouble("backward", out var backwardFactor))
                {
                    backwardFactor = 0;
                }
                if (!_resultsTable.TryGetBool("canstop", out var canStop))
                {
                    canStop = backwardFactor > 0 || forwardFactor > 0;
                }
                
                // the speeds are supposed to be in m/s.
                if (!_resultsTable.TryGetDouble("forward_speed", out var speedForward))
                { // when forward_speed isn't explicitly filled, the assumption is that factors are in 1/(m/s)
                    speedForward = 0;
                    if (forwardFactor > 0)
                    { // convert to m/s.
                        speedForward =  1.0 / forwardFactor;
                    }
                }
                else
                { // when forward_speed is filled, it's assumed to be in km/h, it needs to be convert to m/s.
                    speedForward /= 3.6;
                }
                if (!_resultsTable.TryGetDouble("backward_speed", out var speedBackward))
                { // when backward_speed isn't explicitly filled, the assumption is that factors are in 1/(m/s)
                    speedBackward = 0;
                    if (backwardFactor > 0)
                    { // convert to m/s.
                        speedBackward = 1.0 / backwardFactor;
                    }
                }
                else
                { // when forward_speed is filled, it's assumed to be in km/h, it needs to be convert to m/s.
                    speedBackward /= 3.6;
                }

                return new EdgeFactor((uint)(forwardFactor * 100), (uint)(backwardFactor * 100), 
                    (ushort)(speedForward * 100), (ushort)(speedBackward * 100), canStop);
            }
        }

        /// <inheritdoc/>
        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
        {
            lock (_script)
            {
                // build lua table.
                _attributesTable.Clear();
                if (attributes == null)
                {
                    return Profiles.TurnCostFactor.Empty;
                }
                var hasValue = false;
                foreach (var attribute in attributes)
                {
                    hasValue = true;
                    _attributesTable.Set(attribute.key, DynValue.NewString(attribute.value));
                }
                if (!hasValue) return Profiles.TurnCostFactor.Empty;

                // call turn_cost_factor function.
                _resultsTable.Clear();
                _script.Call(_script.Globals["turn_cost_factor"], _attributesTable, _resultsTable);

                // get the results.
                if (!_resultsTable.TryGetDouble("factor", out var factor))
                {
                    factor = 0;
                }

                var turnCostFactor = Profiles.TurnCostFactor.Empty;
                if (factor < 0) 
                {
                    turnCostFactor = Profiles.TurnCostFactor.Binary;
                }
                else if (factor > 0)
                {
                    turnCostFactor = new TurnCostFactor((uint)(factor * 10));
                }
                return turnCostFactor;
            }
        }
    }
}