using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Balancing(string stateName, Humanoid player, StateType oldState, float kP, float kD)
    : HumanoidState(stateName, player, oldState)
{
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
        Vector3 playerUp = Player.GlobalTransform.Basis.Y;
        Vector3 tiltWorld = Vector3.Up.Cross(playerUp);
        Vector3 angVelWorld = Player.AngularVelocity;

        Basis rootBasis = Player.GlobalTransform.Basis;

        Vector3 tiltLocal = tiltWorld * rootBasis;
        Vector3 angVelLocal = angVelWorld * rootBasis;

        Vector3 inertiaVector = Player.GetInertia();

        Vector3 torqueLocal = -kP * (inertiaVector * tiltLocal) - kD * (inertiaVector * angVelLocal);

        Vector3 appliedTorque = rootBasis * torqueLocal;

        // We don't want to add torque counter to our player turning when walking.
        appliedTorque.Y = 0;

        Player.ApplyTorque(appliedTorque);
    }
}