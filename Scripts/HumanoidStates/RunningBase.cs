using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class RunningBase(string stateName, Humanoid player, StateType priorState, float kP = 7000f)
    : Moving(stateName, player, priorState, 741.6f, kP, 100f)
{
    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

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