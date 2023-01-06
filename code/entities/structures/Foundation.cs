using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Foundation" )]
[Description( "The most fundamental building block. Walls, doors and windows can be attached to it." )]
[Icon( "textures/ui/foundation.png" )]
[ItemCost( "wood", 100 )]
[ItemCost( "stone", 50 )]
public partial class Foundation : Structure
{
	public override bool RequiresSocket => false;
	public override bool ShouldRotate => false;

	private Stockpile CachedStockpile { get; set; }

	public void AddFoundationsToSet( HashSet<Foundation> foundations )
	{
		if ( !foundations.Contains( this ) )
		{
			foundations.Add( this );

			foreach ( var socket in Sockets )
			{
				if ( socket.Connection.IsValid() )
				{
					var connected = socket.Connection.Parent as Foundation;

					if ( connected.IsValid() )
					{
						connected.AddFoundationsToSet( foundations );
					}
				}
			}
		}
	}

	public Stockpile FindStockpile()
	{
		if ( CachedStockpile.IsValid() )
		{
			return CachedStockpile;
		}

		var foundations = new HashSet<Foundation>();
		AddFoundationsToSet( foundations );

		foreach ( var socket in Sockets )
		{
			if ( socket.Connection.IsValid() )
			{
				var connected = socket.Connection.Parent as Foundation;

				if ( connected.IsValid() )
				{
					connected.AddFoundationsToSet( foundations );
				}
			}
		}

		foreach ( var foundation in foundations )
		{
			var found = FindInSphere( foundation.Position, 128f ).OfType<Stockpile>().FirstOrDefault();

			if ( found.IsValid() )
			{
				CachedStockpile = found;
				return found;
			}
		}

		CachedStockpile = null;
		return null;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/foundation.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "solid", "foundation" );
	}

	public override bool IsValidPlacement( Vector3 target, Vector3 normal )
	{
		var nerarbyFoundations = FindInSphere( target, Structure.AuthorizationRange )
			.OfType<Foundation>()
			.Where( s => !s.Equals( this ) );

		if ( nerarbyFoundations.Any() )
			return false;

		return base.IsValidPlacement( target, normal );
	}

	public override void OnNewModel( Model model )
	{
		if ( Game.IsServer || IsClientOnly )
		{
			AddFoundationSocket( "forward", "backward" );
			AddFoundationSocket( "backward", "forward" );
			AddFoundationSocket( "left", "right" );
			AddFoundationSocket( "right", "left" );

			AddWallSocket( "forward" );
			AddWallSocket( "backward" );
			AddWallSocket( "left" );
			AddWallSocket( "right" );
		}

		base.OnNewModel( model );
	}

	private void AddFoundationSocket( string direction, string connectorDirection )
	{
		var socket = AddSocket( direction );
		socket.ConnectAny.Add( "foundation" );
		socket.ConnectAll.Add( connectorDirection );
		socket.Tags.Add( "foundation", direction );
	}

	private void AddWallSocket( string attachmentName )
	{
		var socket = AddSocket( attachmentName );
		socket.Tags.Add( "foundation", "wall" );
	}
}
