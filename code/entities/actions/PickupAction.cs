﻿namespace Facepunch.Forsaken;

public class PickupAction : ContextAction
{
	public override string UniqueId => "pickup";
	public override string Name => "Pickup";
	public override string Icon => "textures/ui/armor_slot_head.png";

	public PickupAction()
	{

	}

	public PickupAction( IContextActionProvider owner )
	{
		Provider = owner;
	}

	public override void Select( ForsakenPlayer player )
	{
		Log.Info( "You picked it up" );
	}
}