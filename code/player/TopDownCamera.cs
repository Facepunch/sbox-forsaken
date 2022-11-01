using System;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class TopDownCamera : CameraMode
{
	public float Height { get; set; } = 450f;
	public float MoveSpeed { get; set; } = 20f;

	public override void Update()
	{
		if ( Local.Pawn is not Player pawn )
			return;

		var targetPosition = pawn.Position.WithZ( pawn.Position.z + Height );

		Position = Position.LerpTo( targetPosition, Time.Delta * MoveSpeed );
		Rotation = Rotation.LookAt( Vector3.Down );
		FieldOfView = 70f;
		Viewer = null;
	}
}
