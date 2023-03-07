using Facepunch.Forsaken.FlowFields;
using Sandbox;
using Sandbox.Component;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Structure : ModelEntity, IPersistence, IDamageable, IContextActionProvider
{
	[ConVar.Replicated( "fsk.privilege.range" )]
	public static float PrivilegeRange { get; set; } = 512f;

	public static Structure Ghost { get; private set; }

	public static Dictionary<string,int> GetCostsFor( TypeDescription type )
	{
		var attributes = type.GetAttributes<ItemCostAttribute>();
		return attributes.ToDictionary( k => k.UniqueId, v => v.Quantity );
	}

	public static Structure GetOrCreateGhost( TypeDescription type )
	{
		if ( !Ghost.IsValid() || type.TargetType != Ghost.GetType() )
		{
			ClearGhost();

			Ghost = type.Create<Structure>();
			Ghost.EnableShadowCasting = false;
			Ghost.EnableShadowReceive = false;
			Ghost.EnableAllCollisions = false;
			Ghost.SetMaterialOverride( Material.Load( "materials/blueprint.vmat" ) );

			var glow = Ghost.Components.GetOrCreate<Glow>();
			glow.Color = Color.White.WithAlpha( 0.8f );
			glow.InsideObscuredColor = Color.White.WithAlpha( 0.6f );
			glow.Width = 0.2f;
		}

		return Ghost;
	}

	public static bool CanAfford( ForsakenPlayer player, TypeDescription type )
	{
		var costs = GetCostsFor( type );

		foreach ( var kv in costs )
		{
			if ( !player.HasItems( kv.Key, kv.Value ) )
				return false;
		}

		return true;
	}

	public static void ClearGhost()
	{
		Ghost?.Delete();
		Ghost = null;
	}

	[Net] public IList<Socket> Sockets { get; set; } = new List<Socket>();

	public virtual string PlaceSoundName => "building.place";
	public virtual bool RequiresSocket => true;
	public virtual bool ShouldRotate => true;
	public virtual float MaxHealth => 100f;

	public virtual float InteractionRange => 100f;
	public virtual bool AlwaysGlow => false;
	public virtual Color GlowColor => Color.White;

	public bool IsCollidingWithWorld()
	{
		var testPosition = Position + Vector3.Up * 4f;
		var collision = Trace.Body( PhysicsBody, Transform.WithPosition( testPosition ), testPosition )
			.WithAnyTags( "world" )
			.Run();

		if ( collision.Hit || collision.StartedSolid )
			return true;

		var zones = All.OfType<BuildExclusionZone>()
			.Where( z => PhysicsBody.CheckOverlap( z.PhysicsBody ) );

		return zones.Any();
	}

	public void SnapToSocket( Socket.Match match )
	{
		var transform = match.Theirs.Transform;

		Rotation = Rotation.Identity;

		var relative = Position - match.Ours.Position;
		var rotation = transform.Rotation;
		var theirStructure = match.Theirs.Parent as Structure;

		if ( theirStructure.IsValid() && !theirStructure.ShouldRotate )
		{
			transform.Rotation = Rotation.Identity;
		}

		Position = transform.TransformVector( relative );

		if ( ShouldRotate )
			Rotation = rotation;

		PhysicsBody.Transform = Transform;
		ResetInterpolation();
	}

	public virtual bool ShouldSaveState()
	{
		return true;
	}

	public virtual void BeforeStateLoaded()
	{
		foreach ( var socket in Sockets )
		{
			socket.RestoreConnection();
		}
	}

	public virtual void AfterStateLoaded()
	{

	}

	public virtual void SerializeState( BinaryWriter writer )
	{
		writer.Write( Transform );
		writer.Write( Sockets.Count );

		foreach ( var socket in Sockets )
		{
			socket.Serialize( writer );
		}

		writer.Write( Health );
	}

	public virtual void DeserializeState( BinaryReader reader )
	{
		Transform = reader.ReadTransform();

		var count = reader.ReadInt32();

		for ( var i = 0; i < count; i++ )
		{
			var socket = Sockets.ElementAt( i );

			if ( socket.IsValid() )
			{
				socket.Deserialize( reader );
			}
		}

		Health = reader.ReadSingle();
	}

	public virtual string GetContextName()
	{
		return $"Structure ({Health.CeilToInt()}HP)";
	}

	public virtual IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public virtual ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return default;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{

	}

	public virtual void OnPlacedByPlayer( ForsakenPlayer player )
	{
		PathManager.UpdateCollisions( Position );
	}

	public virtual void OnConnected( Socket ours, Socket theirs )
	{

	}

	public virtual bool CanConnectTo( Socket socket )
	{
		return true;
	}

	public virtual bool IsValidPlacement( Vector3 target, Vector3 normal )
	{
		return normal.Dot( Vector3.Up ).AlmostEqual( 1f );
	}

	public virtual Socket.Match LocateSocket( Vector3 target )
	{
		var ourSockets = Sockets
			.OrderBy( s => s.Position.Distance( target ) );

		var nearbyStructures = FindInSphere( target, 48f )
			.OfType<Structure>()
			.Where( s => !s.Equals( this ) )
			.OrderBy( s => s.Position.Distance( target ) );

		foreach ( var theirStructure in nearbyStructures )
		{
			var theirSockets = theirStructure.Sockets
				.Where( s => !s.Connection.IsValid() && CanConnectTo( s ) )
				.OrderBy( a => a.Position.Distance( target ) );

			foreach ( var theirSocket in theirSockets )
			{
				foreach ( var ourSocket in ourSockets )
				{
					if ( ourSocket.CanConnectTo( theirSocket ) )
					{
						return new Socket.Match( ourSocket, theirSocket );
					}
				}
			}
		}

		return default;
	}

	public override void OnKilled()
	{

	}

	public override void TakeDamage( DamageInfo info )
	{

	}

	public override void Spawn()
	{
		Health = MaxHealth;

		Tags.Add( "hover" );

		base.Spawn();
	}

	protected Socket AddSocket( string attachmentName )
	{
		var attachment = GetAttachment( attachmentName );

		if ( attachment.HasValue )
		{
			var socket = new Socket
			{
				Transform = attachment.Value
			};

			socket.SetParent( this );

			AddSocket( socket );

			return socket;
		}

		return null;
	}

	protected Socket AddSocket( Socket socket )
	{
		socket.SetParent( this );
		Sockets.Add( socket );
		return socket;
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			PathManager.UpdateCollisions( Position );
		}

		base.OnDestroy();
	}
}
