namespace Itinero.Instructions.Instructions
{
    public interface IInstructionGenerator
    {
        /// <summary>
        /// Generates an instruction for the route at the given offset.
        /// Returns the instruction and how much shape-points have been used
        ///
        ///<remarks>
        ///Returns (null, 0) if no instruction was generated with this constructor.
        /// This can be the case e.g. for a roundabout-instruction-generator when there is no roundabout.
        /// (This will never happen for the BaseInstruction, which will always use exactly one shapepoint)
        /// </remarks>
        /// </summary>
        /// <returns></returns>
        BaseInstruction Generate(IndexedRoute route, int offset, out int usedInstructions);
    }
}