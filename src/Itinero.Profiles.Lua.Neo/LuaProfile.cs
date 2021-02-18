using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Logging;
using Itinero.Profiles;
using Neo.IronLua;

namespace Itinero.Lua.Neo
{
    public class LuaProfile : Profile
    {
        private static readonly global::Neo.IronLua.Lua _lua = new();
        private readonly dynamic _env = _lua.CreateEnvironment();

        private readonly bool _hasTurnFactor;

        private LuaProfile(LuaChunk chunk)
        {
            _env.dochunk(chunk);
            this.Name = _env.name;
            _hasTurnFactor = _env.turn_cost_factor != null;
            if (!_hasTurnFactor) {
                Logger.Log("LuaProfile Turnfactor", TraceEventType.Warning,
                    "The profile " + this.Name + " doesn't have a turn_cost_factor defined");
            }
        }

        public override string Name { get; }

        // static LuaProfile Load
        // static LuaProfile LoadFile

        public static IReadOnlyCollection<Profile> LoadDirectory(string path)
        {
            return Directory.EnumerateFiles(path, "*.lua").Select(s => {
                try {
                    return LoadFile(s);
                }
                catch (LuaParseException e) {
                    Logger.Log("LuaProfile", TraceEventType.Critical,
                        $"Could not load lua profile at {s} [{e.Line}:{e.Column}]: {e.Message}");
                    return null;
                }
            }).Where(t => t != null).ToArray();
        }

        public static Profile LoadFile(string path)
        {
            var chunk = _lua.CompileChunk(path, new LuaCompileOptions());
            return new LuaProfile(chunk);
        }

        public static Profile Load(string scriptContents, string name = "Unnamed_lua")
        {
            var chunk = _lua.CompileChunk(scriptContents, name, new LuaCompileOptions());
            return new LuaProfile(chunk);
        }


        private static int ToInt(LuaTable table, string key)
        {
            var obj = table[key];
            if (obj == null) {
                return 0;
            }

            if (obj is double d) {
                return (int) d;
            }

            if (obj is int i) {
                return i;
            }

            return (int) obj;
        }

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
        {
            var attributesTable = new LuaTable();
            var resultTable = new LuaTable();
            foreach (var (k, v) in attributes) {
                attributesTable[k] = v;
            }

            _env.factor(attributesTable, resultTable);

            var forward = ToInt(resultTable, "forward");
            var backward = ToInt(resultTable, "backward");
            var forwardSpeed = ToInt(resultTable, "forward_speed");
            var backwardSpeed = ToInt(resultTable, "backward_speed");
            var canstop = (bool) resultTable["canstop"];
            return new EdgeFactor(
                (uint) forward,
                (uint) backward,
                (ushort) forwardSpeed,
                (ushort) backwardSpeed,
                canstop
            );
        }

        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
        {
            if (!_hasTurnFactor || attributes == null || !attributes.Any()) {
                return Profiles.TurnCostFactor.Empty;
            }

            var attributesTable = new LuaTable();
            var resultTable = new LuaTable();
            foreach (var (k, v) in attributes) {
                attributesTable[k] = v;
            }

            _env.turn_cost_factor(attributesTable, resultTable);

            var factor = 0.0;
            if (resultTable.ContainsKey("factor")) {
                factor = (double) resultTable["factor"];
            }

            var turnCostFactor = Profiles.TurnCostFactor.Empty;
            if (factor < 0) {
                turnCostFactor = Profiles.TurnCostFactor.Binary;
            }
            else if (factor > 0) {
                turnCostFactor = new TurnCostFactor((uint) (factor * 10));
            }

            return turnCostFactor;
        }
    }
}