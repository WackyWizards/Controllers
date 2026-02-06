using Sandbox;
using System;
using Controllers.Movement;

namespace Controllers.Camera;

public class FirstPersonCamera : CameraController
{
	[Property, Category( "Components" )]
	public GameObject ModelParent { get; set; }
	
	[Property, Category( "Components" )]
	public Rigidbody Rigidbody { get; set; }

	[Property]
	private Vector3 Offset { get; set; } = new( 0, 0, 65 );
	
	[Property]
	private float MinPitchAngle { get; set; } = -89f;
	
	[Property]
	private float MaxPitchAngle { get; set; } = 89f;
	
	/// <summary>
	/// Position lerp smoothness. Higher = less smooth but more responsive.
	/// </summary>
	[Property]
	private float PositionSmoothness { get; set; } = 25f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( nameof(UseCrouchOffset), Title = "Crouching" )]
	public bool UseCrouchOffset { get; set; } = true;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( nameof(UseCrouchOffset), Title = "Crouching" )]
	public float CrouchCameraOffset { get; set; } = 34f;
	
	[Property, FeatureEnabled( nameof(UseCrouchOffset), Title = "Crouching" )]
	public WalkController3D Movement { get; set; }
	
	private bool _initialized;
	private Rotation _currentRotation = Rotation.Identity;
	private float _currentPitch;
	private Vector3 _smoothedPosition;
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
		var crouchOffset = 0f;
		
		if ( UseCrouchOffset && Movement.IsValid() )
		{
			crouchOffset = Movement.CrouchFactor * CrouchCameraOffset;
		}
		
		var target = GameObject.WorldPosition + Offset - Vector3.Up * crouchOffset;
		
		if ( !_initialized )
		{
			_smoothedPosition = target;
			_previousPlayerPosition = GameObject.WorldPosition;
			_initialized = true;
		}
		
		var playerDelta = GameObject.WorldPosition - _previousPlayerPosition;
		var expectedVertical = Rigidbody.IsValid() ? Rigidbody.Velocity.z * Time.Delta : 0f;
		
		if ( MathF.Abs( playerDelta.z - expectedVertical ) > 1.0f )
		{
			_smoothedPosition.z += playerDelta.z;
		}
		
		_previousPlayerPosition = GameObject.WorldPosition;
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
