using Sandbox;

namespace Controllers.Movement;

public partial class WalkController3D : MovementController3D
{
	[Property]
	private bool Debug { get; set; }
	
	private void DrawDebugInfo()
	{
		Gizmo.Draw.ScreenText( $"IsGrounded: {IsGrounded}", new Vector2( 10, 10 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Velocity: {Velocity}", new Vector2( 10, 30 ), size: 20 );
		Gizmo.Draw.ScreenText( $"WishVelocity: {WishVelocity}", new Vector2( 10, 50 ), size: 20 );
		Gizmo.Draw.ScreenText( $"Speed: {_speed}", new Vector2( 10, 70 ), size: 20 );

		if ( CanJump )
		{
			Gizmo.Draw.ScreenText( $"WishJump: {WishJump}", new Vector2( 10, 90 ), size: 20 );
			Gizmo.Draw.ScreenText( $"Coyote Time: {_coyoteTime}", new Vector2( 10, 110 ), size: 20 );
			Gizmo.Draw.ScreenText( $"Jump Buffer: {_jumpBuffer}", new Vector2( 10, 130 ), size: 20 );
		}

		if ( CanCrouch )
		{
			Gizmo.Draw.ScreenText( $"WishCrouch: {WishCrouch}", new Vector2( 10, 150 ), size: 20 );
		}
	}
}
