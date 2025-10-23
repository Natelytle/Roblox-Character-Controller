using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class FallingBase(string stateName, Humanoid player, StateType priorState, float kP = 5000f)
    : Moving(stateName, player, priorState, 143.0f, kP, 100f)
{
    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

        Vector3 playerHeading = Player.Heading;
        float angle = playerHeading.SignedAngleTo(Player.MoveDirection, Vector3.Up);
        float desiredRotationalVelocity = float.Abs(angle) > 1 ? 8.0f * float.Sign(angle) : 8.0f * angle;

        float desiredTorque = 100f * Player.GetInertia().Y * (desiredRotationalVelocity - Player.AngularVelocity.Y);
	
        Player.ApplyTorque(Vector3.Up * desiredTorque);
    }
}