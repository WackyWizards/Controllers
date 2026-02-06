using System;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;

namespace Controllers.Movement;

/// <summary>
/// A movement controller that allows free flight through geometry (noclip mode).
/// Useful for debugging, level design, and spectating.
/// </summary>
public class NoclipController3D : MovementController3D
{
	[Property, Category( "Components" )]
	public CitizenAnimationHelper AnimationHelper { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float FlySpeed { get; set; } = 500f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float SprintMultiplier { get; set; } = 2f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float SlowMultiplier { get; set; } = 0.3f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float Acceleration { get; set; } = 12f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float Friction { get; set; } = 8f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float StopSpeed { get; set; } = 100f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, InputAction]
	public string SprintInput { get; set; } = "Run";
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, InputAction]
	public string SlowInput { get; set; } = "duck";
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, InputAction]
	public string MoveUpInput { get; set; } = "Jump";
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, InputAction]
	public string MoveDownInput { get; set; } = "duck";
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property]
	private bool Debug { get; set; }
	
	private Vector3 _wishVelocity;
	private Rotation _eyeRotation;
	private float _currentSpeed;
	
	private const float MinVelocityThreshold = 0.001f;
	private const float MinSpeedThreshold = 0.1f;
	private const float FullStopThreshold = 1f;
	
	private static readonly Logger Log = new( "NoclipController" );
	
	protected override void OnStart()
	{
		base.OnStart();
		
		if ( IsProxy )
		{
			return;
		}
		
		_currentSpeed = FlySpeed;
	}
	
	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		if ( Debug && !IsProxy )
		{
			DrawDebugInfo();
		}
	}
	
	/// <summary>
	/// Build the wish velocity based on player input, including vertical movement
	/// </summary>
	protected override void BuildWishVelocity()
	{
		_wishVelocity = Vector3.Zero;
		var input = Input.AnalogMove;
		
		// Get speed modifiers
		var speedMod = 1f;
		
		if ( Input.Down( SprintInput ) )
		{
			speedMod = SprintMultiplier;
		}
		else if ( Input.Down( SlowInput ) )
		{
			speedMod = SlowMultiplier;
		}
		
		_currentSpeed = FlySpeed * speedMod;
		
		// No input means no wish velocity
		if ( input.Length < 0.01f && !Input.Down( MoveUpInput ) && !Input.Down( MoveDownInput ) )
		{
			return;
		}
		
		// Build horizontal movement from analog input
		_eyeRotation = Rotation.FromYaw( EyeAngles.yaw );
		var horizontal = _eyeRotation * new Vector3( input.x, input.y, 0 );
		
		// Build vertical movement from up/down inputs
		var vertical = Vector3.Zero;
		
		if ( Input.Down( MoveUpInput ) )
		{
			vertical += Vector3.Up;
		}
		
		if ( Input.Down( SlowInput ) )
		{
			vertical += Vector3.Down;
		}
		
		// Combine and normalize
		_wishVelocity = horizontal + vertical;
		
		if ( _wishVelocity.Length > 0.01f )
		{
			_wishVelocity = _wishVelocity.Normal * _currentSpeed;
		}
	}
	
	/// <summary>
	/// Execute noclip movement - no collision, just smooth acceleration
	/// </summary>
	protected override void Move()
	{
		// Apply friction
		ApplyFriction();
		
		// Apply acceleration toward wish velocity
		if ( _wishVelocity.Length > 0.01f )
		{
			var wishDir = _wishVelocity.Normal;
			Accelerate( wishDir, _currentSpeed );
		}
		
		// Move without collision
		WorldPosition += Velocity * Scene.FixedDelta;
		
		WishVelocity = _wishVelocity;
	}
	
	/// <summary>
	/// Apply friction to velocity
	/// </summary>
	private void ApplyFriction()
	{
		var speed = Velocity.Length;
		
		if ( speed < MinSpeedThreshold )
		{
			return;
		}
		
		// Calculate drop amount
		var control = speed < StopSpeed ? StopSpeed : speed;
		var drop = control * Friction * Scene.FixedDelta;
		
		// Scale velocity
		var newSpeed = MathF.Max( speed - drop, 0f );
		
		if ( Math.Abs( newSpeed - speed ) > MinVelocityThreshold )
		{
			newSpeed /= speed;
			Velocity *= newSpeed;
		}
		
		// Full stop if very slow
		if ( Velocity.Length < FullStopThreshold )
		{
			Velocity = Vector3.Zero;
		}
	}
	
	/// <summary>
	/// Accelerate toward the wish direction
	/// </summary>
	private void Accelerate( Vector3 wishDir, float wishSpeed )
	{
		var currentSpeed = Vector3.Dot( Velocity, wishDir );
		var addSpeed = wishSpeed - currentSpeed;
		
		if ( addSpeed <= 0 )
		{
			return;
		}
		
		var accelSpeed = Acceleration * Scene.FixedDelta * wishSpeed;
		accelSpeed = MathF.Min( accelSpeed, addSpeed );
		Velocity += wishDir * accelSpeed;
	}
	
	/// <summary>
	/// Update animations for noclip mode
	/// </summary>
	protected override void UpdateAnimations()
	{
		base.UpdateAnimations();
		
		if ( !AnimationHelper.IsValid() )
		{
			return;
		}
		
		// Always floating in noclip
		AnimationHelper.IsGrounded = false;
		AnimationHelper.DuckLevel = 0f;
		AnimationHelper.WithLook( EyeAngles.Forward * 100 );
		AnimationHelper.WithVelocity( Velocity );
		AnimationHelper.WithWishVelocity( IsProxy ? Velocity : _wishVelocity );
	}
	
	/// <summary>
	/// Draw debug information
	/// </summary>
	private void DrawDebugInfo()
	{
		Gizmo.Draw.ScreenText( $"Velocity: {Velocity.Length:F1}", new Vector2( 10, 40 ), size: 20 );
		Gizmo.Draw.ScreenText( $"WishVelocity: {_wishVelocity.Length:F1}", new Vector2( 10, 60 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Current Speed: {_currentSpeed:F0}", new Vector2( 10, 80 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Position: {WorldPosition}", new Vector2( 10, 100 ), size: 20 );
		
		// Draw velocity direction
		using ( Gizmo.Scope( "velocity", WorldPosition ) )
		{
			if ( Velocity.Length > 1f )
			{
				Gizmo.Draw.Color = Color.Cyan;
				Gizmo.Draw.Arrow( Vector3.Zero, Velocity.Normal * 50f, 5f );
			}
		}
	}
}
