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
	[Property, Group( "Inputs" ), InputAction]
	public string CrouchInput { get; set; } = "duck";
	
	private bool _wishCrouch;
	private float _currentCrouchFactor;
	private float _originalCapsuleHeight;
	
	private void HandleCrouchInput()
	{
		if ( !CanCrouch )
		{
			return;
		}
		
		if ( Input.Pressed( CrouchInput ) )
		{
			_wishCrouch = true;
		}
		else if ( Input.Released( CrouchInput ) )
		{
			_wishCrouch = false;
		}
		
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
	
	private void TryCrouch()
	{
		IsCrouched = true;
		CurrentSpeed = CrouchSpeed;
		BodyHeight = CrouchHeight;
		_maxs = _maxs.WithZ( CrouchHeight );
	}
	
	private void TryUncrouch()
	{
		if ( !CanStandUp() )
		{
			return;
		}
		
		IsCrouched = false;
		CurrentSpeed = WalkSpeed;
		BodyHeight = _originalCapsuleHeight;
		_maxs = _maxs.WithZ( _originalCapsuleHeight );
	}
	
	private bool CanStandUp()
	{
		var standMins = _mins;
		var standMaxs = _maxs.WithZ( _originalCapsuleHeight );
		var standBBox = new BBox( standMins, standMaxs );
		
		// Check if there's room to stand
		var tr = Scene.Trace.Box( standBBox, WorldPosition, WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "nocollide" ).Run();
		
		return !tr.Hit;
	}
	
	private void UpdateCrouchVisuals()
	{
		var target = IsCrouched ? 1f : 0f;
		_currentCrouchFactor = _currentCrouchFactor.LerpTo( target, Time.Delta * 10f );
	}
}
