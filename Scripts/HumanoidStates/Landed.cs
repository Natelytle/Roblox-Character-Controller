using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Landed(Humanoid player, StateType priorState) 
    : RunningBase("Landed", player, priorState, kP: 15000f)
{
    public override void OnEnter()
    {
        base.OnEnter();

        Timer = 0.05;
    }

    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);
        
        // Transition to other states
        if (ComputeEvent(EventType.TimerUp))
        {
            InvokeFinished(this, StateType.Running);
        }
    }
}