using System;
using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Moving(string stateName, Humanoid player, StateType priorState, float maxAccel, float kP, float kD)
    : Balancing(stateName, player, priorState, kP, kD)
{
    private const float Gain = 150f;

    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
    }

    public override void Process(double delta)
    {
    }

    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

        Vector3 targetMovementVector = Player.MoveDirection;
        Vector3 target = targetMovementVector * Player.WalkSpeed;
        Vector3 correctionVector = target - new Vector3(Player.LinearVelocity.X, 0, Player.LinearVelocity.Z);

        float length = Math.Min(maxAccel, Gain * correctionVector.Length());

        correctionVector = correctionVector.Normalized() * length;

        Vector3 correctionForce = correctionVector * Player.Mass;
        
        Player.ApplyCentralForce(correctionForce);
    }
}