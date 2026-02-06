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
	
	[Sync]
	public Vector3 Velocity { get; set; }
	
	// ReSharper disable once MemberCanBePrivate.Global
	// ReSharper disable once MemberCanBeProtected.Global
	/// <summary>
	/// By default, if this controller has a <see cref="CameraController"/> assigned, uses it's EyeAngles. <br/>
	/// If you don't have one, you may set this to your own value.
	/// </summary>
	[Sync]
	public Angles EyeAngles { get; set; }
	
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
