using Sandbox;
using System;
using Conna.Projectiles;

namespace Facepunch.Forsaken;

public abstract partial class ProjectileWeapon<T> : Weapon where T : Projectile, new()
{
	public virtual string ProjectileData => "";
	public virtual int ProjectileCount => 1;
	public virtual float InheritVelocity => 0f;
	public virtual float Spread => 0.05f;

	public override void AttackPrimary()
	{
		if ( Prediction.FirstTime )
        {
			Game.SetRandomSeed( Time.Tick );
			FireProjectile();
        }
	}

	public virtual void FireProjectile()
	{
		if ( Owner is not ForsakenPlayer player )
			return;

		var cursorTrace = Trace.Ray( player.CameraPosition, player.CameraPosition + player.CursorDirection * 3000f )
			.UseHitboxes()
			.WithoutTags( "trigger" )
			.WithAnyTags( "solid", "world", "player", "npc" )
			.Ignore( player )
			.Ignore( this )
			.Run();

		var eyePosition = player.EyePosition;

		for ( var i = 0; i < ProjectileCount; i++ )
		{
			var projectile = Projectile.Create<T>( ProjectileData );

			projectile.IgnoreEntity = this;
			projectile.Simulator = player.Projectiles;
			projectile.Attacker = player;

			OnCreateProjectile( projectile );

			var position = eyePosition + player.EyeRotation.Forward * 40f;
			var muzzle = GetMuzzlePosition();

			if ( muzzle.HasValue )
			{
				position = muzzle.Value;
			}

			var trace = Trace.Ray( eyePosition, position )
				.WithoutTags( "trigger" )
				.Ignore( player )
				.Ignore( this )
				.Run();

			if ( trace.Hit )
			{
				// Let's roll it back a bit because we may be poking through a wall.
				position = trace.EndPosition - trace.Direction * 4f;
			}

			Vector3 direction;

			if ( player.IsAiming() )
				direction = (cursorTrace.EndPosition - position).Normal;
			else
				direction = player.EyeRotation.Forward;

			var spread = (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * Spread * 0.25f;

			if ( !ForsakenGame.Isometric )
				spread = spread.WithZ( 0f );

			direction += spread;
			direction = direction.Normal;

			var speed = projectile.Data.Speed.GetValue();
			var velocity = (direction * speed) + (player.Velocity * InheritVelocity);
			velocity = AdjustProjectileVelocity( velocity );

			position -= direction * speed * Time.Delta;
			projectile.Initialize( position, velocity, ( p, t ) => OnProjectileHit( (T)p, t ) );

			OnProjectileFired( projectile );
		}
	}

	protected virtual void OnProjectileFired( T projectile )
	{

	}

	protected virtual Vector3 AdjustProjectileVelocity( Vector3 velocity )
	{
		return velocity;
	}

	protected virtual float ModifyDamage( Entity victim, float damage )
	{
		return damage;
	}

	protected virtual void DamageInRadius( Vector3 position, float radius, float baseDamage, float force = 1f )
	{
		var entities = FindInSphere( position, radius );

		foreach ( var entity in entities )
		{
			var direction = (entity.Position - position).Normal;
			var distance = entity.Position.Distance( position );
			var damage = Math.Max( baseDamage - ((baseDamage / radius) * distance), 0f );

			damage = ModifyDamage( entity, damage );

			DealDamage( entity, position, direction * 100f * force, damage );
		}
	}

	protected virtual Vector3? GetMuzzlePosition()
	{
		var muzzle = GetAttachment( MuzzleAttachment );
		return muzzle.HasValue ? muzzle.Value.Position : null;
	}

	protected virtual void OnCreateProjectile( T projectile )
	{

	}

	protected virtual void OnProjectileHit( T projectile, TraceResult trace )
	{
		if ( Game.IsServer && trace.Entity is IDamageable victim )
		{
			var info = new DamageInfo()
				.WithAttacker( Owner )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithForce( projectile.Velocity * 0.02f )
				.WithTag( DamageType )
				.UsingTraceResult( trace );

			info.Damage = GetDamageFalloff( projectile.StartPosition.Distance( victim.Position ), WeaponItem.Damage );

			victim.TakeDamage( info );
		}
	}
}
