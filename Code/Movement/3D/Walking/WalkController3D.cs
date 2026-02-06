using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;

namespace Controllers.Movement;

public partial class WalkController3D : MovementController3D
{
	[Property, Category( "Components" )]
	public CitizenAnimationHelper AnimationHelper { get; set; }
	
	[Property, Category( "Components" )]
	public Rigidbody Rigidbody { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// Reference list of colliders used by this controller. <br/>
	/// If a <see cref="Rigidbody"/> is provided, the Rigidbody will automatically use all colliders 
	/// that are children of its GameObject - this list is just for convenience/reference. <br/>
	/// If no Rigidbody is provided, the controller falls back to using <see cref="BodyGirth"/> and 
	/// <see cref="BodyHeight"/> to create a bounding box for traces. <br/>
	/// <br/>
	/// <b>Optional:</b> Leave empty to use a single collider instead of separate body/feet colliders.
	/// </summary>
	[Property, Category( "Components" )]
	public List<Collider> Colliders { get; set; } = [];
	
	[Property]
	public bool SkipImpulses { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float WalkSpeed { get; set; } = 190f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float Acceleration { get; set; } = 8f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float AirAcceleration { get; set; } = 1f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float GroundFriction { get; set; } = 4f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float StopSpeed { get; set; } = 100f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float GroundAngle { get; set; } = 45f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// If we have no set <see cref="Rigidbody"/>, this is used to define the width of our box trace.
	/// </summary>
	[Property, Category( "Movement" )]
	public float BodyGirth { get; set; } = 32f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// If we have no set <see cref="Rigidbody"/>, this will define the height of our box trace.
	/// </summary>
	[Property, Category( "Movement" )]
	public float BodyHeight { get; set; } = 64f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float StepHeight { get; set; } = 18f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "Movement" )]
	public float Bounciness { get; set; } = 0.3f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "State" ), ReadOnly, Sync]
	public bool IsGrounded { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "State" ), ReadOnly]
	public Vector3 GroundNormal { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Category( "State" )]
	public float CurrentSpeed { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	// Ground object tracking for moving platforms
	public GameObject GroundObject { get; private set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	public Collider GroundCollider { get; private set; }
	
	private Transform _groundTransform;
	private Vector3 _groundVelocity;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Sync]
	public Vector3 Velocity { get; set; }
	
	/// <summary>
	/// Finds the first CapsuleCollider in the <see cref="Colliders"/> list. <br/>
	/// This is typically the main body collision shape for the character. <br/>
	/// If the Colliders list is empty, returns the first CapsuleCollider found in children.
	/// </summary>
	public Collider BodyCollider
	{
		get
		{
			var collider = Colliders.Find( x => x is CapsuleCollider );
			
			if ( collider.IsValid() )
			{
				return collider;
			}
			
			// Fallback: find any CapsuleCollider in children
			return GetComponentInChildren<CapsuleCollider>();
		}
	}
	
	/// <summary>
	/// Finds the first BoxCollider in the <see cref="Colliders"/> list. <br/>
	/// This is typically used for ground detection and stepping. <br/>
	/// If the Colliders list is empty, returns the first BoxCollider found in children.
	/// </summary>
	public Collider FeetCollider
	{
		get
		{
			var collider = Colliders.Find( x => x is BoxCollider );
			
			if ( collider.IsValid() )
			{
				return collider;
			}
			
			// Fallback: find any BoxCollider in children
			return GetComponentInChildren<BoxCollider>();
		}
	}
	
	private Vector3 _mins;
	private Vector3 _maxs;
	private Vector3 _wishVelocity;
	private Rotation _eyeRotation;
	private int _stuckTries;
	private bool _usedPlatformTransform;
	
	// Velocity clipping planes for collision
	private const int MaxClipPlanes = 3;
	private Vector3[] _clipPlanes = new Vector3[MaxClipPlanes];
	private int _clipPlaneCount;
	private Vector3 _originalVelocity;
	private Vector3 _bumpVelocity;
	
	private const float UnstuckUpOffset = 2f;
	private const float GroundCheckDistance = 2f;
	private const float FlyingUpwardThreshold = 40.0f;
	private const float GroundedStepDownDistance = 0.1f;
	private const float MinVelocityThreshold = 0.001f;
	private const float MinSpeedThreshold = 0.1f;
	private const float FullStopThreshold = 1f;
	private const float MinMoveFraction = 0.01f;
	private const float MinStepDelta = 0.5f;
	private const float StepDeltaFallback = 0.5f;
	private const int UnstuckAttemptsPerTick = 20;
	
	private static readonly Logger Log = new( "WalkController" );
	
	protected override void OnStart()
	{
		base.OnStart();
		
		if ( IsProxy )
		{
			return;
		}
		
		// Set up hull size
		_mins = new Vector3( -BodyGirth / 2f, -BodyGirth / 2f, 0 );
		_maxs = new Vector3( BodyGirth / 2f, BodyGirth / 2f, BodyHeight );
		_originalCapsuleHeight = BodyHeight;
		
		// Set variables
		CurrentSpeed = WalkSpeed;
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
	/// Build the wish velocity based on player input. Called before Move().
	/// </summary>
	protected override void BuildWishVelocity()
	{
		_wishVelocity = Vector3.Zero;
		var input = Input.AnalogMove;
		
		if ( input.Length < 0.01f )
		{
			return;
		}
		
		_eyeRotation = Rotation.FromYaw( EyeAngles.yaw );
		_wishVelocity = _eyeRotation * new Vector3( input.x, input.y, 0 );
		_wishVelocity = _wishVelocity.Normal;
		_wishVelocity *= CurrentSpeed;
	}
	
	protected override void PreMove()
	{
		// Reset rigidbody velocity at the start of our movement frame
		// This prevents inherited velocity from previous physics interactions
		if ( Rigidbody.IsValid() && !IsProxy )
		{
			if ( !SkipImpulses )
			{
				// Pull external physics influence into controller
				Velocity += Rigidbody.Velocity;
			}
			
			// Clear physics so it doesn't integrate twice
			Rigidbody.Velocity = Vector3.Zero;
			Rigidbody.AngularVelocity = Vector3.Zero;
		}
		
		if ( IsGrounded && GroundCollider.IsValid() )
		{
			_groundVelocity = GroundCollider.SurfaceVelocity.WithZ( 0 );
			Velocity -= _groundVelocity;
		}
		
		base.PreMove();
	}
	
	/// <summary>
	/// Execute movement. Called after BuildWishVelocity().
	/// </summary>
	protected override void Move()
	{
		// Try to unstuck first
		if ( TryUnstuck() )
		{
			return;
		}
		
		// Update grounded state
		CategorizePosition();
		
		// Handle inputs
		HandleCoyoteTime();
		HandleJumpInput();
		HandleCrouchInput();
		
		// Execute jump
		var jumped = Jump();
		
		if ( !jumped )
		{
			// Apply movement
			if ( IsGrounded )
			{
				GroundMove();
			}
			else
			{
				AirMove();
			}
		}
		
		// Apply gravity
		if ( !IsGrounded )
		{
			Velocity += Scene.PhysicsWorld.Gravity * Scene.FixedDelta;
		}
		
		// Perform collision-based movement
		PerformMove( IsGrounded );
		
		// Final ground check
		CategorizePosition();
		
		// Apply platform movement
		ApplyPlatformMovement();
		
		// Apply ground velocity, for example for conveyor belts.
		ApplyGroundVelocity();
		
		WishVelocity = _wishVelocity;
	}
	
	/// <summary>
	/// Apply movement from platform (handles both transform and physics-based platforms)
	/// </summary>
	private void ApplyPlatformMovement()
	{
		if ( !IsGrounded || !GroundObject.IsValid() )
		{
			_groundTransform = default;
			_usedPlatformTransform = false;
			return;
		}
		
		var currentTransform = GroundObject.WorldTransform;
		_usedPlatformTransform = false;
		
		// If we have a previous transform, calculate and apply the delta
		if ( _groundTransform != default )
		{
			var positionDelta = currentTransform.Position - _groundTransform.Position;
			var rotationDelta = currentTransform.Rotation * _groundTransform.Rotation.Inverse;
			
			var localPos = WorldPosition - _groundTransform.Position;
			var rotatedPos = rotationDelta * localPos;
			var rotationPositionDelta = rotatedPos - localPos;
			
			var totalDelta = positionDelta + rotationPositionDelta;
			
			if ( !totalDelta.IsNearZeroLength )
			{
				_usedPlatformTransform = true;
				
				WorldPosition += totalDelta;
				Velocity += totalDelta / Scene.FixedDelta;
			}
		}
		
		// Store current transform for next frame
		_groundTransform = currentTransform;
	}
	
	private void ApplyGroundVelocity()
	{
		if ( !IsGrounded )
		{
			_groundVelocity = Vector3.Zero;
			return;
		}
		
		if ( _usedPlatformTransform )
		{
			return;
		}
		
		Velocity += _groundVelocity;
		WorldPosition += _groundVelocity * Scene.FixedDelta;
	}
	
	/// <summary>
	/// Get a new bounding box (BBox) from the <see cref="_mins"/> and <see cref="_maxs"/>
	/// </summary>
	private BBox GetBBox()
	{
		return new BBox( _mins, _maxs );
	}
	
	/// <summary>
	/// Build a trace for the player using the actual colliders. <br/>
	/// <br/>
	/// <b>How it works:</b> <br/>
	/// - If a Rigidbody is present, uses Scene.Trace.Body() which automatically sweeps ALL colliders 
	///   that are children of the Rigidbody's GameObject (the Rigidbody maintains an internal list 
	///   of colliders that register with it automatically). <br/>
	/// - If no Rigidbody is present, falls back to a simple box trace using BodyGirth and BodyHeight. <br/>
	/// <br/>
	/// <b>Important:</b> The <see cref="Colliders"/> list is just a reference - the Rigidbody uses 
	/// its own internal mechanism to find and use colliders in its hierarchy.
	/// </summary>
	private SceneTrace BuildTrace( Vector3 from, Vector3 to )
	{
		// Use the rigidbody if available, which will sweep all attached colliders
		if ( Rigidbody.IsValid() && Rigidbody.PhysicsBody.IsValid() )
		{
			// This will trace with ALL colliders that are children of the Rigidbody's GameObject
			// The Rigidbody automatically discovers and uses these colliders
			return Scene.Trace.Body( Rigidbody, to )
				.FromTo( from, to )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "player", "nocollide" );
		}
		
		// Fallback to box trace if no rigidbody
		// This uses the manually defined BodyGirth and BodyHeight values
		var bbox = GetBBox();
		
		return Scene.Trace.Box( bbox, from, to )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "nocollide" );
	}
	
	/// <summary>
	/// Perform the actual movement with stepping and collision
	/// </summary>
	private void PerformMove( bool useStep )
	{
		if ( Velocity.Length < MinVelocityThreshold )
		{
			Velocity = Vector3.Zero;
			return;
		}
		
		if ( useStep )
		{
			TryMoveWithStep( Scene.FixedDelta );
		}
		else
		{
			TryMove( Scene.FixedDelta );
		}
	}
	
	/// <summary>
	/// Try to move with collision detection and sliding
	/// </summary>
	private float TryMove( float timeDelta )
	{
		var timeLeft = timeDelta;
		float travelFraction = 0;
		
		_clipPlaneCount = 0;
		_originalVelocity = Velocity;
		_bumpVelocity = Velocity;
		
		for ( var bump = 0; bump < MaxClipPlanes; bump++ )
		{
			if ( Velocity.Length.AlmostEqual( 0.0f ) )
			{
				break;
			}
			
			var end = WorldPosition + Velocity * timeLeft;
			var trace = BuildTrace( WorldPosition, end ).Run();
			travelFraction += trace.Fraction;
			timeLeft -= timeLeft * trace.Fraction;
			
			if ( !trace.Hit )
			{
				WorldPosition = trace.EndPosition;
				
				break;
			}
			
			if ( trace.Fraction > 0 )
			{
				WorldPosition = trace.EndPosition;
				StartBump( Velocity );
			}
			
			var standable = trace.Normal.Angle( Vector3.Up ) <= GroundAngle;
			
			if ( !TryAddPlane( trace.Normal, standable ? 0 : Bounciness ) )
			{
				break;
			}
		}
		
		return travelFraction;
	}
	
	/// <summary>
	/// Try to move with step up capability
	/// </summary>
	private void TryMoveWithStep( float timeDelta )
	{
		var startPosition = WorldPosition;
		var startVelocity = Velocity;
		
		// Do a regular move
		var fraction = TryMove( timeDelta );
		
		// If we barely moved, keep the regular move result
		if ( fraction <= MinMoveFraction )
		{
			return;
		}
		
		// Save the result of the regular move
		var regularPosition = WorldPosition;
		var regularVelocity = Velocity;
		
		// Try stepping - use separate variables to not pollute the regular move
		var stepPosition = startPosition;
		var stepVelocity = startVelocity;
		
		// Step up (simple trace)
		var upTrace = BuildTrace( stepPosition, stepPosition + Vector3.Up * StepHeight ).Run();
		stepPosition = upTrace.EndPosition;
		
		// Use original velocity (not clipped) for step delta calculation
		var moveDelta = stepVelocity.WithZ( 0 ) * timeDelta;
		var deltaLen = moveDelta.Length;
		
		// If it's really low, we're probably moving straight up or down
		if ( deltaLen < MinVelocityThreshold )
		{
			// Keep regular move
			return;
		}
		
		var moveBack = Vector3.Zero;
		
		if ( deltaLen < MinStepDelta )
		{
			var newDelta = moveDelta.Normal * StepDeltaFallback;
			moveBack = moveDelta - newDelta;
			moveDelta = newDelta;
		}
		
		// Move forward
		var forwardTrace = BuildTrace( stepPosition, stepPosition + moveDelta ).Run();
		stepPosition = forwardTrace.EndPosition;
		
		// Step down
		var downTrace = BuildTrace( stepPosition, stepPosition + Vector3.Down * StepHeight ).Run();
		stepPosition = downTrace.EndPosition;
		
		// Verify we landed on something valid
		if ( !downTrace.Hit )
		{
			// Keep regular move
			return;
		}
		
		// Check if surface is too steep
		if ( downTrace.Normal.Angle( Vector3.Up ) > GroundAngle )
		{
			// Keep regular move
			return;
		}
		
		// Check if we actually moved further with the step
		var regularDist = startPosition.Distance( regularPosition.WithZ( startPosition.z ) );
		var stepDist = startPosition.Distance( stepPosition.WithZ( startPosition.z ) );
		
		if ( regularDist > stepDist )
		{
			// Regular move was better, keep it (already set)
			return;
		}
		
		// Apply move back if needed
		if ( !moveBack.IsNearZeroLength )
		{
			var moveBackTrace = BuildTrace( stepPosition, stepPosition + moveBack ).Run();
			stepPosition = moveBackTrace.EndPosition;
		}
		
		// Step move was better, use it
		WorldPosition = stepPosition;
		Velocity = stepVelocity;
	}
	
	/// <summary>
	/// Start a new bump iteration
	/// </summary>
	private void StartBump( Vector3 velocity )
	{
		_bumpVelocity = velocity;
		_clipPlaneCount = 0;
	}
	
	/// <summary>
	/// Try to add a collision plane and clip velocity
	/// </summary>
	private bool TryAddPlane( Vector3 normal, float bounce )
	{
		if ( _clipPlaneCount >= MaxClipPlanes )
		{
			return false;
		}
		
		_clipPlanes[_clipPlaneCount++] = normal;
		
		// First plane - apply bounce
		if ( _clipPlaneCount == 1 )
		{
			_bumpVelocity = ClipVelocity( _originalVelocity, normal, 1.0f + bounce );
			_originalVelocity = _bumpVelocity;
			Velocity = _bumpVelocity;
			
			return true;
		}
		
		// Try to clip to all planes
		if ( TryClipVelocity() )
		{
			// Hit floor and wall - slide along the intersection
			if ( _clipPlaneCount == 2 )
			{
				var dir = Vector3.Cross( _clipPlanes[0], _clipPlanes[1] ).Normal;
				var d = dir.Dot( Velocity );
				Velocity = dir * d;
			}
			else
			{
				Velocity = Vector3.Zero;
				return false;
			}
		}
		
		// Check if we're moving backwards
		if ( Velocity.Dot( _originalVelocity ) <= 0 )
		{
			Velocity = Vector3.Zero;
			return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Try to clip velocity to all planes
	/// </summary>
	private bool TryClipVelocity()
	{
		for ( var i = 0; i < _clipPlaneCount; i++ )
		{
			Velocity = ClipVelocity( _originalVelocity, _clipPlanes[i] );
			
			if ( IsMovingTowardsAnyPlane( i ) )
			{
				return false;
			}
		}
		
		return true;
	}
	
	/// <summary>
	/// Check if velocity is moving towards any plane (except skip index)
	/// </summary>
	private bool IsMovingTowardsAnyPlane( int skipIndex )
	{
		for ( var j = 0; j < _clipPlaneCount; j++ )
		{
			if ( j == skipIndex )
			{
				continue;
			}
			
			if ( Velocity.Dot( _clipPlanes[j] ) < 0 )
			{
				return false;
			}
		}
		
		return true;
	}
	
	/// <summary>
	/// Check if we're stuck and try to escape
	/// </summary>
	private bool TryUnstuck()
	{
		var result = BuildTrace( WorldPosition, WorldPosition ).Run();
		
		// Not stuck
		if ( !result.StartedSolid )
		{
			_stuckTries = 0;
			return false;
		}
		
		for ( var i = 0; i < UnstuckAttemptsPerTick; i++ )
		{
			var pos = WorldPosition + Vector3.Random.Normal * (_stuckTries / 2.0f);
			
			// First try the up direction for moving platforms
			if ( i == 0 )
			{
				pos = WorldPosition + Vector3.Up * UnstuckUpOffset;
			}
			
			result = BuildTrace( pos, pos ).Run();
			
			if ( !result.StartedSolid )
			{
				WorldPosition = pos;
				return false;
			}
		}
		
		_stuckTries++;
		return true;
	}
	
	/// <summary>
	/// Check if we're on ground and update ground normal
	/// </summary>
	private void CategorizePosition()
	{
		var wasGrounded = IsGrounded;
		var previousGroundObject = GroundObject;
		var vBumpOrigin = WorldPosition;
		var point = vBumpOrigin + Vector3.Down * GroundCheckDistance;
		
		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Velocity.z > FlyingUpwardThreshold )
		{
			ClearGround();
			return;
		}
		
		// Trace down for ground detection
		point.z -= wasGrounded ? StepHeight : GroundedStepDownDistance;
		var trace = BuildTrace( vBumpOrigin, point ).Run();
		
		// Not on ground if we didn't hit or surface is too steep
		if ( !trace.Hit || Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle )
		{
			ClearGround();
			return;
		}
		
		// We are on ground
		IsGrounded = true;
		GroundNormal = trace.Normal;
		GroundObject = trace.GameObject;
		GroundCollider = trace.Shape?.Collider;
		
		// Store ground transform when we first land or switch platforms
		if ( GroundObject != previousGroundObject )
		{
			_groundTransform = GroundObject.WorldTransform;
			_usedPlatformTransform = false;
		}
		
		// Snap to ground if we moved and hit
		if ( wasGrounded && !trace.StartedSolid && trace.Fraction > 0.0f && trace.Fraction < 1.0f )
		{
			WorldPosition = trace.EndPosition;
		}
		
		ResetJumpFlagOnLanding( wasGrounded );
	}
	
	/// <summary>
	/// Clear ground state
	/// </summary>
	private void ClearGround()
	{
		IsGrounded = false;
		GroundNormal = Vector3.Up;
		GroundObject = null;
		GroundCollider = null;
		_groundTransform = default;
		_groundVelocity = Vector3.Zero;
	}
	
	/// <summary>
	/// Ground movement with friction and acceleration
	/// </summary>
	private void GroundMove()
	{
		var friction = GetGroundFriction();
		ApplyFriction( friction );
		
		if ( _wishVelocity.Length < 0.01f )
		{
			return;
		}
		
		var wishDir = _wishVelocity.Normal;
		
		// Project movement onto ground plane
		wishDir -= GroundNormal * Vector3.Dot( wishDir, GroundNormal );
		wishDir = wishDir.Normal;
		Accelerate( wishDir, CurrentSpeed, Acceleration );
	}
	
	private float GetGroundFriction()
	{
		if ( !GroundObject.IsValid() )
		{
			return GroundFriction;
		}
		
		var collider = GroundObject.GetComponent<Collider>();
		
		if ( !collider.IsValid() )
		{
			return GroundFriction;
		}
		
		return collider.Friction ?? GroundFriction;
	}
	
	/// <summary>
	/// Air movement with reduced acceleration
	/// </summary>
	private void AirMove()
	{
		if ( _wishVelocity.Length < 0.01f )
		{
			return;
		}
		
		var wishDir = _wishVelocity.Normal;
		var wishSpeed = _wishVelocity.Length;
		Accelerate( wishDir, wishSpeed, AirAcceleration );
	}
	
	/// <summary>
	/// Apply friction to horizontal velocity
	/// </summary>
	private void ApplyFriction( float friction )
	{
		var speed = Velocity.Length;
		
		if ( speed < MinSpeedThreshold )
		{
			return;
		}
		
		// Calculate drop amount
		var control = speed < StopSpeed ? StopSpeed : speed;
		var drop = control * friction * Scene.FixedDelta;
		
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
	/// Accelerate the player toward wish velocity (Quake-style)
	/// </summary>
	private void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration )
	{
		var currentSpeed = Vector3.Dot( Velocity, wishDir );
		var addSpeed = wishSpeed - currentSpeed;
		
		if ( addSpeed <= 0 )
		{
			return;
		}
		
		var accelSpeed = acceleration * Scene.FixedDelta * wishSpeed;
		accelSpeed = MathF.Min( accelSpeed, addSpeed );
		Velocity += wishDir * accelSpeed;
	}
	
	/// <summary>
	/// By default, we assume you're using Terry, so we update animations using <see cref="CitizenAnimationHelper"/>. <br/>
	/// If you're not, you might want to update this method to run with your own model.
	/// </summary>
	protected override void UpdateAnimations()
	{
		// Calling base for in case we use it in the future.
		base.UpdateAnimations();
		
		if ( !AnimationHelper.IsValid() )
		{
			return;
		}
		
		AnimationHelper.IsGrounded = IsGrounded;
		AnimationHelper.DuckLevel = CrouchFactor;
		AnimationHelper.WithLook( EyeAngles.Forward * 100 );
		AnimationHelper.WithVelocity( Velocity );
		AnimationHelper.WithWishVelocity( IsProxy ? Velocity : _wishVelocity );
	}
	
	/// <summary>
	/// Clip velocity against a plane
	/// </summary>
	private static Vector3 ClipVelocity( Vector3 vel, Vector3 normal, float overbounce = 1.0f )
	{
		var backoff = Vector3.Dot( vel, normal ) * overbounce;
		var output = vel - normal * backoff;
		
		// Extra adjustment to prevent moving into the plane
		var adjust = Vector3.Dot( output, normal );
		
		if ( adjust < 0.0f )
		{
			output -= normal * adjust;
		}
		
		return output;
	}
}
