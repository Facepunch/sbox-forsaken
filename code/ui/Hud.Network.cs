using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Hud
{
	private static string LastZoneName { get; set; }
	private static TimeSince LastZoneShown { get; set; }

	[ClientRpc]
	[ConCmd.Client]
    public static void ShowZoneName( string name )
    {
		if ( LastZoneName == name && LastZoneShown < 10f )
			return;

		Current?.ShowZoneName( name, 4f );
	}

	[ClientRpc]
	public static void ShowDamage( Vector3 position, float damage )
	{
		FloatingText.CreateNew()
			.WithPosition( position + Vector3.Up * 60f + Vector3.Random.WithZ( 0f ) * 20f )
			.WithText( damage.CeilToInt().ToString() )
			.WithMotion( Vector2.Random, Game.Random.Float( 60f, 80f ), 0.6f, 2 )
			.WithScale( 0.75f, 1.5f, 0.5f )
			.WithLifespan( 1f + Game.Random.NextSingle() )
			.WithClass( "floating-damage" )
			.WithFadeIn( 0.15f )
			.WithFadeOut( 0.25f );
	}
}
