using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Deer : AnimalNPC, ILimitedSpawner, IDamageable, IContextActionProvider
{
	public TimeSince LastDamageTime { get; set; }

	public float InteractionRange => 100f;
	public Color GlowColor => Color.White;
	public bool AlwaysGlow => true;
	public float MaxHealth => 80f;

	private ContextAction HarvestAction { get; set; }
	private TimeUntil NextSwapTrotting { get; set; }
	private float CurrentSpeed { get; set; }
	private bool IsTrotting { get; set; }

	private float WalkSpeed => 80f;
	private float TrotSpeed => 200f;
	private float RunSpeed => 300f;

	public Deer()
	{
		HarvestAction = new( "harvest", "Harvest", "textures/ui/actions/harvest.png" );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return HarvestAction;
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

	public override bool ShouldWander()
	{
		return true;
	}

	public override float GetMoveSpeed()
	{
		if ( LastDamageTime < 10f )
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

		if ( TryGetNavMeshPosition( 2000f, 4000f, out var targetPosition ) )
		{
			MoveToLocation( targetPosition );
			NextWanderTime = 10f;
		}

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

	protected override void HandleAnimation()
	{
		if ( NextSwapTrotting )
		{
			NextSwapTrotting = Game.Random.Float( 8f, 16f );
			IsTrotting = !IsTrotting;
		}

		if ( LifeState == LifeState.Dead )
		{
			SetAnimParameter( "dead", true );
			SetAnimParameter( "speed", 0f );
		}
		else
		{
			var velocity = Velocity.Length;
			var targetSpeed = 0f;

			if ( velocity >= RunSpeed * 0.9f )
				targetSpeed = 1f;
			else if ( velocity > TrotSpeed * 0.9f )
				targetSpeed = 0.5f;
			else if ( velocity > 1f )
				targetSpeed = 0.01f;

			CurrentSpeed = CurrentSpeed.LerpTo( targetSpeed, Time.Delta * 8f );

			SetAnimParameter( "dead", false );
			SetAnimParameter( "speed", CurrentSpeed );
		}
	}
}
