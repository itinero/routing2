using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Types;
using Itinero.Routes;

namespace Itinero.Instructions
{
    public class RouteWithInstructions
    {
        private readonly InstructionsGenerator _generator;

        private readonly Route _route;
        private IEnumerable<BaseInstruction>? _instructions;

        internal RouteWithInstructions(Route route, InstructionsGenerator generator)
        {
            _route = route;
            _generator = generator;
        }

        /// <summary>
        ///     Generates the instructions and adds the respective texts for the respective languages, as a shapemeta-tag.
        ///     The texts will be added to _all_ the underlying shapeMetas of the route.
        /// </summary>
        /// <returns>A route where every segment will contains a shapemeta `key="instruction text for language`</returns>
        public Route MergeInstructions(IEnumerable<(string key, string language)> add)
        {
            add = add.ToArray();
            var instr = this.GenerateInstructions().ToList();
            
            var instrTexts = new List<IEnumerable<(string key, string text)>>();
            foreach (var instruction in instr) {
                var texts = add.Select(k => {
                    var toText = _generator.ToText[k.language];
                    return (k.key, toText.ToText(instruction));
                }).ToList();
                instrTexts.Add(texts);
            }

            var instructionPointer = 0;
            foreach (var shapeMeta in _route.ShapeMeta) {
                if (shapeMeta.Shape > instr[instructionPointer].ShapeIndexEnd) {
                    instructionPointer++;
                }
                shapeMeta.Attributes = instrTexts[instructionPointer].Concat(shapeMeta.Attributes);
            }

            return _route;
        }

        /// <summary>
        ///     Generates the instructions for the given language and adds it as shapemeta-tag
        ///     The texts will be added to _all_ the underlying shapeMetas of the route.
        /// </summary>
        /// <returns>A route where every segment will contains a shapemeta `key="instruction text for language`</returns>
        /// <param name="key">The key of the applied shapemeta</param>
        /// <param name="language">
        ///     The language for which to calculate. If none given and only one language is known given, this
        ///     language will be used
        /// </param>
        public Route MergeInstructions(string key, string language = "")
        {
            return this.MergeInstructions(new[] {(key, language)});
        }


        /// <summary>
        ///     Generates all the instructions for this route - mostly used if direct access is needed.
        ///     You'll probably want to use <see cref="MergeInstructions" />
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseInstruction> GenerateInstructions()
        {
            _instructions ??= _generator.Generator.GenerateInstructions(_route);
            return _instructions;
        }
    }
}