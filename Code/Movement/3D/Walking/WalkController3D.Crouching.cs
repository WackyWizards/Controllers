using Sandbox;

namespace Controllers.Movement;

public partial class WalkController3D
{
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	public bool CanCrouch { get; set; } = true;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	public float CrouchHeight { get; set; } = 36f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" )]
	public float CrouchSpeed { get; set; } = 100f;
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, FeatureEnabled( "CanCrouch", Title = "Crouching" ), ReadOnly, Sync]
	public bool IsCrouched { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Property, Feature( "CanCrouch" ), InputAction]
	public string CrouchInput { get; set; } = "duck";
	
	public float CrouchFactor { get; private set; }
	
	private bool _wishCrouch;
	private float _originalCapsuleHeight;
	
	/// <summary>
	/// Called internally to handle crouch input and state changes
	/// </summary>
	private void HandleCrouchInput()
	{
		if ( !CanCrouch )
		{
			return;
		}
		
		// Check for crouch input
		if ( Input.Pressed( CrouchInput ) )
		{
			_wishCrouch = true;
		}
		else if ( Input.Released( CrouchInput ) )
		{
			_wishCrouch = false;
		}
		
		// Update crouch state
		if ( _wishCrouch && !IsCrouched )
		{
			TryCrouch();
		}
		else if ( !_wishCrouch && IsCrouched )
		{
			TryUncrouch();
		}
		
		UpdateCrouchVisuals();
	}
	
	/// <summary>
	/// Attempt to crouch.
	/// </summary>
	public bool TryCrouch()
	{
		if ( !CanCrouch || IsCrouched )
		{
			return false;
		}
		
		IsCrouched = true;
		CurrentSpeed = CrouchSpeed;
		BodyHeight = CrouchHeight;
		_maxs = _maxs.WithZ( CrouchHeight );
		OnCrouchStarted?.Invoke();
		
		return true;
	}
	
	/// <summary>
	/// Attempt to stand up from crouch.
	/// </summary>
	public bool TryUncrouch()
	{
		if ( !IsCrouched || !CanStandUp() )
		{
			return false;
		}
		
		IsCrouched = false;
		CurrentSpeed = WalkSpeed;
		BodyHeight = _originalCapsuleHeight;
		_maxs = _maxs.WithZ( _originalCapsuleHeight );
		OnCrouchEnded?.Invoke();
		
		return true;
	}
	
	/// <summary>
	/// Check if there's room to stand up
	/// </summary>
	private bool CanStandUp()
	{
		var standMins = _mins;
		var standMaxs = _maxs.WithZ( _originalCapsuleHeight );
		var standBBox = new BBox( standMins, standMaxs );
		
		// Check if there's room to stand
		var tr = Scene.Trace.Box( standBBox, WorldPosition, WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "nocollide" )
			.Run();
		
		return !tr.Hit;
	}
	
	/// <summary>
	/// Smoothly interpolate the visual crouch factor
	/// </summary>
	private void UpdateCrouchVisuals()
	{
		var target = IsCrouched ? 1f : 0f;
		CrouchFactor = CrouchFactor.LerpTo( target, Time.Delta * 10f );
	}
}
