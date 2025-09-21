using Sandbox;
using System;

namespace Controllers.Camera;

public class FirstPersonCamera : CameraController
{
	[Property]
	private Vector3 Offset { get; set; } = new( -5, 5, 70 );

	[Property]
	private float MinPitchAngle { get; set; } = -89.0f;

	[Property]
	private float MaxPitchAngle { get; set; } = 89.0f;

	/// <summary>
	/// Position lerp smoothness. Higher = less smooth but more responsive
	/// </summary>
	[Property]
	private float PositionSmoothness { get; set; } = 25f;

	[Property]
	public GameObject ModelParent { get; set; }

	private Rotation _currentRotation = Rotation.Identity;
	private float _currentPitch;
	private Vector3 _smoothedPosition;
	private bool _initialized;

	protected override void OnStart()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( !ModelParent.IsValid() )
		{
			return;
		}

		var models = ModelParent.GetAllObjects( true );
		foreach ( var model in models )
		{
			var modelComponent = model.GetComponent<ModelRenderer>();
			if ( modelComponent.IsValid() )
			{
				modelComponent.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		}
	}

	protected override void UpdateCameraPosition()
	{
		var targetPosition = GameObject.WorldPosition + Offset;

		if ( !_initialized )
		{
			_smoothedPosition = targetPosition;
			_initialized = true;
		}

		_smoothedPosition = Vector3.Lerp( _smoothedPosition, targetPosition, PositionSmoothness * Time.Delta );
		Camera.WorldPosition = _smoothedPosition;
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
