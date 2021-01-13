using Itinero.Instructions.Instructions;
using Itinero.Instructions.ToText;
using Xunit;

namespace Itinero.Tests.Instructions
{
    public class InstructionToTextTest
    {

        [Fact]
        public void SimpleInstructionToText_BaseInstruction_GeneratesText()
        {
            var basicTurnInstruction = new SubstituteText(
                new []
                {
                    ("Turn ", false),
                    ("TurnDegrees", true),
                    (" degrees", false)
                });
            var baseInstruction = new BaseInstruction(0, 42);

            var result = basicTurnInstruction.ToText(baseInstruction);
            Assert.Equal("Turn 42 degrees", result);

        }

    }
}