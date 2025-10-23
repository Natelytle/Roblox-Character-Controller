using System.Numerics;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Running(Humanoid player, StateType priorState) 
    : RunningBase("Running", player, priorState)
{
    public override void OnEnter()
    {
        Player.AnimationPlayer.SetCurrentAnimation("run cycle");
    }

    public override void OnExit()
    {
        Player.AnimationPlayer.SetCurrentAnimation("default");
    }
    
    public override void PhysicsProcess(double delta)
    {
        base.PhysicsProcess(delta);

        Vector3 horizontalSpeed = new Vector3(Player.LinearVelocity.X, 0, Player.LinearVelocity.Z);
        Player.AnimationPlayer.SetSpeedScale(horizontalSpeed.Length() / 14.5f);
        
        // Transition to other states
        if (ComputeEvent(EventType.FacingLadder))
        {
            InvokeFinished(this, StateType.StandClimbing);
        }
        else if (ComputeEvent(EventType.JumpCommand))
        {
            InvokeFinished(this, StateType.Jumping);
        }
        else if (ComputeEvent(EventType.OffFloor))
        {
            InvokeFinished(this, StateType.Coyote);
        }
    }
}