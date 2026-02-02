using Sandbox;
using Sandbox.Diagnostics;
using Controllers.Camera;

namespace Controllers.Movement;

public abstract class MovementController3D : Component, IScenePhysicsEvents
{
	[Property, Category( "Components" )]
	public CameraController CameraController { get; set; }
	
	[Sync]
	public Vector3 WishVelocity { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	[Sync]
	public Angles EyeAngles { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
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
	
	void IScenePhysicsEvents.PrePhysicsStep()
	{
		if ( IsProxy )
		{
			return;
		}
		
		BuildWishVelocity();
		PreMove();
	}
	
	void IScenePhysicsEvents.PostPhysicsStep()
	{
		if ( IsProxy )
		{
			return;
		}
		
		Move();
		PostMove();
	}
	
	/// <summary>
	/// Build the wish velocity based on player input.
	/// </summary>
	protected virtual void BuildWishVelocity() { }
	
	/// <summary>
	/// Called before physics simulation (good for input gathering)
	/// </summary>
	protected virtual void PreMove() { }
	
	/// <summary>
	/// Execute movement after physics simulation
	/// </summary>
	protected virtual void Move() { }
	
	/// <summary>
	/// Called after movement is complete
	/// </summary>
	protected virtual void PostMove() { }
	
	/// <summary>
	/// Update character animations.
	/// </summary>
	protected virtual void UpdateAnimations() { }
}
