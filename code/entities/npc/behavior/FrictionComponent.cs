using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class FrictionComponent : EntityComponent
{
	public float Amount { get; set; } = 4f;

	public void Update()
	{
		var speed = Entity.Velocity.Length;
		if ( speed < 0.1f ) return;

		var control = (speed < 100f) ? 100f : speed;
		var newSpeed = speed - (control * Time.Delta * Amount);

		if ( newSpeed < 0 ) newSpeed = 0;
		if ( newSpeed == speed ) return;

		newSpeed /= speed;
		Entity.Velocity *= newSpeed;
	}
}
