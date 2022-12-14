using Sandbox;
using System;

namespace Facepunch.Forsaken;

public abstract partial class MeleeWeapon : Weapon
{
	public virtual float DamageStaminaThreshold => 40f;
	public virtual bool ScaleDamageWithStamina => true;
	public virtual float ScaleNonBlockDamage => 1f;
	public virtual float StaminaLossPerSwing => 4f;
	public virtual bool DoesBlockDamage => false;
	public virtual bool UseTierBodyGroups => false;
	public virtual string HitPlayerSound => "melee.hitflesh";
	public virtual string HitObjectSound => "sword.hit";
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Swing;
	public virtual string SwingSound => "melee.swing";
	public virtual float Force => 1.5f;

	public override float MeleeRange => 80f;
	public override float PrimaryRate => 2f;
	public override float SecondaryRate => 1f;
	public override int ClipSize => 0;
	public override bool IsMelee => true;

	public override void AttackPrimary()
	{
		if ( Owner is not ForsakenPlayer player )
			return;

		var damageScale = ScaleNonBlockDamage;

		if ( ScaleDamageWithStamina )
		{
			damageScale *= Math.Max( (player.Stamina / DamageStaminaThreshold ), 1f );
		}

		PlayAttackAnimation();
		ShootEffects();
		MeleeStrike( WeaponItem.Damage * damageScale, Force );
		PlaySound( SwingSound );

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		player.ReduceStamina( StaminaLossPerSwing );
	}

	public override void CreateViewModel()
	{
		Game.AssertClient();
		base.CreateViewModel();
	}

	protected override void ShootEffects()
	{
		base.ShootEffects();

		ViewModelEntity?.SetAnimParameter( "attack", true );
		ViewModelEntity?.SetAnimParameter( "holdtype_attack", 1 );
	}

	protected override void OnMeleeAttackMissed( TraceResult trace )
	{
		if ( trace.Hit )
		{
			PlaySound( HitObjectSound );
		}
	}

	protected override void OnMeleeAttackHit( Entity victim )
	{
		ViewModelEntity?.SetAnimParameter( "attack_has_hit", true );

		if ( victim is ForsakenPlayer target )
			target.PlaySound( HitPlayerSound );
		else
			victim.PlaySound( HitObjectSound );

		base.OnMeleeAttackHit( victim );
	}
}
