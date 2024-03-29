﻿using System;
using Itinero.Geo;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.Types.Generators;

internal class FollowBendGenerator : IInstructionGenerator
{
    public string Name { get; } = "followbend";

    private static bool DoesFollowBend(IndexedRoute route, int shapeI, double dAngle, int angleSign)
    {
        // We aren't allowed to have branches on the inner side, to avoid confusing situations

        if (shapeI >= route.Branches.Count)
        {
            return true;
        }

        foreach (var branch in route.Branches[shapeI])
        {
            // What is the angle-difference of the branch?
            // This resembles the route.DirectionChangeAt-definition
            var selfAngle = route.ArrivingDirectionAt(shapeI);
            var branchAngle = route.Shape[shapeI].AngleWithMeridian(branch.Coordinate);
            var dBranchAngle = (selfAngle - branchAngle).NormalizeDegrees();

            // With the angle in hand, we can ask ourselves: lies it on the inner side?
            if (Math.Sign(dBranchAngle) != angleSign)
            {
                // It lies on the other side; this branch doesn't pose a problem
                continue;
            }

            // We know the signs are the same; so we pretend both are going left (aka positive)
            var dAngleAbs = Math.Abs(dAngle);
            var dBranchAbs = Math.Abs(dBranchAngle);

            // If the turning angle of the route is bigger, then the branch lies on the outer side
            if (dBranchAbs < dAngleAbs)
            {
                continue;
            }

            // At this point, we know the branch lies on _the inner side_
            // We cannot issue a simple follow bend
            return false;
        }

        return true;
    }


    public BaseInstruction? Generate(IndexedRoute route, int offset)
    {
        if (offset == 0 || offset == route.Last)
        {
            // We never have a bend at first or as last...
            return null;
        }
        // Okay folks!
        // We will be walking forward - as long as we are turning in one direction, it is fine!


        var angleDiff = route.DirectionChangeAt(offset);
        var angleSign = Math.Sign(angleDiff);
        var usedShapes = 1;
        route.Meta[offset].Attributes.TryGetValue("name", out var name);


        var totalDistance = route.DistanceToNextPoint(offset);
        // We walk forward and detect a true gentle bend:
        while (offset + usedShapes < route.Last)
        {
            var distance = route.DistanceToNextPoint(offset + usedShapes);
            if (distance > 35)
            {
                // a gentle bend must have pieces that are not too long at a time
                break;
            }

            var dAngle = route.DirectionChangeAt(offset + usedShapes);
            if (Math.Sign(route.DirectionChangeAt(offset + usedShapes)) != angleSign)
            {
                // The gentle bend should turn in the same direction as the first angle
                // Here, it doesn't have that...
                break;
            }


            if (!DoesFollowBend(route, offset + usedShapes, dAngle, angleSign))
            {
                // 
                break;
            }

            route.Meta[offset + usedShapes].Attributes.TryGetValue("name", out var newName);
            if (name != newName)
            {
                // Different street
                break;
            }

            totalDistance += distance;
            angleDiff += dAngle;
            // We keep the total angle too; as it might turn more then 180°
            // We do NOT normalize the angle
            usedShapes++;
        }


        if (usedShapes <= 2)
        {
            // A 'bend' isn't a bend if there is only one point, otherwise it is a turn...
            return null;
        }


        // A gentle bend also does turn, at least a few degrees per meter
        if (Math.Abs(angleDiff) < 45)
        {
            // There is little change - does it at least turn a bit?
            if (Math.Abs(angleDiff) / totalDistance < 2.5)
            {
                // Nope, we turn only 2.5° per meter - that isn't a lot
                return null;
            }
        }

        return new FollowBendInstruction(
            route,
            offset,
            offset + usedShapes,
            angleDiff
        );
    }
}
