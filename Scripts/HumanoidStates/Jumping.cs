using System;
using Godot;
using static RobloxCharacterController.Scripts.Humanoid;

namespace RobloxCharacterController.Scripts.HumanoidStates;

public class Jumping(Humanoid player, StateType priorState) 
    : HumanoidState("Jumping", player, priorState)
{
    private Vector3 _jumpDirection;
    private readonly StateType _priorState = priorState;

    private int jumpFrames;

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

        Timer = 0.5;

        jumpFrames = 0;
    }

    public override void OnExit()
    {
    }

    public override void Process(double delta)
    {
    }

    public override void PhysicsProcess(double delta)
    {
        float jumpVelocity = Player.LinearVelocity.Dot(_jumpDirection);
        float desiredVelocity = 53;
        
        float yAccelDesired = float.Round(1 / (float)delta) * (desiredVelocity - jumpVelocity);
        
        if (Player.HittingCeiling)
            InvokeFinished(this, StateType.Falling);
        else if (yAccelDesired <= 0)
            InvokeFinished(this, StateType.Falling);
        else if (ComputeEvent(EventType.TimerUp))
            InvokeFinished(this, StateType.Falling);
        else if (ComputeEvent(EventType.OffFloor))
            InvokeFinished(this, StateType.Falling);
        else
        {
            Player.ApplyCentralForce(-Player.GetGravity() * Player.Mass);
            GodotObject? floorPart = Player.FloorPart;

            if (floorPart is not null)
            {
                float newForceY = Player.Mass * yAccelDesired;
                float currentForceY = Player.CurrentForce.Y;

                if (newForceY > currentForceY)
                {
                    float diff = newForceY - currentForceY;
                    
                    Player.ApplyCentralForce(_jumpDirection * newForceY);

                    if (floorPart is RigidBody3D rigid)
                    {
                        rigid.ApplyForce(-_jumpDirection * diff, Player.FloorLocation - rigid.GlobalPosition);
                    }
                }
            }
            else
            {
                float newForceY = Player.Mass * yAccelDesired;

                Player.ApplyCentralForce(_jumpDirection * newForceY);
            }
        }
    }
}