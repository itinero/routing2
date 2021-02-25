using Neo.IronLua;

namespace Itinero.Profiles.Lua
{
    internal static class LuaTableExtensions
    {
        internal static double? GetDouble(this LuaTable table, string key)
        {
            var obj = table[key];
            if (obj == null) {
                return null;
            }

            if (obj is double d) {
                return d;
            }

            if (obj is int i) {
                return i;
            }

            return (double) obj;
        }
        
        internal static bool? GetBoolean(this LuaTable table, string key)
        {
            var obj = table[key];
            if (obj == null) {
                return null;
            }

            if (obj is bool b) {
                return b;
            }

            return (bool) obj;
        }
    }
}