using System;
using System.Linq;
using Sandbox;

namespace Controllers.Movement;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class WalkController3D : MovementController3D
{
	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	private bool CanCrouch { get; set; } = true;

	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	private float CrouchHeight { get; set; } = 0.6f;

	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	private float CrouchTransitionSpeed { get; set; } = 7.0f;

	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" ), ReadOnly, Sync]
	private bool IsCrouched { get; set; } = false;

	[Property, Group( "Inputs" ), InputAction]
	private string CrouchInput { get; set; } = "duck";

	private bool WishCrouch { get; set; }
	
	/// <summary>
	/// 0 = standing, 1 = fully crouched
	/// </summary>
	private float _currentCrouchFactor;

	private void Crouch()
	{
		if ( !CanCrouch || IsCrouched )
		{
			return;
		}

		SetCrouchState( true );
	}

	private void Uncrouch()
	{
		if ( !CanCrouch || !IsCrouched )
		{
			return;
		}

		if ( CanStandUp() )
		{
			SetCrouchState( false );
		}
	}

	private bool CanStandUp()
	{
		var collider = Colliders.FirstOrDefault( x => x is CapsuleCollider ) as CapsuleCollider;
		if ( !collider.IsValid() )
		{
			return false;
		}

		var start = WorldPosition;
		var heightDifference = _originalCapsuleHeight - collider.End.z;
		var end = start + Vector3.Up * heightDifference;
		var capsule = new Capsule( collider.Start, new Vector3( 0, 0, _originalCapsuleHeight ), collider.Radius );

		var trace = Scene.Trace.Capsule( capsule, start, end )
			.WithoutTags( "player", "nocollide" )
			.Run();

		return !trace.Hit;
	}

	private bool CanUncrouch()
	{
		return CanStandUp();
	}

	private void SetCrouchState( bool crouched )
	{
		if ( !CanCrouch )
		{
			return;
		}

		IsCrouched = crouched;
		
		// Set crouch factor - the lerping will happen in HandleCrouch
		_currentCrouchFactor = crouched ? 1.0f : 0.0f;
		
		// Only check for obstacles when trying to stand up
		if ( !crouched && !CanStandUp() )
		{
			return;
		}
	}

	private void HandleCrouch()
	{
		// Smooth transition of crouch factor
		var targetFactor = IsCrouched ? 1.0f : 0.0f;
		var delta = CrouchTransitionSpeed * Scene.FixedDelta;
		_currentCrouchFactor = MathX.Lerp( _currentCrouchFactor, targetFactor, delta );

		// Snap to final value when close enough
		if ( MathF.Abs( _currentCrouchFactor - targetFactor ) < 0.01f )
		{
			_currentCrouchFactor = targetFactor;
		}

		var bottomCollider = Colliders.FirstOrDefault( x => x is BoxCollider ) as BoxCollider;
		if ( !bottomCollider.IsValid() )
		{
			return;
		}

		const float crouchFactor = 0.7f;
		var scaleFactor = MathX.Lerp( 1.0f, crouchFactor, _currentCrouchFactor );

		// Update box collider scale
		bottomCollider.Scale = new Vector3(
			_originalBoxSize.x,
			_originalBoxSize.y,
			_originalBoxSize.z * scaleFactor
		);

		// Adjust the capsule collider
		var topCollider = Colliders.FirstOrDefault( x => x is CapsuleCollider ) as CapsuleCollider;
		if ( !topCollider.IsValid() )
		{
			return;
		}

		var targetHeight = MathX.Lerp( _originalCapsuleHeight, CrouchHeight, _currentCrouchFactor );
		topCollider.End = new Vector3( 0, 0, targetHeight );
	}
	
	private void HandleCrouchInput()
	{
		if ( !CanCrouch )
		{
			return;
		}

		if ( Input.Pressed( CrouchInput ) )
		{
			WishCrouch = true;
		}
		else if ( Input.Released( CrouchInput ) )
		{
			WishCrouch = false;
		}

		ProcessCrouchState();
	}

	private void ProcessCrouchState()
	{
		if ( WishCrouch )
		{
			Crouch();
		}
		else if ( CanUncrouch() && !WishCrouch )
		{
			Uncrouch();
		}
	}

	private void HandleCrouchTransition()
	{
		// Handle crouching
		if ( CanCrouch && WishCrouch != IsCrouched )
		{
			HandleCrouch();
		}

		// Smooth crouch factor transition
		if ( !IsCrouched )
		{
			return;
		}

		var delta = CrouchTransitionSpeed * Scene.FixedDelta;
		if ( _currentCrouchFactor != 0.0f )
		{
			_currentCrouchFactor = MathX.Lerp( _currentCrouchFactor, 0.0f, delta );
		}
	}
}
