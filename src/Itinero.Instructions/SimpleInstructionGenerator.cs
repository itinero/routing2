using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions
{
    public class SimpleInstructionGenerator : IInstructionGenerator
    {
        public static Func<Route.Meta, IEnumerable<string>> DefaultAmendments = meta =>
        {
            var amendments = new List<string>();

            amendments.Add(meta.GetAttributeOrNull("highway"));
            if ("yes".Equals(meta.GetAttributeOrNull("cyclestreet")))
            {
                amendments.Add("cyclestreet");
            }


            return amendments;
        };

        private readonly Func<Route.Meta, IEnumerable<string>> _amendmentsCreator;

        public SimpleInstructionGenerator(
            Func<Route.Meta, IEnumerable<string>> amendmentsCreator)
        {
            _amendmentsCreator = amendmentsCreator;
        }

        public Instruction GenerateStartInstruction(Route route)
        {
            var startPoint = route.Stops[0];
            var snappedPoint = route.Shape[0];
            var nextPoint = route.Shape[1];
            var meta = route.ShapeMeta[0];
            
            /*var projectionDistance = Utils.DistanceEstimateInMeter
                (startPoint.Coordinate, snappedPoint);*/

            var snappingDirection = Utils.AngleBetween(startPoint.Coordinate, snappedPoint);
            var newDirection = Utils.AngleBetween(snappedPoint, nextPoint);

            return new AmendedInstruction(
                new Turn(0, newDirection - snappingDirection, meta.GetAttributeOrNull("name")),
                "start");
        }

        public Instruction GenerateStopInstruction(Route route)
        { var l = route.Shape.Count;

            var endPoint = route.Stops.Last();
            var snappedPoint = route.Shape[l - 1];
            var previousPoint = route.Shape[l - 2];

            var snappingDirection = Utils.AngleBetween(endPoint.Coordinate, snappedPoint);
            var prevDirection = Utils.AngleBetween(snappedPoint, previousPoint);

            return new AmendedInstruction(
                new Turn((uint) (l - 1), snappingDirection - prevDirection),
                "end");
        }


        public IEnumerable<Instruction> GenerateInstructions(Route route)
        {
            var instructions = new List<Instruction>();

            var start = GenerateStartInstruction(route);
            instructions.Add(start);


            var metas = route.MetaList();
            var allBranches = route.GetBranchesList();
            for (var i = 1; i < route.Shape.Count - 1; i++)
            {
                var meta = metas[i];
                var branches = allBranches[i];

                var directionBefore = Utils.AngleBetween(route.Shape[i - 1], route.Shape[i]);
                var directionAfter = Utils.AngleBetween(route.Shape[i], route.Shape[i + 1]);
                var turn = new Turn((uint) i, directionAfter - directionBefore, meta.GetAttributeOrNull("name"));

                var amendments = _amendmentsCreator(meta);

                var streetName = meta.GetAttributeOrNull("name");

                if (branches.Count == 0)
                {
                    // Follow-along
                    instructions.Add(
                        new AmendedInstruction(
                            new FollowAlong(turn,
                                meta.GetAttributeOrNull("name")),
                            amendments
                        ));
                }
                else
                {
                    instructions.Add(new AmendedInstruction(turn, amendments));
                }
            }

            instructions.Add(GenerateStopInstruction(route));

            return instructions;
        }
    }
}