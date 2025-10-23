using System;
using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class FallingBase(string stateName, Humanoid player, StateType priorState, float kP = 5000f)
    : Balancing(stateName, player, priorState, kP, 100f)
{
    private const float MaxForce = 143.0f;
    private const float Gain = 150f;

    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);
        
        Vector3 targetMovementVector = Player.MoveDirection;
        Vector3 target = targetMovementVector * Player.WalkSpeed;
        Vector3 correctionVector = target - new Vector3(Player.LinearVelocity.X, 0, Player.LinearVelocity.Z);
        correctionVector = correctionVector.Normalized() * Math.Min(MaxForce, Gain * correctionVector.Length());
        Vector3 correctionForce = correctionVector * Player.Mass;
        
        Player.ApplyCentralForce(correctionForce);

        Vector3 playerHeading = Player.Heading;
        float angle = playerHeading.SignedAngleTo(Player.MoveDirection, Vector3.Up);
        float desiredRotationalVelocity = float.Abs(angle) > 1 ? 8.0f * float.Sign(angle) : 8.0f * angle;

        float desiredTorque = 100f * Player.GetInertia().Y * (desiredRotationalVelocity - Player.AngularVelocity.Y);
	
        Player.ApplyTorque(Vector3.Up * desiredTorque);
    }
}