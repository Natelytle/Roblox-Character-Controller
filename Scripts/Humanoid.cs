using System;
using Godot;
using RobloxCharacterController.Scripts.HumanoidStates;

namespace RobloxCharacterController.Scripts;

public partial class Humanoid : RigidBody3D
{
	[ExportGroup("Movement")] 
	[Export] public float WalkSpeed { get; private set; } = 16f;
	[Export] public float JumpPower { get; private set; } = 53f;
	[Export] public float MaxSlope { get; private set; } = 89.0f;

	private RayCast3D _ceilingRayCast = null!;
	private RayCast3D _groundRayCast = null!;
	private RayCast3D _climbRayCast = null!;
	
	private Camera _camera = null!;
	public AnimationPlayer AnimationPlayer = null!;
	
	// Movement properties
	public Vector3 MoveDirection { get; private set; }
	public Vector3 Heading { get; private set; }
	public bool RotationLocked => _camera.RotationLocked;

	// Floor properties
	public float? FloorDistance { get; private set; }
	public Vector3? FloorNormal { get; private set; }
	public Vector3? FloorHitLocation { get; private set; }
	public GodotObject? FloorPart { get; private set; }
	public PhysicsMaterial? FloorMaterial { get; private set; }
	public Vector3 CurrentForce => (_currentVelocity - _previousVelocity) * Mass;
	private Vector3 _currentVelocity = Vector3.Zero;
	private Vector3 _previousVelocity = Vector3.Zero;
	
	// Ceiling properties
	public bool HittingCeiling { get; private set; }
	
	// Climbing properties
	public bool IsClimbing { get; private set; }
	
	// State Machine
	[Export]
	public StateType InitialState { get; set; }
	
	public HumanoidState? CurrentState { get; private set; }
	private StateType _currentStateType;
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_ceilingRayCast = new RayCast3D();
		AddChild(_ceilingRayCast);

		_groundRayCast = new RayCast3D();
		AddChild(_groundRayCast);
		
		_climbRayCast = new RayCast3D();
		AddChild(_climbRayCast);
		
		_camera = (Camera)GetNode("Attachments/Camera");
		AnimationPlayer = (AnimationPlayer)GetNode("Avatar/AnimationPlayer");
		
		CurrentState = GetState(InitialState);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		CurrentState?.Process(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		MoveDirection = (GlobalBasis.Rotated(Vector3.Up, _camera.Rotation.Y - Rotation.Y) * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		Heading = new Plane(Vector3.Up).Project(-Basis.Z).Normalized();
		 
		SetFloorProperties();
		SetCeilingProperties();
		SetClimbingProperties();
		
		// Set these before we process state for a one frame delay instead of 2
		_previousVelocity = _currentVelocity;
		_currentVelocity = LinearVelocity;
		
		CurrentState?.PrePhysicsProcess(delta);
		CurrentState?.PhysicsProcess(delta);
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (!RotationLocked)
			return;
		
		float currentYaw = Rotation.Y;
		float desiredYaw = _camera.Rotation.Y;
		float angleDelta = Mathf.AngleDifference(currentYaw, desiredYaw);

		// Apply rotation to the physics state's transform
		Transform3D currentTransform = state.Transform;
		currentTransform.Basis = currentTransform.Basis.Rotated(Vector3.Up, angleDelta);
		state.Transform = currentTransform;

		int sign = float.Sign(angleDelta);
		float collisionAngularY = -sign * float.Max(angleDelta * 50.0f * float.Sign(sign), state.AngularVelocity.Y * float.Sign(angleDelta));
		var angularVelocity = state.AngularVelocity;
		angularVelocity.Y = collisionAngularY;
		state.AngularVelocity = angularVelocity;
	}

	private void SetFloorProperties()
	{
		float[] xPositions = [0, 0.8f, -0.8f];
		float[] zPositions = [0, -0.45f, 0.45f];
		const float yPosition = -0.9f;
		
		// Get the raycast length depending on if we had a floor last frame.
		float length = FloorDistance is not null ? 1.5f : 1.1f;
		length += Math.Abs(LinearVelocity.Y) > 100 ? Math.Abs(LinearVelocity.Y) / 100.0f : 0;
		length = length * 2 + 1;

		_groundRayCast.TargetPosition = new Vector3(0, -length, 0);
		
		// Reset floor info
		FloorDistance = null;
		FloorNormal = null;
		FloorHitLocation = null;
		FloorMaterial = null;
		FloorPart = null;

		float sum = 0;
		int count = 0;

		// Check the center, then the sides.
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				// We skip the center raycast on the sides..
				if (i > 0 && j == 0)
					continue;

				_groundRayCast.Position = new Vector3(xPositions[i], yPosition, zPositions[j]);
				_groundRayCast.ForceRaycastUpdate();

				if (_groundRayCast.IsColliding())
				{
					sum += _groundRayCast.GlobalPosition.DistanceTo(_groundRayCast.GetCollisionPoint());
					count++;

					FloorNormal ??= _groundRayCast.GetCollisionNormal();
					FloorHitLocation ??= _groundRayCast.GetCollisionPoint();
					FloorPart ??= _groundRayCast.GetCollider();

					if (FloorMaterial is null && FloorPart is not null)
					{
						FloorMaterial = FloorPart switch
						{
							StaticBody3D s => s.PhysicsMaterialOverride,
							RigidBody3D r => r.PhysicsMaterialOverride,
							_ => FloorMaterial
						};
					}
				}
			}

			if (count != 0)
				break;
		}
		
