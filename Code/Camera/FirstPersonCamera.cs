using Sandbox;
using System;

namespace Controllers.Camera;

public class FirstPersonCamera : BaseCameraController
{
	[Property]
	public Vector3 Offset { get; set; } = new( 0, 0, 70 );
	
	protected const float MaxPitchAngle = 89.0f;
	protected const float MinPitchAngle = -89.0f;
	
	private Rotation _currentRotation = Rotation.Identity;
	private float _currentPitch;

	protected override void UpdateCameraPosition()
	{
		Camera.WorldPosition = GameObject.WorldPosition + Offset;
	}

	protected override void UpdateCameraRotation()
	{
		var lookInput = Input.AnalogLook;
		var pitchDelta = lookInput.pitch;
		var yawDelta = lookInput.yaw;

		_currentPitch += pitchDelta;
		_currentPitch = Math.Clamp( _currentPitch, MinPitchAngle, MaxPitchAngle );
		_currentRotation = Rotation.FromYaw( _currentRotation.Yaw() + yawDelta );
		
		var pitchRotation = Rotation.FromPitch( _currentPitch );
		EyeAngles = (_currentRotation * pitchRotation).Angles();
		Camera.WorldRotation = _currentRotation * pitchRotation;
		GameObject.WorldRotation = _currentRotation;
	}
}
