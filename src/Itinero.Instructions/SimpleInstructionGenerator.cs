using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Instructions;

namespace Itinero.Instructions
{
    public class SimpleInstructionGenerator : IInstructionGenerator
    {
        private readonly IEnumerable<IInstructionConstructor> _constructors;

        public SimpleInstructionGenerator(
            IEnumerable<IInstructionConstructor> constructors)
        {
            _constructors = constructors;
        }

        public SimpleInstructionGenerator(
            params IInstructionConstructor[] constructors) :
            this(constructors.ToList())
        {
        }

        private BaseInstruction ConstructNext(IndexedRoute r, int currentOffset, out int used)
        {
            foreach (var constructor in _constructors)
            {
                var instruction = constructor.Construct(r, currentOffset, out used);
                if (instruction != null && used > 0)
                {
                    return instruction;
                }

                if (instruction != null && used == 0)
                {
                    throw new Exception(
                        "Hanging instruction generation: an instruction was emitted but the offset was zero. This is a bug in " +
                        constructor.Name);
                }
            }

            throw new Exception("Could not generate instruction");
        }

        public IEnumerable<BaseInstruction> GenerateInstructions(Route route)
        {
            var indexedRoute = new IndexedRoute(route);
            var instructions = new List<BaseInstruction>();

            instructions.Add(new StartInstruction(indexedRoute));


            var currentIndex = 0;
            while (currentIndex < route.Shape.Count - 1)
            {
                var instruction = ConstructNext(indexedRoute, currentIndex, out var used);
                instructions.Add(instruction);
                currentIndex += used;
            }

            instructions.Add(new EndInstruction(indexedRoute));
            return instructions;
        }
    }
}