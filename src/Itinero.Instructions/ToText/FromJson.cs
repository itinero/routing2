using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Itinero.Instructions.Instructions;
using Newtonsoft.Json.Linq;

namespace Itinero.Instructions.ToText
{
    public class FromJson
    {
        private static readonly Regex RenderValueRegex = new Regex(@"^(\$[^ ]*|[^\$]+)*$");

        private static readonly List<(string, Predicate<(string a, string b)>)> _operators =
            new List<(string, Predicate<(string a, string b)>)>
            {
                // This is a list, as we first need to match '<=' and '>=', otherwise we might think the match is "abc<" = "def", not "abc" <= "def
                ("<=", t => BothDouble(t, d => d.a <= d.b)),
                (">=", t => BothDouble(t, d => d.a >= d.b)),
                ("=", t => t.a == t.b),
                ("<", t => BothDouble(t, d => d.a < d.b)),
                (">", t => BothDouble(t, d => d.a > d.b))
            };

        
        
        /**
         * Parses a JSON-file and converts it into a InstructionToText.
         * This is done as following:
         * 
         * A hash is interpreted as being "condition":"rendervalue"
         * 
         * A condition is parsed in the following way:
         * 
         * "InstructionType": the 'type' of the instruction must match the given string, e.g. 'Roundabout', 'Start', 'Base', ...
         * These are the same as the classnames of 'Instruction/*.cs' (but not case sensitive and the Instruction can be dropped)
         * 
         * If the condition contains a "=","
         * <
         * ","
         * >
         * ","
         * <
         * =
         * " or "
         * >
         * =" then both parts are interpreted as renderValues and should satisfy the condition.
         * (If a substitution fails in any part, the condition is considered failed)
         * (Note that = compares the string values, whereas the comparators first parse to a double. If parsing fails, the condition fails automatically)
         * 
         * If the condition equals "*", then this is considered the 'fallback'-value. This implies that if every other condition fails, this condition is taken.
         * 
         * 
         * A rendervalue is a string such as "Take the {exitNumber}th exit", where 'exitNumber' is substituted by the corresponding field declared in the instruction.
         * If that substitution fails, the result will be null which will either cause an error in rendering or a condition to fail.
         */
        public static IInstructionToText FromJSON(JObject jobj)
        {
            var conditions = new List<(Predicate<BaseInstruction>, IInstructionToText)>();
            var lowPriority = new List<(Predicate<BaseInstruction>, IInstructionToText)>();
            foreach (var (key, value) in jobj)
            {
                var (p, isLowPriority) = ParseCondition(key);
                var sub = ParseSubObj(value);
                (isLowPriority ? lowPriority : conditions).Add((p, sub));
            }

            return new ConditionalToText(conditions.Concat(lowPriority));
        }

        private static IInstructionToText ParseSubObj(JToken j)
        {
            if (j.Type == JTokenType.String) return ParseRenderValue(j.Value<string>());

            if (j is JObject o) return FromJSON(o);

            throw new ArgumentException("Invalid value in ParseSubObj" + j);
        }

        private static bool BothDouble((string a, string b) t, Predicate<(double a, double b)> p)
        {
            if (double.TryParse(t.a, out var a) && double.TryParse(t.b, out var b)) return p.Invoke((a, b));
            return false;
        }

        public static (Predicate<BaseInstruction> predicate, bool lowPriority) ParseCondition(string condition)
        {
            if (condition == "*") return (_ => true, true);

            if (condition.IndexOf("&", StringComparison.Ordinal) >= 0)
            {
                var cs = condition.Split("&")
                    .Select(ParseCondition)
                    .Select(t => t.predicate);
                return (instruction => cs.All(p => p.Invoke(instruction)), false);
            }

            foreach (var (key, op) in _operators)
            {
                // We iterate over all the possible operator keywords: '=', '<=', '>=', ...
                // If they are found, we apply the actual operator
                if (condition.IndexOf(key, StringComparison.Ordinal) < 0) continue;

                // Get the two parts of the condition...
                var parts = condition.Split(key).Select(renderValue => ParseRenderValue(renderValue,false))
                    .ToList();
                if (parts.Count() != 2)
                    throw new ArgumentException("Parsing condition " + condition +
                                                " failed, it has an operator, but to much matches");
                // And apply the instruction on it!
                // We pull the instruction from thin air by returning a lambda instead
                return (instruction =>
                {
                    Console.WriteLine("COmparing"+key+"of"+instruction+" of parts "+parts[0]+", "+parts[1]);
                    return op.Invoke((parts[0].ToText(instruction), parts[1].ToText(instruction)));
                }, false);
            }

            // At this point, the condition is a simple type matching
            return (instruction => instruction.Type == condition, false);
        }

        public static SubstituteText ParseRenderValue(string value, bool crashOnNotFound = true)
        {
            var parts = RenderValueRegex.Match(value).Groups[1].Captures
                    .Select(m =>
                    {
                        var v = m.Value;
                        if (v.StartsWith("$")) return (v.Substring(1, v.Length - 1), true);
                        return (m.Value, false);
                    })
                ;

            return new SubstituteText(parts, crashOnNotFound);
        }
    }
}