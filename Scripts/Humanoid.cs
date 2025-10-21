using System;
using Godot;

namespace RobloxCharacterController.Scripts;

public partial class Humanoid : RigidBody3D
{
	[ExportGroup("Movement")] 
	[Export] public float WalkSpeed { get; private set; } = 16f;
	[Export] public float JumpPower { get; private set; } = 53f;

	private RayCast3D _ceilingRayCast = null!;
	private RayCast3D _groundRayCast = null!;
	private RayCast3D _climbRayCast = null!;

	// Floor properties
	public float? FloorDistance { get; private set; }
	public Vector3? FloorNormal { get; private set; }
	public Vector3? FloorHitLocation { get; private set; }
	public GodotObject? FloorPart { get; private set; }
	public PhysicsMaterial? FloorMaterial { get; private set; }
	
	// Ceiling properties
	public bool HittingCeiling { get; private set; }
	
	// Climbing properties
	public bool IsClimbing { get; private set; }
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_ceilingRayCast = new RayCast3D();
		AddChild(_ceilingRayCast);

		_groundRayCast = new RayCast3D();
		AddChild(_groundRayCast);
		
		_climbRayCast = new RayCast3D();
		AddChild(_climbRayCast);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		SetFloorProperties();
		SetCeilingProperties();
		SetClimbingProperties();
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
}