using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Deer : Animal, ILimitedSpawner, IDamageable, IContextActionProvider
{
	private enum DeerPose
	{
		Default,
		Sitting,
		Sleeping
	}

	public TimeSince? LastDamageTime { get; set; }

	public float InteractionRange => 100f;
	public Color GlowColor => Color.White;
	public bool AlwaysGlow => true;
	public float MaxHealth => 80f;

	private ContextAction HarvestAction { get; set; }

	private TimeUntil BlockMovementUntil { get; set; }
	private TimeUntil NextSwapTrotting { get; set; }
	private TimeUntil NextChangePose { get; set; }

	private ForsakenPlayer EvadePlayer { get; set; }
	private float CurrentSpeed { get; set; }
	private bool IsTrotting { get; set; }

	private DeerPose Pose { get; set; }

	private float WalkSpeed => 60f;
	private float TrotSpeed => 250f;
	private float RunSpeed => 400f;

	private EvadeBehavior Evade { get; set; }

	public Deer()
	{
		HarvestAction = new( "harvest", "Harvest", "textures/ui/actions/harvest.png" );
	}

	public bool IsInPanicMode()
	{
		return LastDamageTime.HasValue && LastDamageTime < 10f;
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return HarvestAction;
	}

	public virtual void Despawn()
	{

	}

	public virtual string GetContextName() => GetDisplayName();

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == HarvestAction )
		{
			if ( Game.IsServer )
			{
				var timedAction = new TimedActionInfo( OnHarvested );

				timedAction.SoundName = "";
				timedAction.Title = "Harvesting";
				timedAction.Origin = Position;
				timedAction.Duration = 2f;
				timedAction.Icon = "textures/ui/actions/harvest.png";

				player.StartTimedAction( timedAction );
			}
		}
	}

	public override string GetDisplayName()
	{
		return "Deer";
	}

	public override float GetMoveSpeed()
	{
		if ( IsInPanicMode() )
			return RunSpeed;

		if ( IsTrotting )
			return TrotSpeed;

		return WalkSpeed;
	}

	public override void Spawn()
	{
		SetModel( "models/deer/deer.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		EnableSolidCollisions = false;
		Health = MaxHealth;
		Scale = Game.Random.Float( 0.9f, 1.1f );

		Evade = Components.GetOrCreate<EvadeBehavior>();

		base.Spawn();
	}

	public override void OnKilled()
	{
		LifeState = LifeState.Dead;
		Tags.Add( "hover" );
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead ) return;

		if ( info.Attacker is ForsakenPlayer attacker )
		{
			if ( info.HasTag( "bullet" ) )
			{
				if ( info.Hitbox.HasTag( "head" ) )
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.headshot" );
					info.Damage *= 2f;
				}
				else
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.hit" );
				}
			}

			EvadePlayer = attacker;
		}

		if ( info.HasTag( "bullet" ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gameplay/player/taken_damage/taken_damage.vpcf", info.Position );
				particles.SetForward( 0, info.Force.Normal );

				PlaySound( "melee.hitflesh" );
			}
		}

		LastDamageTime = 0f;
		State = MovementState.Moving;

		base.TakeDamage( info );
	}

	protected virtual void OnHarvested( ForsakenPlayer player )
	{
		if ( !IsValid ) return;

		var yield = Game.Random.Int( 2, 4 );
		var item = InventorySystem.CreateItem( "raw_meat" );
		item.StackSize = (ushort)yield;

		var remaining = player.TryGiveItem( item );

		if ( remaining < yield )
		{
			Sound.FromScreen( To.Single( player ), "inventory.move" );
		}

		if ( remaining == yield ) return;

		if ( remaining > 0 )
		{
			var entity = new ItemEntity();
			entity.Position = Position;
			entity.SetItem( item );
		}

		Delete();
	}

	protected override void HandleBehavior()
	{
		if ( NextSwapTrotting )
		{
			IsTrotting = !IsTrotting;

			if ( IsTrotting )
				NextSwapTrotting = Game.Random.Float( 2f, 4f );
			else
				NextSwapTrotting = Game.Random.Float( 10f, 20f );
		}

		base.HandleBehavior();
	}

	protected override bool CanChangeState()
	{
		return !IsInPanicMode();
	}

	protected override void UpdateVelocity()
	{
		if ( !BlockMovementUntil )
		{
			Velocity = Vector3.Zero;
			return;
		}

		if ( EvadePlayer.IsValid() && IsInPanicMode() )
		{
			var acceleration = Evade.GetSteering( EvadePlayer );

			if ( !acceleration.IsNearZeroLength )
			{
				Steering.Steer( acceleration );
			}
		}

		base.UpdateVelocity();
	}

	protected override void HandleAnimation()
	{
		if ( State == MovementState.Moving && Pose > DeerPose.Default && !IsInPanicMode() )
		{
			BlockMovementUntil = 3f;
			Pose = DeerPose.Default;
		}

		if ( LifeState == LifeState.Dead )
		{
			SetAnimParameter( "dead", true );
			SetAnimParameter( "speed", 0f );
			SetAnimParameter( "pose", (int)DeerPose.Default );
		}
		else
		{
			var targetSpeed = Velocity.WithZ( 0f ).Length;

			if ( NextChangePose && targetSpeed == 0f && !HasValidPath() )
			{
				NextChangePose = Game.Random.Float( 4f, 16f );
				Pose = (DeerPose)Game.Random.Int( 0, 2 );
			}

			CurrentSpeed = CurrentSpeed.LerpTo( targetSpeed, Time.Delta * 20f );

			SetAnimParameter( "dead", false );
			SetAnimParameter( "pose", (int)Pose );
			SetAnimParameter( "speed", CurrentSpeed );
		}
	}
}
