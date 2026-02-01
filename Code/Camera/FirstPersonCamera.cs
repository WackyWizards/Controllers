using Sandbox;
using System;

namespace Controllers.Camera;

public class FirstPersonCamera : CameraController
{
	[Property]
	private Vector3 Offset { get; set; } = new( 0, 1, 70 );
	
	[Property]
	private float MinPitchAngle { get; set; } = -89f;
	
	[Property]
	private float MaxPitchAngle { get; set; } = 89f;
	
	/// <summary>
	/// Position lerp smoothness. Higher = less smooth but more responsive.
	/// </summary>
	[Property]
	private float PositionSmoothness { get; set; } = 25f;
	
	[Property]
	public GameObject ModelParent { get; set; }
	
	[Property]
	public Rigidbody Rigidbody { get; set; }
	
	private Rotation _currentRotation = Rotation.Identity;
	private float _currentPitch;
	private Vector3 _smoothedPosition;
	private bool _initialized;
	private Vector3 _previousPlayerPosition;
	
	protected override void OnStart()
	{
		if ( !IsProxy && Network.IsOwner && ModelParent.IsValid() )
		{
			foreach ( var model in ModelParent.GetAllObjects( true ) )
			{
				var renderer = model.GetComponent<ModelRenderer>();
				if ( !renderer.IsValid() )
				{
					continue;
				}
				
				renderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		}
		
		base.OnStart();
	}
	
	protected override void UpdateCameraPosition()
	{
		var target = GameObject.WorldPosition + Offset;
		
		if ( !_initialized )
		{
			_smoothedPosition = target;
			_previousPlayerPosition = GameObject.WorldPosition;
			_initialized = true;
		}
		
		// Detect vertical snaps
		// If the player moved vertically more than could be explained by normal velocity
		// in one frame, it was a snap - follow it instantly on the vertical axis
		var playerDelta = GameObject.WorldPosition - _previousPlayerPosition;
		
		var expectedVertical = Rigidbody.IsValid() ? Rigidbody.Velocity.z * Time.Delta : 0f;
		var snapThreshold = 1.0f;
		
		if ( MathF.Abs( playerDelta.z - expectedVertical ) > snapThreshold )
		{
			// Vertical snap detected - apply it directly to the smoothed position
			_smoothedPosition.z += playerDelta.z;
		}
		
		_previousPlayerPosition = GameObject.WorldPosition;
		
		// Normal lerp for horizontal (and non-snapped vertical)
		_smoothedPosition = Vector3.Lerp( _smoothedPosition, target, PositionSmoothness * Time.Delta );
		Camera.WorldPosition = _smoothedPosition;
	}
	
	protected override void UpdateCameraRotation()
	{
		var look = Input.AnalogLook;
		_currentPitch = Math.Clamp( _currentPitch + look.pitch, MinPitchAngle, MaxPitchAngle );
		_currentRotation = Rotation.FromYaw( _currentRotation.Yaw() + look.yaw );
		
		var rot = _currentRotation * Rotation.FromPitch( _currentPitch );
		EyeAngles = rot.Angles();
		Camera.WorldRotation = rot;
		
		GameObject.WorldRotation = _currentRotation;
	}
}
