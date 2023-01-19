using Sandbox;
using System;

namespace Facepunch.Forsaken;

public abstract partial class ProjectileWeapon<T> : Weapon where T : Projectile, new()
{
	public virtual string ProjectileModel => "";
	public virtual float ProjectileRadius => 10f;
	public virtual float ProjectileLifeTime => 10f;
	public virtual int ProjectileCount => 1;
	public virtual string TrailEffect => null;
	public virtual string HitSound => null;
	public virtual float InheritVelocity => 0f;
	public virtual float Gravity => 50f;
	public virtual float Speed => 2000f;
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

		for ( var i = 0; i < ProjectileCount; i++ )
		{
			var projectile = new T()
			{
				ExplosionEffect = ImpactEffect,
				FaceDirection = true,
				IgnoreEntity = this,
				TrailEffect = TrailEffect,
				Simulator = player.Projectiles,
				Attacker = player,
				HitSound = HitSound,
				LifeTime = ProjectileLifeTime,
				Gravity = Gravity,
				ModelName = ProjectileModel
			};

			OnCreateProjectile( projectile );

			var eyePosition = player.EyePosition;
			var forward = player.EyeRotation.Forward;
			var position = eyePosition + forward * 40f;
			var muzzle = GetMuzzlePosition();

			if ( muzzle.HasValue )
			{
				position = muzzle.Value;
			}

			var trace = Trace.Ray( eyePosition, position )
				.Ignore( player )
				.Ignore( this )
				.Run();

			if ( trace.Hit )
			{
				// Let's roll it back a bit because we may be poking through a wall.
				position = trace.EndPosition - trace.Direction * 4f;
			}

			var direction = forward;
			direction += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random).WithZ( 0f ) * Spread * 0.25f;
			direction = direction.Normal;

			var velocity = (direction * Speed) + (player.Velocity * InheritVelocity);
			velocity = AdjustProjectileVelocity( velocity );

			position -= direction * Speed * Time.Delta;
			projectile.Initialize( position, velocity, ProjectileRadius, ( p, t ) => OnProjectileHit( (T)p, t ) );

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
		if ( Game.IsServer && trace.Entity.IsValid() )
		{
			var distance = trace.Entity.Position.Distance( projectile.StartPosition );
			var damage = GetDamageFalloff( distance, WeaponItem.Damage );
			DealDamage( trace.Entity, projectile.Position, projectile.Velocity * 0.1f, damage );
		}
	}
}
