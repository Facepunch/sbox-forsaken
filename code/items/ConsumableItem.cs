using Sandbox;

namespace Facepunch.Forsaken;

public class ConsumableItem : InventoryItem, IConsumableItem, ILootTableItem
{
	public override ushort MaxStackSize => 4;
	public override Color Color => ItemColors.Consumable;
	public virtual string ConsumeSound => string.Empty;
	public virtual string ConsumeEffect => null;
	public virtual string ActivateSound => string.Empty;
	public virtual float ActivateDelay => 0.5f;
	public virtual float SpawnChance => default;
	public virtual int AmountToSpawn => default;
	public virtual bool IsLootable => default;

	public async void Consume( ForsakenPlayer player )
	{
		StackSize--;

		if ( StackSize <= 0 )
			Remove();

		using ( Prediction.Off() )
		{
			if ( !string.IsNullOrEmpty( ConsumeSound ) )
			{
				player.PlaySound( ConsumeSound );
			}
		}

		await GameTask.DelaySeconds( ActivateDelay );

		if ( !player.IsValid() )
			return;

		if ( !string.IsNullOrEmpty( ActivateSound ) )
		{
			player.PlaySound( ActivateSound );
		}

		if ( !string.IsNullOrEmpty( ConsumeEffect ) )
		{
			var effect = Particles.Create( ConsumeEffect, player );
			effect.AutoDestroy( 3f );
			effect.SetEntity( 0, player );
		}

		if ( player.LifeState == LifeState.Alive )
		{
			OnActivated( player );
		}
	}

	public virtual void OnActivated( ForsakenPlayer player )
	{

	}

	public override bool CanStackWith( InventoryItem other )
	{
		return true;
	}
}
