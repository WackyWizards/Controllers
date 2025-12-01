using Sandbox;
using System;

namespace Controllers.Camera;

public class FirstPersonCamera : CameraController
{
	[Property]
	private Vector3 Offset { get; set; } = new( -5, 5, 70 );
	
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
	
	private Rotation _currentRotation = Rotation.Identity;
	private float _currentPitch;
	private Vector3 _smoothedPosition;
	private bool _initialized;
	
	protected override void OnStart()
	{
		if ( IsProxy || !ModelParent.IsValid() )
		{
			return;
		}
		
		foreach ( var model in ModelParent.GetAllObjects( true ) )
		{
			var renderer = model.GetComponent<ModelRenderer>();
			
			if ( renderer.IsValid() )
			{
				renderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		}
	}
	
	protected override void UpdateCameraPosition()
	{
		var target = GameObject.WorldPosition + Offset;
		
		if ( !_initialized )
		{
			_smoothedPosition = target;
			_initialized = true;
		}
		
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
