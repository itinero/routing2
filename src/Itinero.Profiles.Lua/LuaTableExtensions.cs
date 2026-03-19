using MoonSharp.Interpreter;

namespace Itinero.Profiles.Lua;

internal static class LuaTableExtensions
{
    internal static double? GetDouble(this Table table, string key)
    {
        var val = table.Get(key);
        if (val.IsNil() || val.IsVoid())
        {
            return null;
        }

        return val.CastToNumber();
    }

    internal static bool? GetBoolean(this Table table, string key)
    {
        var val = table.Get(key);
        if (val.IsNil() || val.IsVoid())
        {
            return null;
        }

        if (val.Type == DataType.Boolean)
        {
            return val.Boolean;
        }

        return val.CastToBool();
    }
}
