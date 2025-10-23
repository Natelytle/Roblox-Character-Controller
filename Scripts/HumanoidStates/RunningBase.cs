using System;
using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class RunningBase(string stateName, Humanoid player, StateType priorState, float kP = 7000f)
    : Balancing(stateName, player, priorState, kP, 100f)
{
    private const float MaxForce = 741.6f;
    private const float Gain = 150f;

    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

        Vector3 targetMovementVector = Player.MoveDirection;

        if (Player.FloorNormal is not null)
        {
            Vector3 floorNormal = Player.FloorNormal.Value;
            Vector3 upSlopeMovementVector = (targetMovementVector - floorNormal * targetMovementVector.Dot(floorNormal)).Normalized();
            upSlopeMovementVector.Y = 0;

            // Bad hack blah who cares
            targetMovementVector *= upSlopeMovementVector.Length();
        }

        Vector3 target = targetMovementVector * Player.WalkSpeed;

        Vector3 correctionVector = target - new Vector3(Player.LinearVelocity.X, 0, Player.LinearVelocity.Z);
        correctionVector = correctionVector.Normalized() * Math.Min(MaxForce, Gain * correctionVector.Length());
        Vector3 correctionForce = correctionVector * Player.Mass;
        
        Player.ApplyCentralForce(correctionForce);

        const float hipHeight = 2f;
        
        float? desiredYVelocity = 27 * (hipHeight - Player.FloorDistance);

        if (desiredYVelocity != null && desiredYVelocity > 0)
        {
            Player.ApplyCentralForce(-Player.GetGravity() * Player.Mass);

            float desiredForce = 110 * (desiredYVelocity.Value - Player.LinearVelocity.Y) * Player.Mass;
        
            Player.ApplyCentralForce(Vector3.Up * desiredForce);
        }

        if (!Player.RotationLocked)
        {
            Vector3 playerHeading = Player.Heading;
            float angle = playerHeading.SignedAngleTo(Player.MoveDirection, Vector3.Up);
            float desiredRotationalVelocity = 8.0f * angle;

            float desiredTorque = 100f * Player.GetInertia().Y * (desiredRotationalVelocity - Player.AngularVelocity.Y);
            
            const float torqueMax = 1e5f;
            desiredTorque = Mathf.Clamp(desiredTorque, -torqueMax, torqueMax);
		
            Player.ApplyTorque(Vector3.Up * desiredTorque);
        }
    }
}