namespace Itinero.Instructions.Instructions
{
    public interface IInstructionGenerator
    {
        /// <summary>
        /// An instructionGenerator will attempt to create an instruction for the given route at the given location.
        ///
        /// This should be interpreted as:
        /// given that the traveller is following the given route and is located _at_ the start of the segment of the shapepoint,
        /// what should they do next?
        ///
        /// It is up to the implementing IInstructionGenerator to generate an appropriate instruction - if any _is_ appropriate.
        /// Some generators will be highly specialized and give an instruction in rare cases (e.g. roundabouts),
        /// other generators are generic and will always give a result.
        ///
        /// </summary>
        /// <param name="route"></param>
        /// <param name="offset"></param>
        /// <returns>The appropriate instruction; null otherwise</returns>
        BaseInstruction Generate(IndexedRoute route, int offset);
    }
}