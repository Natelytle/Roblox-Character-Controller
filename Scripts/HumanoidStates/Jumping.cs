using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Jumping(Humanoid player, StateType priorState) 
    : HumanoidState("Jumping", player, priorState)
{
    private Vector3 _jumpDirection;
    private readonly StateType _priorState = priorState;

    public override void OnEnter()
    {
        if (_priorState is StateType.Climbing or StateType.StandClimbing)
        {
            Vector3 backwardsVector = -Player.Heading;

            _jumpDirection = (Vector3.Up + backwardsVector).Normalized();
        }
        else
        {
            _jumpDirection = Vector3.Up;
        }
    }

    public override void OnExit()
    {
    }

    public override void Process(double delta)
    {
    }

    public override void PhysicsProcess(double delta)
    {
        Player.SetAxisVelocity(_jumpDirection * Player.JumpPower);

        InvokeFinished(this, StateType.Falling);
    }
}