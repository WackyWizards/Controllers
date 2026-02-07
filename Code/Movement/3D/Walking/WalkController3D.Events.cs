using System;
using Sandbox;

namespace Controllers.Movement;

public partial class WalkController3D
{
	/// <summary>
	/// Called when the character lands on the ground. Provides the impact velocity.
	/// </summary>
	public Action<float> OnLanded { get; set; }
	
	/// <summary>
	/// Called when the character leaves the ground
	/// </summary>
	public Action OnLeftGround { get; set; }
	
	/// <summary>
	/// Called when the character jumps. Provides the jump velocity.
	/// </summary>
	public Action<Vector3> OnJumped { get; set; }
	
	/// <summary>
	/// Called when the character starts crouching
	/// </summary>
	public Action OnCrouchStarted { get; set; }
	
	/// <summary>
	/// Called when the character stops crouching
	/// </summary>
	public Action OnCrouchEnded { get; set; }
	
	/// <summary>
	/// Called every frame while moving. Provides current velocity.
	/// </summary>
	public Action<Vector3> OnMove { get; set; }
	
	/// <summary>
	/// Called when velocity changes significantly
	/// </summary>
	public Action<Vector3, Vector3> OnVelocityChanged { get; set; } // old, new
	
	/// <summary>
	/// Called when the character collides with something during movement
	/// </summary>
	public Action<SceneTraceResult> OnCollision { get; set; }
	
	/// <summary>
	/// Called when the character hits a wall
	/// </summary>
	public Action<Vector3> OnHitWall { get; set; } // wall normal
	
	/// <summary>
	/// Called when the character hits the ceiling
	/// </summary>
	public Action<Vector3> OnHitCeiling { get; set; } // ceiling normal
	
	/// <summary>
	/// Called when stepping up
	/// </summary>
	public Action<float> OnStep { get; set; } // step height
	
	/// <summary>
	/// Called when the ground object changes (switching platforms)
	/// </summary>
	public Action<GameObject, GameObject> OnGroundChanged { get; set; } // old, new
	
	/// <summary>
	/// Called when standing on a moving platform
	/// </summary>
	public Action<GameObject, Vector3> OnPlatformMove { get; set; } // platform, delta
	
	/// <summary>
	/// Called when the ground surface changes
	/// </summary>
	public Action<Collider, Collider> OnGroundSurfaceChanged { get; set; } // old, new
	
	/// <summary>
	/// Called when the character gets stuck in geometry
	/// </summary>
	public Action<int> OnStuck { get; set; } // stuck attempt count
	
	/// <summary>
	/// Called when the character successfully unstucks
	/// </summary>
	public Action OnUnstuck { get; set; }
	
	/// <summary>
	/// Called when speed changes significantly
	/// </summary>
	public Action<float, float> OnSpeedChanged { get; set; } // old speed, new speed
	
	/// <summary>
	/// Called when an impulse is applied to the character
	/// </summary>
	public Action<Vector3> OnImpulseApplied { get; set; }
	
	/// <summary>
	/// Called when a force is applied to the character
	/// </summary>
	public Action<Vector3> OnForceApplied { get; set; }
	
	/// <summary>
	/// Called when forces are cleared
	/// </summary>
	public Action OnForcesCleared { get; set; }
	
	/// <summary>
	/// Called when an impulse is applied at a specific position
	/// </summary>
	public Action<Vector3, Vector3> OnImpulseAppliedAt { get; set; }
	
	/// <summary>
	/// Called when a force is applied at a specific position
	/// </summary>
	public Action<Vector3, Vector3> OnForceAppliedAt { get; set; }
}
