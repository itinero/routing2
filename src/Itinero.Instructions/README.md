# Instruction generation

This project focusses on the generation of instructions for a given route.

## Architecture

There are little exposed classes:

0. The `BaseInstruction`, which represents a piece of the itinery: namely travel a bit and make a maneuver afterwards
1. `IInstructionGenerator` is a simple interface, with one function: `GenerateInstructions`. It takes a route and converts it in instructions. This is the _only_ entry point the routeplanner knows about. If you want to make your own instructionGenerator, implement this class.

Every `instruction`-class provides a singleton `InstructionGenerator`. An instructionGenerator is a small class which (possibly) consumes a piece of the route and emits a single instruction for that piece.

The `SimpleInstructionGenerator` takes a list of such generators. It attempts to generate an instruction for the current piece of the route. The first generator which emits an instruction is used, thus creating a cascade of options.




