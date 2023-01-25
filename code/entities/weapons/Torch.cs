using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_torch" )]
public partial class Torch : MeleeWeapon
{
	public override string PrimaryUseHint => IsIgnited ? "Extinguish" : "Ignite";
	public override string DamageType => "blunt";
	public override float MeleeRange => 80f;
	public override float PrimaryRate => 1.5f;
	public override float Force => 1f;

	[Net] private bool IsIgnited { get; set; }
	private PointLightEntity Light { get; set; }

	public override void AttackPrimary()
	{
		if ( Game.IsServer )
		{
			IsIgnited = !IsIgnited;

			if ( IsIgnited )
				CreateLight();
			else
				DestroyLight();
		}
	}

	public override void ActiveStart( Entity owner )
	{
		if ( Game.IsServer && IsIgnited )
		{
			CreateLight();
		}

		base.ActiveStart( owner );
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		DestroyLight();

		base.ActiveEnd( ent, dropped );
	}

	protected override void OnDestroy()
	{
		DestroyLight();

		base.OnDestroy();
	}

	private void DestroyLight()
	{
		Light?.Delete();
		Light = null;
	}

	private void CreateLight()
	{
		Light?.Delete();

		Light = new();
		Light.Position = Position;
		Light.SetParent( this );
		Light.Range = 600f;
		Light.Color = Color.Orange.Lighten( 0.5f );
		Light.Brightness = 0.1f;
	}
}
