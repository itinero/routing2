using Itinero.Data.Attributes;

namespace Itinero.Profiles.Lua
{
    /// <summary>
    /// Contains extension methods related to Lua and Moonscharp.
    /// </summary>
    internal static class LuaExtensions
    {
        /// <summary>
        /// Tries to get a number as a double for the given key.
        /// </summary>
        internal static bool TryGetDouble(this Table table, string key, out double value)
        {
            var dynValue = table.Get(key);
            var number = dynValue?.CastToNumber();
            if (number.HasValue)
            {
                value = number.Value;
                return true;
            }
            value = double.MaxValue;
            return false;
        }
        
        /// <summary>
        /// Tries to get a number as a float for the given key.
        /// </summary>
        internal static bool TryGetFloat(this Table table, string key, out float value)
        {
            var dynValue = table.Get(key);
            var number = dynValue?.CastToNumber();
            if (number.HasValue)
            {
                value = (float)number.Value;
                return true;
            }
            value = float.MaxValue;
            return false;
        }

        /// <summary>
        /// Tries to get a bool for the given key.
        /// </summary>
        internal static bool TryGetBool(this Table table, string key, out bool value)
        {
            var dynValue = table.Get(key);
            if (dynValue != null && !DynValue.Nil.Equals(dynValue))
            {
                value = dynValue.CastToBool();
                return true;
            }
            value = false;
            return false;
        }

        /// <summary>
        /// Converts the given attribute collection to a lua table.
        /// </summary>
        internal static Table ToTable(this IAttributeCollection attributes, Script script)
        {
            var table = new Table(script);
            if (attributes == null)
            {
                return table;
            }
            foreach(var attribute in attributes)
            {
                table[attribute.Key] = attribute.Value;
            }
            return table;
        }
    }
}