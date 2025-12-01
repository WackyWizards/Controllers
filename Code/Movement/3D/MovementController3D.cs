using System.Linq;
using System.Collections.Generic;
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
	public Rigidbody Rigidbody { get; set; }

	[Property, Category( "Components" ), RequireComponent]
	public CameraController CameraController { get; set; }
	
	// ReSharper disable once MemberCanBeProtected.Global
	[Property, Category( "Components" )]
	public List<Collider> Colliders { get; set; } = [];

	[Sync]
	// ReSharper disable once MemberCanBeProtected.Global
	public Vector3 WishVelocity { get; set; }

	[Sync]
	// ReSharper disable once MemberCanBeProtected.Global
	public Angles EyeAngles { get; set; }
	
	// ReSharper disable once MemberCanBeProtected.Global
	public Vector3 Velocity => Rigidbody?.Velocity ?? Vector3.Zero;

	private static readonly Logger Log = new("MovementController");

	protected override void OnStart()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( Colliders is null || Colliders.Count == 0 || Colliders.Any( c => !c.IsValid() ) )
		{
			Log.Error( $"{this} requires at least one valid collider." );
		}
	}

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

		CheckGround();
		CalculateVelocity();
		Move();
	}

	/// <summary>
	/// Check if the controller is grounded.
	/// This is here to make sure it's called before <see cref="CalculateVelocity"/> and <see cref="Move"/> for your convenience.
	/// </summary>
	protected virtual void CheckGround() { }

	/// <summary>
	/// Calculate your velocity and wishvelocity here.
	/// </summary>
	protected virtual void CalculateVelocity() { }

	/// <summary>
	/// Actual movement is usually implemented here.
	/// </summary>
	protected virtual void Move() { }

	/// <summary>
	/// Update character animations.
	/// </summary>
	[Rpc.Broadcast]
	protected virtual void UpdateAnimations() { }
}
