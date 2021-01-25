using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Generators;
using Itinero.Routes;

namespace Itinero.Instructions {
    /**
     * Given a list of IInstructionConstructors, the SimpleInstructionGenerator will construct a route in the following way:
     * - Try to generate a base instruction with the first instruction in the list. If this fails, try the next one
     * - Check the shape-index of the generated instruction and move the index of the route forward
     * - Try to generate a new instruction as described above
     *
     * This means that the list of 'baseInstructionConstructors' should go from "very specialized" to "very generic", e.g.
     * the roundabout-instruction-constructor should be at the first position as that instruction will only trigger in specific circumstances;
     * whereas the 'follow the road/go left/go right' instruction will always trigger but is not very informative
     */
    internal class LinearInstructionGenerator {
        private readonly IEnumerable<IInstructionGenerator> _constructors;

        public LinearInstructionGenerator(
            IEnumerable<string> constructorNames) :
            this(constructorNames.Select(name => {
            if (AllGenerators.AllGeneratorsDict.TryGetValue(name, out var gen)) {
                return gen;
            }

            throw new Exception(("The generator " + name + "; try one of " +
                                 String.Join(", ", AllGenerators.AllGeneratorsDict.Keys)));
        })) { }

        public LinearInstructionGenerator(
            IEnumerable<IInstructionGenerator> constructors) {
            _constructors = constructors;
        }

        public LinearInstructionGenerator(
            params IInstructionGenerator[] constructors) :
            this(constructors.ToList()) { }

        private BaseInstruction ConstructNext(IndexedRoute r, int currentOffset) {
            foreach (var constructor in _constructors) {
                var instruction = constructor.Generate(r, currentOffset);
                if (instruction != null) {
                    return instruction;
                }
            }

            throw new Exception("Could not generate instruction");
        }

        public IEnumerable<BaseInstruction> GenerateInstructions(Route route) {
            var indexedRoute = new IndexedRoute(route);
            var instructions = new List<BaseInstruction>();

            var currentIndex = 0;
            while (currentIndex < indexedRoute.Last) {
                var instruction = ConstructNext(indexedRoute, currentIndex);
                instructions.Add(instruction);
                if (instruction.ShapeIndexEnd == currentIndex) {
                    currentIndex++;
                }
                else {
                    currentIndex = instruction.ShapeIndexEnd;
                }
            }

            instructions[0] = new StartInstruction(indexedRoute, instructions[0]);
            
            return instructions;
        }
    }
}