using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public abstract partial class Weapon : BaseWeapon
{
	public virtual string MuzzleAttachment => "muzzle";
	public virtual string MuzzleFlashEffect => "particles/pistol_muzzleflash.vpcf";
	public virtual string ImpactEffect => null;
	public virtual int ClipSize => 16;
	public virtual float ReloadTime => 3.0f;
	public virtual bool IsMelee => false;
	public virtual float MeleeRange => 100f;
	public virtual float DamageFalloffStart => 0f;
	public virtual float DamageFalloffEnd => 0f;
	public virtual float BulletRange => 20000f;
	public virtual string TracerEffect => null;
	public virtual bool ReloadAnimation => true;
	public virtual bool UnlimitedAmmo => false;
	public virtual bool IsPassive => false;
	public virtual float ChargeAttackDuration => 2f;
	public virtual DamageFlags DamageType => DamageFlags.Bullet;
	public virtual string ReloadSoundName => string.Empty;
	public virtual int HoldType => 1;
	public virtual int ViewModelMaterialGroup => 0;

	[Net, Change( nameof( OnWeaponItemChanged ) )]
	public NetInventoryItem Item { get; private set; }

	[Net]
	public int Slot { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceReload { get; set; }

	[Net, Predicted]
	public bool IsReloading { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceChargeAttack { get; set; }

	[Net, Predicted]
	public TimeSince TimeSincePrimaryHeld { get; set; }

	public float ChargeAttackEndTime { get; private set; }
	public AnimatedEntity AnimationOwner => Owner as AnimatedEntity;

	public Queue<float> RecoilQueue { get; set; } = new();
	private Sound ReloadSound { get; set; }

	public int AmmoClip
	{
		get
		{
			if ( WeaponItem.IsValid() )
			{
				return WeaponItem.Ammo;
			}

			return 0;
		}
		set
		{
			if ( WeaponItem.IsValid() )
			{
				WeaponItem.Ammo = value;
				WeaponItem.IsDirty = true;
			}
		}
	}

	public WeaponItem WeaponItem => Item.Value as WeaponItem;

	public int AvailableAmmo()
	{
		if ( Owner is not ForsakenPlayer owner ) return 0;
		return owner.GetAmmoCount( WeaponItem.AmmoType );
	}

	public void SetWeaponItem( WeaponItem item )
	{
		Item = new NetInventoryItem( item );
		OnWeaponItemChanged();
	}

	public float GetDamageFalloff( float distance, float damage )
	{
		if ( DamageFalloffEnd > 0f )
		{
			if ( DamageFalloffStart > 0f )
			{
				if ( distance < DamageFalloffStart )
				{
					return damage;
				}

				var falloffRange = DamageFalloffEnd - DamageFalloffStart;
				var difference = (distance - DamageFalloffStart);

				return Math.Max( damage - (damage / falloffRange) * difference, 0f );
			}

			return Math.Max( damage - (damage / DamageFalloffEnd) * distance, 0f );
		}

		return damage;
	}

	public virtual bool IsAvailable()
	{
		return true;
	}

	public virtual void PlayAttackAnimation()
	{
		AnimationOwner?.SetAnimParameter( "b_attack", true );
	}

	public virtual void PlayReloadAnimation()
	{
		AnimationOwner?.SetAnimParameter( "b_reload", true );
	}

	public override void ActiveStart( Entity owner )
	{
		base.ActiveStart( owner );
		PlaySound( $"weapon.pickup{Rand.Int( 1, 4 )}" );
		TimeSinceDeployed = 0f;
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		ReloadSound.Stop();
		TimeSinceReload = 0f;
		IsReloading = false;
	}

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void BuildInput()
	{
		var player = ForsakenPlayer.Me;

		if ( !player.IsValid() ) return;

		if ( RecoilQueue.TryDequeue( out var recoil ) )
		{
			var forward = new Angles( 0f, player.ViewAngles.yaw, 0f ).Forward;
			var recoilX = recoil / Screen.Width;
			var recoilY = recoil / Screen.Height;
			player.Cursor += new Vector2( forward.x * recoilX, forward.y * -recoilY );
		}
	}

	public override void Reload()
	{
		if ( IsMelee || IsReloading )
			return;

		if ( AmmoClip >= ClipSize )
			return;

		if ( Owner is ForsakenPlayer player )
		{
			if ( !UnlimitedAmmo )
			{
				if ( player.GetAmmoCount( WeaponItem.AmmoType ) <= 0 )
					return;
			}
		}

		TimeSinceReload = 0f;
		IsReloading = true;

		if ( ReloadAnimation )
			PlayReloadAnimation();

		if ( !string.IsNullOrEmpty( ReloadSoundName ) )
			ReloadSound = PlaySound( ReloadSoundName );

		DoClientReload();
	}

	public override void Simulate( Client owner )
	{
		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			TimeSincePrimaryHeld = 0f;
		}

		if ( owner.Pawn.LifeState == LifeState.Alive )
		{
			if ( ChargeAttackEndTime > 0f && Time.Now >= ChargeAttackEndTime )
			{
				OnChargeAttackFinish();
				ChargeAttackEndTime = 0f;
			}
		}
		else
		{
			ChargeAttackEndTime = 0f;
		}

		if ( !IsReloading )
		{
			base.Simulate( owner );
		}

		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}
	}

	public override bool CanPrimaryAttack()
	{
		if ( ChargeAttackEndTime > 0f && Time.Now < ChargeAttackEndTime )
			return false;

		if ( TimeSinceDeployed < 0.3f )
			return false;

		return base.CanPrimaryAttack();
	}

	public override bool CanSecondaryAttack()
	{
		if ( ChargeAttackEndTime > 0f && Time.Now < ChargeAttackEndTime )
			return false;

		return base.CanSecondaryAttack();
	}

	public virtual void StartChargeAttack()
	{
		ChargeAttackEndTime = Time.Now + ChargeAttackDuration;
	}

	public virtual void OnChargeAttackFinish() { }

	public virtual void OnReloadFinish()
	{
		IsReloading = false;

		if ( Owner is ForsakenPlayer player )
		{
			if ( !UnlimitedAmmo )
			{
				var ammo = player.TakeAmmo( WeaponItem.AmmoType, (ushort)(ClipSize - AmmoClip) );

				if ( ammo == 0 )
					return;

				AmmoClip += ammo;
			}
			else
			{
				AmmoClip = ClipSize;
			}
		}
	}

	[ClientRpc]
	public virtual void DoClientReload()
	{
		if ( ReloadAnimation )
		{
			ViewModelEntity?.SetAnimParameter( "reload", true );
		}
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		Rand.SetSeed( Time.Tick );

		ShootEffects();
		ShootBullet( 0.05f, 1.5f, WeaponItem.Damage, 3.0f );
	}

	public virtual void MeleeStrike( float damage, float force )
	{
		var forward = Owner.EyeRotation.Forward;
		forward = forward.Normal;

		foreach ( var trace in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * MeleeRange, 16f ) )
		{
			if ( !trace.Entity.IsValid() || trace.Entity.IsWorld )
			{
				OnMeleeAttackMissed( trace );
				continue;
			}

			if ( IsServer )
			{
				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithPosition( trace.EndPosition )
						.WithFlag( DamageFlags.Blunt )
						.WithForce( forward * 100f * force )
						.UsingTraceResult( trace )
						.WithAttacker( Owner )
						.WithWeapon( this );

					damageInfo.Damage = damage;

					trace.Entity.TakeDamage( damageInfo );
				}
			}

			OnMeleeAttackHit( trace.Entity );
		}
	}

	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		var forward = Owner.EyeRotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		foreach ( var trace in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * BulletRange, bulletSize ) )
		{
			if ( string.IsNullOrEmpty( ImpactEffect ) )
			{
				trace.Surface.DoBulletImpact( trace );
			}

			var fullEndPos = trace.EndPosition + trace.Direction * bulletSize;

			if ( !string.IsNullOrEmpty( TracerEffect ) )
			{
				var tracer = Particles.Create( TracerEffect, GetEffectEntity(), MuzzleAttachment );
				tracer?.SetPosition( 1, fullEndPos );
				tracer?.SetPosition( 2, trace.Distance );
			}

			if ( !string.IsNullOrEmpty( ImpactEffect ) )
			{
				var impact = Particles.Create( ImpactEffect, fullEndPos );
				impact?.SetForward( 0, trace.Normal );
			}

			if ( !IsServer )
				continue;

			if ( trace.Entity.IsValid() )
			{
				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithPosition( trace.EndPosition )
						.WithFlag( DamageType )
						.WithForce( forward * 100f * force )
						.UsingTraceResult( trace )
						.WithAttacker( Owner )
						.WithWeapon( this );

					damageInfo.Damage = GetDamageFalloff( trace.Distance, damage );

					trace.Entity.TakeDamage( damageInfo );
				}
			}
		}
	}

	public bool TakeAmmo( int amount )
	{
		if ( AmmoClip < amount )
			return false;

		AmmoClip -= amount;
		return true;
	}

	public override void CreateViewModel()
	{
		Host.AssertClient();
	}

	public override void CreateHudElements()
	{
		if ( Local.Hud == null ) return;
	}

	public bool IsUsable()
	{
		if ( IsMelee || ClipSize == 0 || AmmoClip > 0 )
		{
			return true;
		}

		return AvailableAmmo() > 0;
	}

	public override IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2f )
	{
		yield return Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "player" )
			.Ignore( Owner )
			.Ignore( this )
			.Size( radius )
			.Run();
	}

	protected void ApplyRecoil()
	{
		if ( IsClient && Prediction.FirstTime )
		{
			var time = TimeSincePrimaryHeld.Relative.Remap( 0f, 3f, 0f, 1f ) % 1f;
			var recoil = WeaponItem.RecoilCurve.Evaluate( time );
			RecoilQueue.Enqueue( recoil );
		}
	}

	protected virtual void OnMeleeAttackMissed( TraceResult trace ) { }

	protected virtual void OnMeleeAttackHit( Entity victim ) { }

	protected virtual void CreateMuzzleFlash()
	{
		if ( !string.IsNullOrEmpty( MuzzleFlashEffect ) )
		{
			Particles.Create( MuzzleFlashEffect, GetEffectEntity(), "muzzle" );
		}
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Host.AssertClient();

		if ( !IsMelee )
		{
			CreateMuzzleFlash();
		}

		ViewModelEntity?.SetAnimParameter( "fire", true );
	}

	protected virtual void OnWeaponItemChanged()
	{
		if ( IsServer && WeaponItem.IsValid() && !string.IsNullOrEmpty( WeaponItem.WorldModel ) )
		{
			SetModel( WeaponItem.WorldModel );
			SetMaterialGroup( WeaponItem.WorldModelMaterialGroup );
		}
	}

	protected virtual ModelEntity GetEffectEntity()
	{
		return EffectEntity;
	}

	protected void DealDamage( Entity target, Vector3 position, Vector3 force )
	{
		DealDamage( target, position, force, WeaponItem.Damage );
	}

	protected void DealDamage( Entity target, Vector3 position, Vector3 force, float damage )
	{
		var damageInfo = new DamageInfo()
			.WithAttacker( Owner )
			.WithWeapon( this )
			.WithPosition( position )
			.WithForce( force )
			.WithFlag( DamageType );

		damageInfo.Damage = damage;

		target.TakeDamage( damageInfo );
	}
}
