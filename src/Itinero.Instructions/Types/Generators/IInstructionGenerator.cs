namespace Itinero.Instructions.Types.Generators;

/// <summary>
/// Abstract representation of generator that generates individual instructions.
/// </summary>
public interface IInstructionGenerator
{
    /// <summary>
    /// An instructionGenerator will attempt to create an instruction for the given route at the given location.
    ///
    /// This should be interpreted as:
    /// given that the traveller is following the given route and approaching the end of the specified segment, what should they do at the end?
    ///
    /// It is up to the implementing IInstructionGenerator to generate an appropriate instruction - if any _is_ appropriate.
    /// Some generators will be highly specialized and give an instruction in rare cases (e.g. roundabouts),
    /// other generators are generic and will always give a result.
    ///
    /// Note that some instructions e.g. 'followAlong' thus have to leave one segment 'free' for the 'take a turn'-instruction
    ///
    /// </summary>
    /// <param name="route"></param>
    /// <param name="offset"></param>
    /// <returns>The appropriate instruction; null otherwise</returns>
    BaseInstruction? Generate(IndexedRoute route, int offset);

    /// <summary>
    /// The the name of this generator.
    /// </summary>
    string Name { get; }
}
