using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Weapon : BaseWeapon
{
	public virtual string MuzzleAttachment => "muzzle";
	public virtual string MuzzleFlashEffect => null;
	public virtual string ImpactEffect => null;
	public virtual int ClipSize => WeaponItem.ClipSize;
	public virtual float ReloadTime => 3.0f;
	public virtual bool IsMelee => false;
	public virtual float MeleeRange => 100f;
	public virtual float BulletRange => 20000f;
	public virtual string TracerEffect => null;
	public virtual bool ReloadAnimation => true;
	public virtual bool UnlimitedAmmo => false;
	public virtual bool IsPassive => false;
	public virtual float ChargeAttackDuration => 2f;
	public virtual string DamageType => "bullet";
	public virtual string ReloadSoundName => string.Empty;
	public virtual CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Pistol;
	public virtual int ViewModelMaterialGroup => 0;

	[Net, Change( nameof( OnWeaponItemChanged ) )]
	private NetInventoryItem InternalWeaponItem { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; protected set; }

	[Net, Predicted]
	public TimeSince TimeSinceChargeAttack { get; protected set; }

	[Net, Predicted]
	public TimeSince TimeSincePrimaryHeld { get; protected set; }

	[Net]
	public TimeSince TimeSinceReload { get; protected set; }

	[Net]
	public bool IsReloading { get; protected set; }

	[Net]
	private string AmmoItemId { get; set; }

	public AnimatedEntity AnimationOwner => Owner as AnimatedEntity;
	public float ChargeAttackEndTime { get; private set; }

	private TimeSince TimeSinceReloadPressed { get; set; }
	private Queue<float> RecoilQueue { get; set; } = new();
	private bool WasReloading { get; set; }
	private Sound ReloadSound { get; set; }

	public int AmmoClip
	{
		get
		{
			if ( WeaponItem.IsValid() )
			{
				return WeaponItem.AmmoCount;
			}

			return 0;
		}
		set
		{
			if ( WeaponItem.IsValid() )
			{
				WeaponItem.AmmoCount = value;
			}
		}
	}

	public WeaponItem WeaponItem => InternalWeaponItem.IsValid() ? InternalWeaponItem.Value as WeaponItem : null;

	public int AvailableAmmo()
	{
		if ( Owner is not ForsakenPlayer owner )
			return 0;

		if ( !WeaponItem.IsValid() )
			return 0;

		if ( !WeaponItem.AmmoDefinition.IsValid() )
			return 0;

		return owner.GetAmmoCount( WeaponItem.AmmoDefinition.UniqueId );
	}

	public bool SetAmmoDefinition( AmmoItem item )
	{
		Game.AssertServer();

		if ( !WeaponItem.IsValid() ) return false;
		if ( WeaponItem.AmmoDefinition == item ) return false;
		if ( Owner is not ForsakenPlayer player ) return false;

		if ( WeaponItem.AmmoDefinition.IsValid() && AmmoClip > 0 )
		{
			var oldAmmoItem = InventorySystem.DuplicateItem( WeaponItem.AmmoDefinition );
			oldAmmoItem.StackSize = (ushort)AmmoClip;

			var remaining = player.TryGiveItem( oldAmmoItem );
			if ( remaining > 0 )
			{
				AmmoClip = remaining;
				return false;
			}
		}

		WeaponItem.AmmoDefinition = item;
		WeaponItem.IsDirty = true;

		AmmoClip = 0;

		return true;
	}

	public void SetWeaponItem( WeaponItem item )
	{
		InternalWeaponItem = new NetInventoryItem( item );
		OnWeaponItemChanged();
	}

	public float GetDamageFalloff( float distance, float damage )
	{
		return damage * WeaponItem.DamageFalloff.Evaluate( distance );
	}

	public virtual bool IsAvailable()
	{
		return true;
	}

	public virtual void PlayAttackAnimation()
	{
		AnimationOwner?.SetAnimParameter( "b_attack", true );
	}

	public override bool CanReload()
	{
		if ( Game.IsClient ) return false;

		if ( !Owner.IsValid() )
			return false;

		if ( !Input.Released( InputButton.Reload ) )
			return false;

		if ( TimeSinceReloadPressed > 0.2f )
			return false;

		return true;
	}

	public override void ActiveStart( Entity owner )
	{
		base.ActiveStart( owner );
		PlaySound( $"weapon.pickup{Game.Random.Int( 1, 4 )}" );
		TimeSinceDeployed = 0f;
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		ReloadSound.Stop();
		TimeSinceReload = 0f;
		IsReloading = false;
	}

	public override void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.AimBodyWeight = 1f;
		anim.HoldType = HoldType;
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
		Game.AssertServer();

		if ( IsMelee || IsReloading )
			return;

		if ( AmmoClip >= ClipSize )
			return;

		UpdateAmmoItem();

		if ( !WeaponItem.IsValid() )
			return;

		if ( !WeaponItem.AmmoDefinition.IsValid() )
			return;

		if ( Owner is ForsakenPlayer player )
		{
			if ( !UnlimitedAmmo )
			{
				if ( player.GetAmmoCount( WeaponItem.AmmoDefinition.UniqueId ) <= 0 )
					return;
			}
		}

		TimeSinceReload = 0f;
		IsReloading = true;

		using ( Prediction.Off() )
		{
			if ( !string.IsNullOrEmpty( ReloadSoundName ) )
				ReloadSound = PlaySound( ReloadSoundName );
		}
	}

	public override void Simulate( IClient owner )
	{
		if ( Game.IsServer && Input.Pressed( InputButton.Reload ) )
		{
			TimeSinceReloadPressed = 0f;
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			TimeSincePrimaryHeld = 0f;
		}

		var pawn = owner.Pawn as ForsakenPlayer;

		if ( pawn.LifeState == LifeState.Alive )
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

		if ( IsReloading )
		{
			if ( Prediction.FirstTime && !WasReloading )
			{
				AnimationOwner?.SetAnimParameter( "b_reload", true );
			}
		}
		else
		{
			base.Simulate( owner );
		}

		WasReloading = IsReloading;

		if ( Game.IsServer && IsReloading && TimeSinceReload > ReloadTime )
		{
			using ( Prediction.Off() )
			{
				OnReloadFinish();
			}
		}

		if ( WeaponItem.IsValid() )
		{
			var attachments = WeaponItem.Attachments.FindItems<AttachmentItem>();

			foreach ( var attachment in attachments )
			{
				attachment.Simulate( owner );
			}
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

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		Game.SetRandomSeed( Time.Tick );

		ShootEffects();
		ShootBullet( 0.05f, 1.5f, WeaponItem.Damage, 3.0f );
	}

	public virtual void MeleeStrike( float damage, float force )
	{
		foreach ( var trace in TraceBullet( Owner.AimRay.Position, Owner.AimRay.Project( MeleeRange ), 16f ) )
		{
			if ( !trace.Entity.IsValid() || trace.Entity.IsWorld )
			{
				OnMeleeAttackMissed( trace );
				continue;
			}

			if ( Game.IsServer )
			{
				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithPosition( trace.EndPosition )
						.WithTag( "blunt" )
						.WithForce( Owner.AimRay.Forward * 100f * force )
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
		var forward = Owner.AimRay.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		foreach ( var trace in TraceBullet( Owner.AimRay.Position, Owner.AimRay.Position + forward * BulletRange, bulletSize ) )
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

			if ( !Game.IsServer )
				continue;

			if ( trace.Entity.IsValid() )
			{
				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithPosition( trace.EndPosition )
						.WithTag( DamageType )
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
		Game.AssertClient();
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
		if ( Game.IsClient && Prediction.FirstTime )
		{
			var time = TimeSincePrimaryHeld.Relative.Remap( 0f, 3f, 0f, 1f ) % 1f;
			var recoil = WeaponItem.RecoilCurve.Evaluate( time );
			RecoilQueue.Enqueue( recoil );
		}
	}

	protected virtual void OnReloadFinish()
	{
		Game.AssertServer();

		IsReloading = false;

		if ( Owner is not ForsakenPlayer player )
			return;

		if ( !WeaponItem.IsValid() )
			return;

		if ( !WeaponItem.AmmoDefinition.IsValid() )
			return;

		if ( !UnlimitedAmmo )
		{
			var ammo = player.TakeAmmo( WeaponItem.AmmoDefinition.UniqueId, (ushort)(ClipSize - AmmoClip) );
			if ( ammo == 0 ) return;
			AmmoClip += ammo;
		}
		else
		{
			AmmoClip = ClipSize;
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
		if ( !IsMelee )
		{
			CreateMuzzleFlash();
		}

		ViewModelEntity?.SetAnimParameter( "fire", true );
	}

	protected virtual void OnWeaponItemChanged()
	{
		if ( Game.IsServer && WeaponItem.IsValid() && !string.IsNullOrEmpty( WeaponItem.WorldModel ) )
		{
			SetModel( WeaponItem.WorldModel );
			SetMaterialGroup( WeaponItem.WorldModelMaterialGroup );
		}
	}

	protected virtual ModelEntity GetEffectEntity()
	{
		return EffectEntity;
	}

	[ClientRpc]
	protected void ResetReloading()
	{
		WasReloading = false;
	}

	protected void CreateLightSource( Vector3 position, Color color, float range, float brightness, float lifeTime )
	{
		var light = new PointLightEntity();
		light.Brightness = brightness;
		light.Position = position;
		light.Range = range;
		light.Color = color;
		light.DeleteAsync( lifeTime );
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
			.WithTag( DamageType );

		damageInfo.Damage = damage;

		target.TakeDamage( damageInfo );
	}

	protected void UpdateAmmoItem()
	{
		if ( Owner is not ForsakenPlayer player )
			return;

		if ( !WeaponItem.IsValid() )
			return;

		if ( WeaponItem.AmmoDefinition.IsValid() )
			return;

		var availableId = player.FindItems<AmmoItem>()
			.Where( i => i.AmmoType == WeaponItem.AmmoType )
			.Select( i=> i.UniqueId )
			.Distinct()
			.FirstOrDefault();

		if ( !string.IsNullOrEmpty( availableId ) )
		{
			var definition = InventorySystem.GetDefinition( availableId );

			if ( definition is AmmoItem ammo )
			{
				SetAmmoDefinition( ammo );
			}
		}
	}
}
