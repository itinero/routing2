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
        private static readonly Regex RenderValueRegex = new Regex(@"^(\${[^ ]*}|\$[^ ]*|[^\$]+)*$");

        private static readonly List<(string, Predicate<(string a, string b)>)> Operators =
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
         * Parses the full pipeline
         */
        public static (Instructions.LinearInstructionGenerator generators, Dictionary<string, IInstructionToText> toTexts) ParseRouteToInstructions(JObject jobj)
        {
            var generators = new Instructions.LinearInstructionGenerator(jobj["generators"].ToObject<List<string>>());
            var languages = jobj["languages"] as JObject;
            if (languages == null) throw new ArgumentException("JObject does not contain a languages object");

            var toTexts = new Dictionary<string, IInstructionToText>();
            foreach (var (langCode, toText) in languages) {
                toTexts[langCode] = ParseInstructionToText(toText as JObject);
            }

            return (generators, toTexts);
        }

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
         * "extensions": is a special key. The containing object's top levels have keys which are calculated and 'injected' into the instruction and can be used as a value to substitute. This is ideal to encode 'left', 'right', 'slightly left', ...
         *
         * "$someVariable": some variable has to exist.
         *
         * "condition1&condition2": all the conditions have to match
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
         *
         * Other notes:
         * A POSITIVE angle is going left,
         * A NEGATIVE angle is going right
         */
        public static IInstructionToText ParseInstructionToText(JObject jobj)
        {
            var conditions = new List<(Predicate<BaseInstruction>, IInstructionToText)>();
            var lowPriority = new List<(Predicate<BaseInstruction>, IInstructionToText)>();

            var extensions = new Dictionary<string, IInstructionToText>();
            
            
            foreach (var (key, value) in jobj)
            {
                if (key == "extensions") {

                    var extensionsSource = (JObject) value;
                    foreach (var (extKey, extValue) in extensionsSource) {
                        extensions.Add(extKey, 
                            ParseSubObj(extValue, extensions)
                            );
                    }
                    continue;
                }
                
                
                var (p, isLowPriority) = ParseCondition(key, extensions);
                var sub = ParseSubObj(value, extensions);
                (isLowPriority ? lowPriority : conditions).Add((p, sub));
            }

            return new ConditionalToText(conditions.Concat(lowPriority).ToList());
        }

        private static IInstructionToText ParseSubObj(JToken j, Dictionary<string, IInstructionToText> extensions)
        {
            if (j.Type == JTokenType.String) return ParseRenderValue(j.Value<string>(), extensions);

            if (j is JObject o) return ParseInstructionToText(o);

            throw new ArgumentException("Invalid value in ParseSubObj" + j);
        }

        private static bool BothDouble((string a, string b) t, Predicate<(double a, double b)> p)
        {
            if (double.TryParse(t.a, out var a) && double.TryParse(t.b, out var b)) return p.Invoke((a, b));
            return false;
        }

        public static (Predicate<BaseInstruction> predicate, bool lowPriority) ParseCondition(string condition,
            Dictionary<string, IInstructionToText> extensions = null)
        {
            if (condition == "*") return (_ => true, true);

            if (condition.IndexOf("&", StringComparison.Ordinal) >= 0)
            {
                var cs = condition.Split("&")
                    .Select(condition1 => ParseCondition(condition1, extensions))
                    .Select(t => t.predicate);
                return (instruction => cs.All(p => p.Invoke(instruction)), false);
            }

            foreach (var (key, op) in Operators)
            {
                // We iterate over all the possible operator keywords: '=', '<=', '>=', ...
                // If they are found, we apply the actual operator
                if (condition.IndexOf(key, StringComparison.Ordinal) < 0) continue;

                // Get the two parts of the condition...
                var parts = condition.Split(key).Select(renderValue => ParseRenderValue(renderValue, extensions, false))
                    .ToList();
                if (parts.Count() != 2)
                    throw new ArgumentException("Parsing condition " + condition +
                                                " failed, it has an operator, but to much matches. Maybe you forgot to add an '&' between the conditions?");
                // And apply the instruction on it!
                // We pull the instruction from thin air by returning a lambda instead
                return (instruction => op.Invoke((parts[0].ToText(instruction), parts[1].ToText(instruction))), false);
            }

            // At this point, the condition is a single string
            // This could either be a type matching or a substitution that has to exist
            var rendered = ParseRenderValue(condition, extensions, false);
            if (rendered.SubstitutedValueCount() > 0) {
                return (instruction => rendered.ToText(instruction) != null, false);
            }

            return (instruction => instruction.Type == condition, false);
        }

        public static SubstituteText ParseRenderValue(string value, Dictionary<string, IInstructionToText> extensions,  bool crashOnNotFound = true)
        {
            var parts = RenderValueRegex.Match(value).Groups[1].Captures
                    .Select(m =>
                    {
                        var v = m.Value;
                        if (v.StartsWith("$"))
                        {
                            v = v.Substring(1).Trim('{', '}').ToLower();
                            return (v, true);
                        }

                        return (m.Value, false);
                    }).ToList()
                ;
            if (parts.Count == 0) throw new Exception("Could not parse value " + value);

            return new SubstituteText(parts, extensions, crashOnNotFound);
        }
    }
}