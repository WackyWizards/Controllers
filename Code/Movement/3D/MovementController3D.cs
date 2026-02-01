using Sandbox;
using Sandbox.Diagnostics;
using Controllers.Camera;

namespace Controllers.Movement;

/// <summary>
/// Contains the base for handling movement controls.
/// Inherently, this class doesn't do much by itself, just defines useful things for different controller types.
/// </summary>
public abstract class MovementController3D : Component
{
	[Property, Category( "Components" ), RequireComponent]
	public CameraController CameraController { get; set; }
	
	[Sync]
	public Vector3 WishVelocity { get; set; }
	
	// ReSharper disable once MemberCanBeProtected.Global
	[Sync]
	public Angles EyeAngles { get; set; }
	
	// ReSharper disable once MemberCanBeProtected.Global
	[Sync]
	public Vector3 Velocity { get; set; }
	
	private static readonly Logger Log = new( "MovementController" );
	
	protected override void OnUpdate()
	{
		if ( !IsProxy && CameraController.IsValid() )
		{
			EyeAngles = CameraController.EyeAngles;
		}
		
		UpdateAnimations();
	}
	
	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
		{
			return;
		}
		
		BuildWishVelocity();
		Move();
	}
	
	/// <summary>
	/// Build the wish velocity based on player input. Called before Move().
	/// </summary>
	protected virtual void BuildWishVelocity() { }
	
	/// <summary>
	/// Execute movement. Called after BuildWishVelocity().
	/// </summary>
	protected virtual void Move() { }
	
	/// <summary>
	/// Update character animations.
	/// </summary>
	protected virtual void UpdateAnimations() { }
}
