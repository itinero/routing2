using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Instructions;
using Itinero.Instructions.ToText;
using Itinero.Routes;

namespace Itinero.Instructions
{
    public class RouteToInstructions
    {
        private readonly LinearInstructionGenerator _generator;
        private readonly IInstructionToText _toText;

        public RouteToInstructions(LinearInstructionGenerator generator, IInstructionToText toText)
        {
            _generator = generator;
            _toText = toText;
        }

        public IEnumerable<(int shapeStart, int shapeEnd, string text)> CreateInstructions(Route r)
        {
            return _generator.GenerateInstructions(r)
                .Select(i => (i.ShapeIndex, i.ShapeIndexEnd, _toText.ToText(i)));
        }
    }
}