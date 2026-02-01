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
		using ( Gizmo.Scope( "bbox", Transform.World ) )
		{
			Gizmo.Draw.Color = IsGrounded ? Color.Green : Color.Red;
			Gizmo.Draw.LineBBox( GetBBox() );
		}
	}
}
