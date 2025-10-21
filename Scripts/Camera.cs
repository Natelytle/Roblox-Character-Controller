using System;
using Godot;

namespace RobloxCharacterController.Scripts;

public partial class Camera : Node3D
{
    private const float MinDistance = 0.5f;
    private const float MaxDistance = 400f;
    private const float FirstPersonThreshold = 1.0f;
    private const float ShiftLockOffset = 1.75f;
    
    private SpringArm3D _springArm = null!;
    private Camera3D _camera = null!;
    private Node3D _parent = null!;

    [Export] public float MouseSensitivity { get; set; } = 1;
    private Vector2 _mousePixelsToUnits = new(0.002f * float.Pi, 0.0015f * float.Pi);
    
    public bool RotationLocked => _firstPerson || _shiftLock;
    public float Zoom => _currentDistance;
    
    private float _currentDistance = 12.5f;
    private float _horizontalOffset;
    private bool _firstPerson;
    private bool _shiftLock;
    private bool _rightClick;
    
    public override void _Ready()
    {
        _springArm = (SpringArm3D)GetNode("SpringArm3D");
        _camera = (Camera3D)GetNode("SpringArm3D/Camera3D");
        _parent = (Node3D)GetParent();
    }

    public override void _Process(double delta)
    {
        // 1.5 is the distance of the head over the attachment node.
        GlobalPosition = _parent.GlobalPosition + _parent.GlobalBasis.Y * 1.5f + _parent.GlobalBasis.X * _horizontalOffset;
        
        _springArm.SpringLength = float.Min((GlobalPosition - _camera.GlobalPosition).Length(), _springArm.SpringLength);

        // Update spring arm length
        float amount = 1 - float.Pow(0.5f, (float)delta * 30);
        _springArm.SpringLength = float.Lerp(_springArm.SpringLength, _currentDistance, amount);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsAction("zoom_in") && e.IsPressed())
        {
            float zoomStrength = Input.GetActionStrength("zoom_in");
            
            float newDistance = _currentDistance - (1 + _currentDistance * 0.5f);

            SetCameraToDistance(newDistance * zoomStrength);
        }

        if (e.IsAction("zoom_out") && e.IsPressed())
        {
            float zoomStrength = Input.GetActionStrength("zoom_out");

            float newDistance;

            if (_firstPerson)
                newDistance = FirstPersonThreshold;
            else
                newDistance = _currentDistance + (1 + _currentDistance * 0.5f);
            
            SetCameraToDistance(newDistance * zoomStrength);
        }
        
        if (e.IsActionPressed("shift_lock") && e.IsPressed())
        {
            ToggleShiftLock();
        }

        if (e.IsActionPressed("right_click") && e.IsPressed())
        {
            _rightClick = true;
            UpdateMouseBehaviour();
        }

        if (e.IsActionReleased("right_click"))
        {
            _rightClick = false;
            UpdateMouseBehaviour();
        }
        
        if (Input.MouseMode is Input.MouseModeEnum.Captured && e is InputEventMouseMotion motion)
        {
            Move(motion);
        }
    }
    
    private void SetCameraToDistance(float newDistance)
    {
        newDistance = Math.Clamp(newDistance, MinDistance, MaxDistance);

        if (newDistance < FirstPersonThreshold)
        {
            _currentDistance = 0f;
            EnterFirstPerson();
        }
        else
        {
            _currentDistance = newDistance;
            
            if (_firstPerson)
                ExitFirstPerson();
        }
    }
    
    private void EnterFirstPerson()
    {
        _firstPerson = true;
        
        // Reset shift lock camera
        _horizontalOffset = 0f;
        
        UpdateMouseBehaviour();
    }

    private void ExitFirstPerson()
    {
        _firstPerson = false;

        if (_shiftLock)
            _horizontalOffset = ShiftLockOffset;
        
        UpdateMouseBehaviour();
    }
    
    private void ToggleShiftLock()
    {
        _shiftLock = !_shiftLock;

        if (_shiftLock && !_firstPerson)
            _horizontalOffset = ShiftLockOffset;
        else
            _horizontalOffset = 0f;
        
        UpdateMouseBehaviour();
    }

    private void UpdateMouseBehaviour()
    {
        if (_shiftLock || _firstPerson || _rightClick)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
    
    private void Move(InputEventMouseMotion motion)
    {
        Vector2 moveVector = motion.Relative * _mousePixelsToUnits * MouseSensitivity;

        float yRotation = Rotation.Y - moveVector.X;
        float xRotation = Rotation.X - moveVector.Y;

        // Clamp to 80 degrees.
        xRotation = Math.Clamp(xRotation, float.DegreesToRadians(-80), float.DegreesToRadians(80));
        
        SetRotation(new Vector3(xRotation, yRotation, 0f));
    }
}