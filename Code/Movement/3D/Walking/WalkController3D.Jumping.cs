using Sandbox;

namespace Controllers.Movement;

public partial class WalkController3D
{
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	public bool CanJump { get; set; } = true;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	public float JumpPower { get; set; } = 300f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// Delay between allowing the player to jump again after landing.
	/// </summary>
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	public float LandingGracePeriod { get; set; } = 0.1f;

	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	public float CoyoteTimeAmount { get; set; } = 0.15f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanJump", Title = "Jumping" )]
	public float JumpBufferTime { get; set; } = 0.2f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Feature( "CanJump" ), InputAction]
	public string JumpInput { get; set; } = "Jump";
	
	private bool _wishJump;
	private TimeUntil _coyoteTime;
	private TimeUntil _jumpBuffer;
	private bool _wasGroundedLastFrame;
	private bool _hasJumpedSinceGrounded;
	private TimeSince _timeSinceLanded;
	
	/// <summary>
	/// Track when we leave the ground to enable coyote time
	/// </summary>
	private void HandleCoyoteTime()
	{
		if ( _wasGroundedLastFrame && !IsGrounded )
		{
			_coyoteTime = CoyoteTimeAmount;
		}
		
		_wasGroundedLastFrame = IsGrounded;
	}
	
	/// <summary>
	/// Handle jump input and buffering
	/// </summary>
	private void HandleJumpInput()
	{
		if ( !CanJump )
		{
			return;
		}
		
		// Buffer jump input
		if ( Input.Pressed( JumpInput ) )
		{
			_jumpBuffer = JumpBufferTime;
		}
		
		var hasBeenGroundedLongEnough = IsGrounded && _timeSinceLanded >= LandingGracePeriod;
		
		// Can jump if: (grounded long enough OR coyote time active) AND haven't jumped since landing
		var canJumpNow = (hasBeenGroundedLongEnough || _coyoteTime > 0) && !_hasJumpedSinceGrounded;
		
		if ( _jumpBuffer > 0 && canJumpNow )
		{
			_wishJump = true;
			_jumpBuffer = 0;
			_coyoteTime = 0;
		}
		else if ( Input.Released( JumpInput ) )
		{
			_wishJump = false;
		}
	}
	
	/// <summary>
	/// Execute the jump if conditions are met. Returns true if jumped.
	/// </summary>
	private bool Jump()
	{
		if ( !_wishJump )
		{
			return false;
		}
		
		Velocity = Velocity.WithZ( JumpPower );
		_wishJump = false;
		_hasJumpedSinceGrounded = true;
		IsGrounded = false;
		
		return true;
	}
	
	/// <summary>
	/// Reset jump flags when landing
	/// </summary>
	private void ResetJumpFlagOnLanding( bool wasGrounded )
	{
		if ( wasGrounded || !IsGrounded )
		{
			return;
		}
		
		_hasJumpedSinceGrounded = false;
		_timeSinceLanded = 0; // Start the grace period timer
	}
}
