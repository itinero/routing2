# Instruction generation

This project focuses on the generation of instructions for a given route.

## Architecture

There are little exposed classes:

0. The `BaseInstruction`, which represents a piece of the itinerary: namely travel a bit and make a maneuver afterwards
1. `IInstructionGenerator` is a simple interface, with one function: `GenerateInstructions`. It takes a route and converts it in instructions. This is the _only_ entry point the routeplanner knows about. If you want to make your own instructionGenerator, implement this class.
2. The`IInstructionToText` which converts an instruction into human readable text

Every `instruction`-class provides a singleton `InstructionGenerator`. An instructionGenerator is a small class which (possibly) consumes a piece of the route and emits a single instruction for that piece.

The `SimpleInstructionGenerator` takes a list of such generators. It attempts to generate an instruction for the current piece of the route. The first generator which emits an instruction is used, thus creating a cascade of options.




    /*
     * Ideas for 'low hanging fruit':
     * Go over the bridge
     * Go through the tunnel
     *     => (how does this combine with underground crossroads, e.g. beneath koekelberg? Should we ignore small culvert bridges?
     *
     * Higher hanging fruit:
     *     This is a cyclestreet - cyclists should not be overtaken by cars
     *     Go left/right after the traffic lights
     *     Go right at the intersection (which has a speed table)
     *     Follow the blue network until you see the red network
     *     ...
     */
}