		const float zPositionSecondary = 0.85f;

		// We have 2 more checks, just do em manually
		if (sum > 0)
		{
			for (int i = -1; i < 2; i += 2)
			{
				_groundRayCast.Position = new Vector3(0, yPosition, i * zPositionSecondary);
				_groundRayCast.ForceRaycastUpdate();

				if (_groundRayCast.IsColliding())
				{
					sum += _groundRayCast.GlobalPosition.DistanceTo(_groundRayCast.GetCollisionPoint());
					count++;
				}
			}
		}

		if (count > 0)
			FloorDistance = sum / count;
	}

	private void SetCeilingProperties()
	{
		float[] xPositions = [0.8f, -0.8f];
		float[] zPositions = [-0.45f, 0.45f];
		const float yPosition = -0.9f;
		
		_ceilingRayCast.TargetPosition = new Vector3(0, 4, 0);
		
		// Reset ceiling info
		HittingCeiling = false;

		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				_ceilingRayCast.Position = new Vector3(xPositions[i], yPosition, zPositions[j]);
				_ceilingRayCast.ForceRaycastUpdate();

				if (_ceilingRayCast.IsColliding())
				{
					HittingCeiling = true;
				}
			}
		}
	}

	private void SetClimbingProperties()
	{
		const float yPositionInitial = -2.7f + 1 / 7.0f;
		const float yPositionIncrements = 1 / 7.0f;
		const float zSearchLengthTruss = 1.05f;
		const float zSearchLengthLadder = 0.7f;

		IsClimbing = false;

		// TODO: Searching for trusses
		
		// Searching for ladders
		bool hitUnderCyanRaycast = false;
		bool airOverFirstHit = false;
		int distanceOfAirFromFirstHit = 0;
		bool redRaysHit = false;
		bool secondHitExists = false;

		_climbRayCast.TargetPosition = new Vector3(0, 0, -zSearchLengthLadder);

		for (int i = 0; i < 27; i++)
		{
			_climbRayCast.Position = new Vector3(0, yPositionInitial + i * yPositionIncrements, 0);
			_climbRayCast.ForceRaycastUpdate();

			if (i < 3 && _climbRayCast.IsColliding())
			{
				redRaysHit = true;
			}

			if (i < 17 && _climbRayCast.IsColliding())
			{
				hitUnderCyanRaycast = true;
			}
			
			if (hitUnderCyanRaycast && _climbRayCast.IsColliding())
			{
				distanceOfAirFromFirstHit++;
			}

			if (hitUnderCyanRaycast && !_climbRayCast.IsColliding() && distanceOfAirFromFirstHit < 17)
			{
				airOverFirstHit = true;
			}

			if (redRaysHit && i < 26 && airOverFirstHit && _climbRayCast.IsColliding())
			{
				secondHitExists = true;
			}
		}

		IsClimbing = hitUnderCyanRaycast && airOverFirstHit && (!redRaysHit || secondHitExists);
	}

	private void UpdateHitboxes()
	{
		
	}
	
	public enum StateType
	{
		None,
		Running,
		Coyote,
		Falling,
		Climbing,
		StandClimbing,
		Jumping,
		Landed
	}

	private HumanoidState? GetState(StateType stateType)
	{
		HumanoidState state;
        
		switch (stateType)
		{
			case StateType.Running:
				state = new Running(this, _currentStateType);
				break;
			case StateType.Coyote:
				state = new Coyote(this, _currentStateType);
				break;
			case StateType.Falling:
				state = new Falling(this, _currentStateType);
				break;
			// case StateType.Climbing:
			// 	state = new Climbing(this, _currentStateType);
			// 	break;
			// case StateType.StandClimbing:
			// 	state = new StandClimbing(this, _currentStateType);
			// 	break;
			case StateType.Jumping:
				state = new Jumping(this, _currentStateType);
				break;
			case StateType.Landed:
				state = new Landed(this, _currentStateType);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
		}

		if (state != null)
			state.Finished += OnStateFinished;
        
		return state;
	}
	
	private void OnStateFinished(HumanoidState state, StateType newStateType)
	{
		if (state != CurrentState)
			return;

		HumanoidState? newState = GetState(newStateType);

		if (newState is null)
			return;

		CurrentState?.OnExit();

		newState.OnEnter();

		CurrentState = newState;
		_currentStateType = newStateType;
	}
}