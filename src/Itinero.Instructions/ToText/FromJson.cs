using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Itinero.Instructions.Generators;

namespace Itinero.Instructions.ToText
{
    internal class FromJson
    {
        private static readonly Regex RenderValueRegex =
            new Regex(@"^(\${[.+-]?[a-zA-Z0-9_]+}|\$[.+-]?[a-zA-Z0-9_]+|[^\$]+)*$");

        private static readonly List<(string, Predicate<(string a, string b)>)> Operators =
            new List<(string, Predicate<(string a, string b)>)> {
                // This is a list, as we first need to match '<=' and '>=', otherwise we might think the match is "abc<" = "def", not "abc" <= "def
                ("<=", t => BothDouble(t, d => d.a <= d.b)),
                (">=", t => BothDouble(t, d => d.a >= d.b)),
                ("!=", t => t.a != t.b),
                ("=", t => t.a == t.b),
                ("<", t => BothDouble(t, d => d.a < d.b)),
                (">", t => BothDouble(t, d => d.a > d.b))
            };


        /**
         * Parses the full pipeline
         */
        public static (LinearInstructionGenerator generator, Dictionary<string, IInstructionToText> toTexts)
            ParseRouteToInstructions(JsonElement jobj)
        {
            var generators = jobj.GetProperty("generators").EnumerateArray().Select(v => v.GetString()).ToList();
            var generator = new LinearInstructionGenerator(generators);
            var languages = jobj.GetProperty("languages");

            var toTexts = new Dictionary<string, IInstructionToText>();
            foreach (var obj in languages.EnumerateObject()) {
                var langCode = obj.Name;
                var wholeToText = new Box<IInstructionToText>();
                var whole = ParseInstructionToText(obj.Value, wholeToText,
                    new Dictionary<string, IInstructionToText>(), "/");
                wholeToText.Content = whole;
                toTexts[langCode] = whole;
            }

            return (generator, toTexts);
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
        public static IInstructionToText ParseInstructionToText(JsonElement jobj,
            Box<IInstructionToText> wholeToText = null,
            Dictionary<string, IInstructionToText> extensions = null, string context = "")
        {
            extensions ??= new Dictionary<string, IInstructionToText>();
            var conditions = new List<(Predicate<BaseInstruction>, IInstructionToText)>();
            var lowPriority = new List<(Predicate<BaseInstruction>, IInstructionToText)>();

            foreach (var obj in jobj.EnumerateObject()) {
                var key = obj.Name;
                var value = obj.Value;
                
                if (key == "extensions") {
                    var extensionsSource = obj.Value;
                    foreach (var ext in extensionsSource.EnumerateObject()) {
                        extensions.Add(ext.Name,
                            ParseSubObj(ext.Value, context + ".extensions." + ext.Name, extensions, wholeToText)
                        );
                    }

                    continue;
                }

                var (p, isLowPriority) = ParseCondition(key, wholeToText, context + "." + key, extensions);
                var sub = ParseSubObj(value, context + "." + key, extensions, wholeToText);
                (isLowPriority ? lowPriority : conditions).Add((p, sub));
            }

            return new ConditionalToText(conditions.Concat(lowPriority).ToList(), context);
        }

        private static IInstructionToText ParseSubObj(JsonElement j, string context,
            Dictionary<string, IInstructionToText> extensions, Box<IInstructionToText> wholeToText)
        {
            if (j.ValueKind == JsonValueKind.String) {
                return ParseRenderValue(j.GetString(), extensions, wholeToText, context);
            }

            return ParseInstructionToText(j, wholeToText, extensions, context);
        }

        private static bool BothDouble((string a, string b) t, Predicate<(double a, double b)> p)
        {
            if (double.TryParse(t.a, out var a) && double.TryParse(t.b, out var b)) {
                return p.Invoke((a, b));
            }

            return false;
        }

        public static (Predicate<BaseInstruction> predicate, bool lowPriority) ParseCondition(string condition,
            Box<IInstructionToText> wholeToText = null,
            string context = "",
            Dictionary<string, IInstructionToText> extensions = null)
        {
            if (condition == "*") {
                return (_ => { return true; }, true);
            }

            if (condition.IndexOf("&", StringComparison.Ordinal) >= 0) {
                var cs = condition.Split("&")
                    .Select((condition1, i) => ParseCondition(condition1, wholeToText, context + "&" + i, extensions))
                    .Select(t => t.predicate);
                return (instruction => cs.All(p => p.Invoke(instruction)), false);
            }

            foreach (var (key, op) in Operators) {
                // We iterate over all the possible operator keywords: '=', '<=', '>=', ...
                // If they are found, we apply the actual operator
                if (condition.IndexOf(key, StringComparison.Ordinal) < 0) {
                    continue;
                }

                // Get the two parts of the condition...
                var parts = condition.Split(key).Select(renderValue =>
                        ParseRenderValue(renderValue, extensions, wholeToText, context + "." + key, false))
                    .ToList();
                if (parts.Count() != 2) {
                    throw new ArgumentException("Parsing condition " + condition +
                                                " failed, it has an operator, but to much matches. Maybe you forgot to add an '&' between the conditions?");
                }

                // And apply the instruction on it!
                // We pull the instruction from thin air by returning a lambda instead
                return (
                    instruction => { return op.Invoke((parts[0].ToText(instruction), parts[1].ToText(instruction))); },
                    false);
            }

            // At this point, the condition is a single string
            // This could either be a type matching or a substitution that has to exist
            var rendered = ParseRenderValue(condition, extensions, wholeToText, context, false);
            if (rendered.SubstitutedValueCount() > 0) {
                return (instruction => { return rendered.ToText(instruction) != null; }, false);
            }

            return (instruction => { return instruction.Type == condition; }, false);
        }

        public static SubstituteText ParseRenderValue(string value,
            Dictionary<string, IInstructionToText> extensions = null,
            Box<IInstructionToText> wholeToText = null,
            string context = "",
            bool crashOnNotFound = true)
        {
            var parts = RenderValueRegex.Match(value).Groups[1].Captures
                    .Select(m => {
                        var v = m.Value;
                        if (v.StartsWith("$")) {
                            v = v.Substring(1).Trim('{', '}').ToLower();
                            return (v, true);
                        }

                        return (m.Value, false);
                    }).ToList()
                ;
            return new SubstituteText(parts, wholeToText, context, extensions, crashOnNotFound);
        }
    }
}