using Sandbox;

namespace Controllers.Camera;

public abstract class CameraController : Component
{
	[Sync]
	public Angles EyeAngles { get; set; }

	[Property]
	private bool UsePreferredFov { get; set; } = true;
    
	protected CameraComponent Camera => Scene.Camera;

	protected virtual void UpdateCameraPosition() {}
	protected virtual void UpdateCameraRotation() {}

	protected override void OnStart()
	{
		if ( IsProxy )
		{
			return;
		}
		
		if ( Camera.IsValid() && UsePreferredFov )
		{
			Camera.FieldOfView = Preferences.FieldOfView;
		}
		
		base.OnStart();
	}

	protected override void OnUpdate()
	{		
		if ( IsProxy )
		{
			return;
		}

		UpdateCameraPosition();
		UpdateCameraRotation();
	}
}
