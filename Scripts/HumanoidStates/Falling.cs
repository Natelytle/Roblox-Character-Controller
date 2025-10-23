using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Falling(Humanoid player, StateType priorState)
    : FallingBase("Falling", player, priorState)
{
    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

        if (ComputeEvent(EventType.FacingLadder))
        {
            InvokeFinished(this, ComputeEvent(EventType.OnFloor) ? StateType.StandClimbing : StateType.Climbing);
        }
        else if (ComputeEvent(EventType.OnFloor))
        {
            InvokeFinished(this, StateType.Landed);
        }
    }
}