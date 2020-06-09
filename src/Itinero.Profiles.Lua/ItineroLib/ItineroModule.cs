﻿using System.Text;

namespace Itinero.Profiles.Lua.ItineroLib
{
    /// <summary>
    /// Class implementing itinero Lua functions 
    /// </summary>
    [MoonSharpModule(Namespace = "itinero")]
    internal class ItineroModule
    {
        [MoonSharpModuleMethod]
        internal static DynValue parseweight(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue weightstring = args.AsType(0, "parseweight", DataType.String, false);

            float weight;
            if (!Parsers.TryParseWeight(weightstring.String, out weight))
            {
                return DynValue.Nil;
            }
            return DynValue.NewNumber(weight);
        }

        [MoonSharpModuleMethod]
        internal static DynValue parsewidth(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue weightstring = args.AsType(0, "parsewidth", DataType.String, false);

            float weight;
            if (!Parsers.TryParseLength(weightstring.String, out weight))
            {
                return DynValue.Nil;
            }
            return DynValue.NewNumber(weight);
        }

        [MoonSharpModuleMethod]
        internal static DynValue parsespeed(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue weightstring = args.AsType(0, "parsespeed", DataType.String, false);

            float weight;
            if (!Parsers.TryParseSpeed(weightstring.String, out weight))
            {
                return DynValue.Nil;
            }
            return DynValue.NewNumber(weight);
        }

        [MoonSharpModuleMethod]
        internal static DynValue log(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (args[0] == null ||
                args[0].IsNil())
            {
                Itinero.Logging.Logger.Log("Lua", Logging.TraceEventType.Information, "nil");

                return DynValue.NewBoolean(true);
            }
            if (args[0].Type == DataType.Table)
            {
                var table = args[0].Table;
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                foreach (var key in table.Keys)
                {
                    stringBuilder.Append("(");
                    stringBuilder.Append(key.String);
                    stringBuilder.Append("=");
                    stringBuilder.Append(table[key]);
                    stringBuilder.Append(")");
                }

                stringBuilder.Append("]");
                Itinero.Logging.Logger.Log("Lua", Logging.TraceEventType.Information, stringBuilder.ToString());

                return DynValue.NewBoolean(true);
            }

            if (args[0].Type == DataType.Boolean)
            {
                if (args[0].Boolean)
                {
                    Itinero.Logging.Logger.Log("Lua", Logging.TraceEventType.Information, "true");
                }
                Itinero.Logging.Logger.Log("Lua", Logging.TraceEventType.Information, "false");

                return DynValue.NewBoolean(true);
            }
            
            DynValue text = args.AsType(0, "log", DataType.String, false);

            Itinero.Logging.Logger.Log("Lua", Logging.TraceEventType.Information, text.String);
            return DynValue.NewBoolean(true);
        }

        [MoonSharpModuleMethod]
        internal static DynValue format(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            var text = args.AsType(0, "format", DataType.String, false);
            var formatArgs = new string[args.Count - 1];
            for(var i = 1; i < args.Count;i++)
            {
                var formatArg = args.AsType(i, "format", DataType.String, false);
                formatArgs[i - 1] = formatArg.String;
            }

            return DynValue.NewString(string.Format(text.String, formatArgs));
        }
    }
}