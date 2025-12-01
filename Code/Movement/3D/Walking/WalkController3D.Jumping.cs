using Sandbox;

namespace Controllers.Movement;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class WalkController3D : MovementController3D
{
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public bool CanJump { get; set; } = true;

	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public float JumpForce { get; set; } = 250.0f;
	
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public float CoyoteTime { get; set; } = 0.15f;
	
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public float JumpBufferTime { get; set; } = 0.2f;
	
	[Property, Group( "Inputs" ), InputAction]
	// ReSharper disable once MemberCanBePrivate.Global
	public string JumpInput { get; set; } = "Jump";
	
	private bool WishJump { get; set; }
	
	private TimeUntil _coyoteTime;
	private TimeUntil _jumpBuffer;
	private bool _wasGroundedLastFrame;
	private bool _hasJumpedSinceGrounded;
	
	private void HandleCoyoteTime()
	{
		// Just left ground, start coyote time
		if ( _wasGroundedLastFrame && !IsGrounded )
		{
			_coyoteTime = CoyoteTime;
		}
	}

	private void HandleJumpInput()
	{
		if ( !CanJump )
		{
			return;
		}

		// Jumping inputs with buffering
		if ( Input.Pressed( JumpInput ) )
		{
			_jumpBuffer = JumpBufferTime;
		}

		// Check if we can jump (either grounded or in coyote time) and have a buffered jump
		if ( _jumpBuffer > 0 && (_coyoteTime > 0 || IsGrounded) && !_hasJumpedSinceGrounded )
		{
			WishJump = true;
			_jumpBuffer = 0; // Consume the buffer
			_coyoteTime = 0; // Consume coyote time
		}
		else if ( Input.Released( JumpInput ) )
		{
			WishJump = false;
		}
	}

	private void ExecuteJump()
	{
		if ( !WishJump )
		{
			return;
		}

		var jumpImpulse = Vector3.Up * JumpForce * Rigidbody.Mass;
		Rigidbody.ApplyImpulse( jumpImpulse );
		
		WishJump = false;
		_hasJumpedSinceGrounded = true;
	}

	private void ResetJumpFlagOnLanding( bool wasGrounded )
	{
		// Reset jump flag when landing
		if ( !wasGrounded && IsGrounded )
		{
			_hasJumpedSinceGrounded = false;
		}
	}
}
