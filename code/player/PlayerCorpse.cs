using Sandbox;

namespace Facepunch.Forsaken;

public class PlayerCorpse : ModelEntity
{
	public Player Player { get; set; }

	private TimeSince TimeSinceSpawned { get; set; }

	public PlayerCorpse()
	{
		UsePhysicsCollision = true;
		TimeSinceSpawned = 0f;
		PhysicsEnabled = true;
	}

	public void CopyFrom( Player player )
	{
		RenderColor = player.RenderColor;

		SetModel( player.GetModelName() );
		TakeDecalsFrom( player );

		this.CopyBonesFrom( player );
		this.SetRagdollVelocityFrom( player );

		foreach ( var child in player.Children )
		{
			if ( child is BaseClothing e )
			{
				var model = e.GetModelName();
				var clothing = new ModelEntity();

				clothing.RenderColor = e.RenderColor;
				clothing.SetModel( model );
				clothing.SetParent( this, true );
			}
		}
	}

	public void ApplyForceToBone( Vector3 force, int forceBone )
	{
		PhysicsGroup.AddVelocity( force );

		if ( forceBone >= 0 )
		{
			var body = GetBonePhysicsBody( forceBone );

			if ( body != null )
				body.ApplyForce( force * 1000 );
			else
				PhysicsGroup.AddVelocity( force );
		}
	}

	public override void Spawn()
	{
		Tags.Add( "corpse" );
		base.Spawn();
	}

	[Event.Tick.Client]
	protected virtual void ClientTick()
	{
		if ( IsClientOnly && TimeSinceSpawned > 10f )
		{
			Delete();
		}
	}
}
