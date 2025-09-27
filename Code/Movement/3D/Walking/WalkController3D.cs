using System.Linq;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;

namespace Controllers.Movement;

/// <summary>
/// 3D Character Walk Controller.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public partial class WalkController3D : MovementController3D
{
	[Property, Category( "Components" ), RequireComponent]
	public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property, Category( "State" ), ReadOnly, Sync]
	public bool IsGrounded { get; set; } = true;

	[Property]
	public float StepHeight { get; set; } = 24f;

	[Property]
	public float SlopeAngle { get; set; } = 24f;

	[Property]
	public float AccelerationRate { get; set; } = 8000f;

	[Property]
	public float Speed { get; set; } = 200f;

	[Property]
	public float AirControl { get; set; } = 1f;
	
	[Property]
	public float AirInfluence { get; set; } = 0.3f;

	[Property]
	public float Friction { get; set; } = 6.0f;

	[Property]
	public float SkinWidth { get; set; } = 0.1f;

	private BoxCollider FloorCollider { get; set; }
	private float GroundRayDistance { get; set; } = 2f;

	private float _speed;
	private Vector2 _movementInput;
	private Rotation _rotation;
	private float _originalCapsuleHeight;
	private Vector3 _originalBoxSize;

	private static readonly Logger Log = new("WalkController");

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			return;
		}
		
		// TODO: Allow other collider types for floor detection
		if ( !Colliders.OfType<BoxCollider>().Any() )
		{
			Log.Warning( $"{this} currently only supports a BoxCollider for feet!" );
		}

		FloorCollider = Colliders.FirstOrDefault( x => x is BoxCollider ) as BoxCollider;

		var capsuleCollider = Colliders.FirstOrDefault( x => x is CapsuleCollider ) as CapsuleCollider;
		if ( capsuleCollider.IsValid() )
		{
			_originalCapsuleHeight = capsuleCollider.End.z;
		}

		var boxCollider = Colliders.FirstOrDefault( x => x is BoxCollider ) as BoxCollider;
		if ( boxCollider.IsValid() )
		{
			_originalBoxSize = boxCollider.Scale;
		}
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			if ( Debug )
			{
				DrawDebugInfo();
			}

			HandleCoyoteTime();
			HandleJumpInput();
			HandleCrouchInput();

			_speed = Speed;

			// Apply basic inputs
			var input = Input.AnalogMove;
			_rotation = Rotation.FromYaw( EyeAngles.yaw );
			_movementInput = new Vector2( input.x, input.y );

			// Store grounded state for next frame
			_wasGroundedLastFrame = IsGrounded;
		}

		base.OnUpdate();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		// Process movement input
		if ( _movementInput.Length > 0.1f )
		{
			_movementInput = _rotation * _movementInput;
			_movementInput = _movementInput.Normal;
		}

		HandleCrouchTransition();
		base.OnFixedUpdate();
	}

	protected override void CheckGround()
	{
		if ( !FloorCollider.IsValid() )
		{
			return;
		}

		var start = FloorCollider.WorldPosition + Vector3.Up * SkinWidth;
		var end = start - Vector3.Up * GroundRayDistance;
		var bbox = BBox.FromPositionAndSize( FloorCollider.Center, FloorCollider.Scale );

		var trace = Scene.Trace.Box( bbox, start, end )
			.WithoutTags( "player", "nocollide" )
			.UseHitPosition()
			.Run();

		var angle = Vector3.GetAngle( trace.Normal, Vector3.Up );
		var wasGrounded = IsGrounded;
    
		// More reliable grounding check with distance tolerance
		IsGrounded = trace.Hit && angle <= SlopeAngle && trace.Distance <= GroundRayDistance - SkinWidth;
		if ( IsGrounded )
		{
			//GroundStick();
		}

		ResetJumpFlagOnLanding( wasGrounded );

		// Apply grounding force more selectively
		if ( IsGrounded && _movementInput.LengthSquared > 0.01f && angle > 5f && angle <= SlopeAngle )
		{
			var groundingForce = Vector3.Down * Rigidbody.Mass * 2f; // Increased force
			Rigidbody.ApplyForce( groundingForce );
		}
	}

	protected override void CalculateVelocity()
	{
		// If we aren't moving and grounded, apply friction.
		if ( _movementInput.LengthSquared < 0.01f && IsGrounded )
		{
			ApplyFriction();
		}
		else // Else, assign our movement input as our WishVelocity.
		{
			var control = IsGrounded ? 1.0f : AirControl;
			var desiredHorizontal = _movementInput * _speed * control;
			WishVelocity = new Vector3( desiredHorizontal.x, desiredHorizontal.y, 0 );
		}
	}

	/// <summary>
	/// Applies friction to horizontal movement when the player is grounded and not actively moving.
	/// This gradually slows down the player until they come to a stop, while preserving vertical velocity.
	/// </summary>
	private void ApplyFriction()
	{
		var horizontalVelocity = new Vector3( Velocity.x, Velocity.y, 0 );
		var speed = horizontalVelocity.Length;

		if ( speed < 0.1f )
		{
			// Stop the rigidbody.
			var stopForce = -horizontalVelocity * Rigidbody.Mass / Scene.FixedDelta;
			Rigidbody.ApplyForce( stopForce );
			WishVelocity = Vector3.Zero;
			return;
		}

		// Apply friction force directly to rigidbody
		var frictionForce = -horizontalVelocity.Normal * Friction * Rigidbody.Mass;
		Rigidbody.ApplyForce( frictionForce );
		WishVelocity = Vector3.Zero;
	}

	protected override void Move()
	{
		if ( !Rigidbody.IsValid() )
		{
			return;
		}

		// Handle jumping
		ExecuteJump();
		if ( WishJump ) // If we just jumped, don't process movement this frame
		{
			return;
		}

		var horizontalTarget = new Vector3( WishVelocity.x, WishVelocity.y, 0 );
		var horizontalCurrent = new Vector3( Rigidbody.Velocity.x, Rigidbody.Velocity.y, 0 );

		if ( IsGrounded )
		{
			ApplyHorizontalMovement( horizontalTarget, horizontalCurrent );

			if ( horizontalTarget.Length > 0.1f && CanStepUp( horizontalTarget * Scene.FixedDelta ) )
			{
				StepUp( horizontalTarget * Scene.FixedDelta );
			}
		}
		else
		{
			// Air control - preserve momentum but allow direction changes
			var desiredChange = horizontalTarget - horizontalCurrent;
			var airTarget = horizontalCurrent + desiredChange * AirInfluence;
    
			ApplyHorizontalMovement( airTarget, horizontalCurrent, forceMultiplier: AirInfluence );
		}
	}

	/// <summary>
	/// Applies horizontal movement forces.
	/// </summary>
	private void ApplyHorizontalMovement( Vector3 targetHorizontal, Vector3 currentHorizontal, float forceMultiplier = 1.0f, float maxAccelMultiplier = 1.0f )
	{
		var horizontalDiff = targetHorizontal - currentHorizontal;
		var maxAccel = AccelerationRate * Scene.FixedDelta * maxAccelMultiplier;

		if ( horizontalDiff.Length > maxAccel )
		{
			horizontalDiff = horizontalDiff.Normal * maxAccel;
		}

		if ( horizontalDiff.Length <= 0.01f )
		{
			return;
		}

		Rigidbody.ApplyForce( horizontalDiff * Rigidbody.Mass * forceMultiplier / Scene.FixedDelta );

		// Clamp max horizontal velocity
		var newVel = new Vector3( Rigidbody.Velocity.x, Rigidbody.Velocity.y, 0 );
		if ( !(newVel.Length > Speed) )
		{
			return;
		}

		newVel = newVel.Normal * Speed;
		Rigidbody.Velocity = new Vector3( newVel.x, newVel.y, Rigidbody.Velocity.z );
	}
	
	private void StepUp( Vector3 moveDelta )
	{
		if ( StepHeight <= 0 || !IsGrounded || moveDelta.Length < 0.01f )
		{
			return;
		}

		// Add a small upward offset to avoid starting embedded
		var start = WorldPosition + Vector3.Up * 0.1f;
		var upOffset = Vector3.Up * StepHeight;

		// 1. Raise up to step height
		var upward = Scene.Trace
			.Sweep( Rigidbody, WorldTransform.WithPosition( start ), WorldTransform.WithPosition( start + upOffset ) )
			.WithoutTags( "player", "nocollide" )
			.Run();

		var stepOrigin = upward.Hit ? upward.EndPosition : start + upOffset;

		// 2. Try moving forward from raised position
		var forward = Scene.Trace
			.Sweep( Rigidbody, WorldTransform.WithPosition( stepOrigin ), WorldTransform.WithPosition( stepOrigin + moveDelta ) )
			.WithoutTags( "player", "nocollide" )
			.Run();

		var forwardEnd = forward.Hit ? forward.EndPosition : stepOrigin + moveDelta;

		// 3. Drop down to ground from the new position
		var downward = Scene.Trace
			.Sweep( Rigidbody, WorldTransform.WithPosition( forwardEnd ), WorldTransform.WithPosition( forwardEnd - Vector3.Up * (StepHeight + 2f) ) )
			.WithoutTags( "player", "nocollide" )
			.Run();

		// Didn't hit anything.
		if ( !downward.Hit )
		{
			return;
		}

		// 4. Apply final safe step position
		WorldPosition = downward.EndPosition;
		if ( Debug )
		{
			Log.Info( $"Stepped!" );
		}
	}

	private bool CanStepUp( Vector3 moveDelta )
	{
		if ( moveDelta.Length < 0.01f )
		{
			return false;
		}

		var forward = moveDelta.Normal * 8f; // short probe ahead

		// 1. Check if there's a wall at foot level
		var footStart = WorldPosition;
		var footEnd = footStart + forward;

		var footTrace = Scene.Trace.Ray( footStart, footEnd )
			.WithoutTags( "player", "nocollide" )
			.Run();

		if ( !footTrace.Hit )
		{
			// no wall at foot level, so no step needed
			return false;
		}
		
		var surfaceAngle = Vector3.GetAngle( footTrace.Normal, Vector3.Up );
		if ( surfaceAngle <= SlopeAngle )
		{
			return false;
		}

		// 2. Check if at step height it's clear
		var stepStart = WorldPosition + Vector3.Up * StepHeight;
		var stepEnd = stepStart + forward;

		var stepTrace = Scene.Trace.Ray( stepStart, stepEnd )
			.WithoutTags( "player", "nocollide" )
			.Run();

		return !stepTrace.Hit;
	}

	protected override void UpdateAnimations()
	{
		base.UpdateAnimations();
		
		if ( !AnimationHelper.IsValid() )
		{
			return;
		}

		AnimationHelper.IsGrounded = IsGrounded;
		AnimationHelper.DuckLevel = _currentCrouchFactor;
		AnimationHelper.WithLook( EyeAngles.Forward * 100 );
		AnimationHelper.WithVelocity( Velocity );
		AnimationHelper.WithWishVelocity( WishVelocity );
	}
}
