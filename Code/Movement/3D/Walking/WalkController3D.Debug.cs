using System.Linq;
using Sandbox;

namespace Controllers.Movement;

public partial class WalkController3D
{
	[Property]
	private bool Debug { get; set; }
	
	private void DrawDebugInfo()
	{
		Gizmo.Draw.ScreenText( $"IsGrounded: {IsGrounded}", new Vector2( 10, 10 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Velocity: {Velocity.Length:F1}", new Vector2( 10, 30 ), size: 20 );
		Gizmo.Draw.ScreenText( $"WishVelocity: {_wishVelocity.Length:F1}", new Vector2( 10, 50 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Current speed: {CurrentSpeed}", new Vector2( 10, 70 ), size: 20 );
		
		if ( CanJump )
		{
			Gizmo.Draw.ScreenText( $"Coyote: {_coyoteTime}", new Vector2( 10, 90 ), size: 20 );
			Gizmo.Draw.ScreenText( $"Jump Buffer: {_jumpBuffer}", new Vector2( 10, 110 ), size: 20 );
		}
		
		// Draw collision bbox
		if ( !Rigidbody.IsValid() )
		{
			using ( Gizmo.Scope( "bbox", Transform.World ) )
			{
				Gizmo.Draw.Color = IsGrounded ? Color.Green : Color.Red;
				Gizmo.Draw.LineBBox( GetBBox() );
			}
		}
		else
		{
			// Draw each collider in the list
			using ( Gizmo.Scope( "colliders", Transform.World ) )
			{
				Gizmo.Draw.Color = IsGrounded ? Color.Green : Color.Red;
				
				// Get colliders to draw (use list if populated, otherwise find all colliders)
				var collidersToDraw = Colliders.Count > 0
					? Colliders
					: GetComponentsInChildren<Collider>().ToList();
				
				foreach ( var collider in collidersToDraw )
				{
					if ( !collider.IsValid() )
					{
						continue;
					}
					
					// Draw based on collider type
					if ( collider is CapsuleCollider capsuleCollider )
					{
						var capsule = new Capsule( capsuleCollider.Start, capsuleCollider.End, capsuleCollider.Radius );
						Gizmo.Draw.LineCapsule( capsule );
					}
					else if ( collider is BoxCollider box )
					{
						using ( Gizmo.Scope( "box", box.LocalTransform ) )
						{
							Gizmo.Draw.LineBBox( box.LocalBounds );
						}
					}
					else if ( collider is SphereCollider sphere )
					{
						Gizmo.Draw.LineSphere( sphere.Center, sphere.Radius );
					}
				}
			}
		}
	}
}
