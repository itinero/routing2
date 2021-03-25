using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Instructions.Types;
using Itinero.Routes;
[assembly: InternalsVisibleTo("Itinero.Tests.Instructions")]
namespace Itinero.Instructions
{
    /// <summary>
    /// A route which has the capabilities to generate instructions.
    /// </summary>
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

        public Route MergeInstructions(params (string key, string language)[] add)
        {
            return this.MergeInstructions(add.ToList());
        }

        /// <summary>
        ///     Generates the instructions and adds the respective texts for the respective languages, as a shapemeta-tag.
        ///     The texts will be added to _all_ the underlying shapeMetas of the route.
        ///     Note that 'distance' and 'Time' are NOT set
        /// </summary>
        /// <returns>A route where every segment will contains a shapemeta `key="instruction text for language`</returns>
        /// <remarks>
        ///     This returns a new route-object, which is a shallow copy of the previous route object except for the
        ///     'shapeMeta'-objects which are freshly constructed
        /// </remarks>
        public Route MergeInstructions(IEnumerable<(string key, string language)> add)
        {
            var instructions = this.GenerateInstructions().ToArray();

            var metas = this.MergeShapeMetas(instructions, add, _route.ShapeMeta);

            return new Route {
                Attributes = _route.Attributes,
                Branches = _route.Branches,
                Profile = _route.Profile,
                Shape = _route.Shape,
                Stops = _route.Stops,
                TotalDistance = _route.TotalDistance,
                TotalTime = _route.TotalTime,
                ShapeMeta = metas
            };
        }

        /// <summary>
        /// Method extracted for testing.
        /// </summary>
        /// <param name="instructions">The instructions of which the texts should be added</param>
        /// <param name="add">THe languages to translate the instructions into + the respective tag to add them</param>
        /// <param name="shapeMetas">The shapeMetas to merge with - in production the same as _route.ShapeMeta</param>
        /// <returns></returns>
        internal List<Route.Meta> MergeShapeMetas(
            IReadOnlyList<BaseInstruction> instructions,
            IEnumerable<(string key, string language)> add,
            IReadOnlyList<Route.Meta> shapeMetas)
        {
            
            // It turns out that 'instructions' and 'shapeMeta' might have different boundaries - so reusing the old shapeMeta's is not possible
            var metas = new List<Route.Meta>();
            var texts = instructions.Select(instr =>
                add.Select(kv => {
                    var toText = _generator.ToText[kv.language];
                    return (kv.key, toText.ToText(instr));
                }).ToArray()
            ).ToArray();

            var instructionPointer = 0;
            var shapeMetaPointer = 0;
            var routeCount = shapeMetas.Last().Shape;

            Route.Meta? lastMeta = null;
            while ((lastMeta?.Shape ?? 0) < routeCount && instructionPointer < instructions.Count &&
                   shapeMetaPointer < shapeMetas.Count) {
                var currentMeta = shapeMetas[shapeMetaPointer];
                var currentInstruction = instructions[instructionPointer];
                var currentTexts = texts[instructionPointer];


                var latestIncludedPoint = Math.Min(
                    currentMeta.Shape,
                    currentInstruction.ShapeIndexEnd);

                var attributes = currentTexts.Concat(currentMeta.Attributes);
                var distance = _route.DistanceBetween(lastMeta?.Shape ?? 0, latestIncludedPoint);
                var speed = currentMeta.Distance / currentMeta.Time;

                var meta = new Route.Meta {
                    Shape = latestIncludedPoint,
                    AttributesAreForward = currentMeta.AttributesAreForward,
                    Attributes = attributes,
                    Profile = currentMeta.Profile,
                    Distance = distance,
                    Time = speed * distance
                };


                if (currentMeta.Shape == meta.Shape) {
                    shapeMetaPointer++;
                }

                if (currentInstruction.ShapeIndexEnd == meta.Shape) {
                    instructionPointer++;
                }

                metas.Add(meta);
                lastMeta = meta;
            }

            return metas;
        }

        /// <summary>
        ///     Generates the instructions for the given language and adds it as shapemeta-tag
        ///     The texts will be added to _all_ the underlying shapeMetas of the route.
        /// </summary>
        /// <returns>A route where every segment will contains a shapemeta `key="instruction text for language`</returns>
        /// <param name="key">The key of the applied shapemeta</param>
        /// <param name="language">
        ///     The language for which to calculate.
        /// </param>
        public Route MergeInstructions(string key = "instruction", string language = "en")
        {
            return this.MergeInstructions((key, language));
        }


        /// <summary>
        ///     Generates all the instructions for this route - mostly used if direct access to the underlying instruction objects is needed.
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