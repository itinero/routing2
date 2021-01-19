using System;
using System.Collections.Generic;
using Itinero.Instructions.Generators;
using Itinero.Instructions.ToText;
using Xunit;

namespace Itinero.Tests.Instructions {
    public class InstructionToTextTest {
        [Fact]
        public void SimpleInstructionToText_BaseInstruction_GeneratesText() {
            var basicTurnInstruction = new SubstituteText(
                new[] {
                    ("Turn ", false),
                    ("TurnDegrees", true),
                    (" degrees", false)
                });
            var baseInstruction = new BaseInstruction(null, 0, 42);

            var result = basicTurnInstruction.ToText(baseInstruction);
            Assert.Equal("Turn 42 degrees", result);
        }

        [Fact]
        public void SubstituteText_WithExtensions_ExtensionGenerated() {
            var extensions = new Dictionary<string, IInstructionToText> {
                {
                    "direction", new ConditionalToText(
                        new List<(Predicate<BaseInstruction>, IInstructionToText)> {
                            (i => i.TurnDegrees > 0, new SubstituteText("left")),
                            (i => i.TurnDegrees < 0, new SubstituteText("right")),
                            (_ => true, new SubstituteText("rehtendoare"))
                        }
                    )
                }
            };


            var compound = new SubstituteText(
                new[] {
                    ("Turn ", false),
                    ("direction", true)
                },
                null,
                "during a unit test",
                extensions
            );
            var left = compound.ToText(new BaseInstruction(null, 0, 90));
            Assert.Equal("Turn left", left);
            
            var right = compound.ToText(new BaseInstruction(null, 0, -90));
            Assert.Equal("Turn right", right);
            var straight = compound.ToText(new BaseInstruction(null, 0, 0));
            Assert.Equal("Turn rehtendoare", straight);
        }
    }
}