using System;
using System.IO;
using System.Linq;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class CookingProcessor : BaseNetworkable
{
	[Net] private NetInventoryContainer InternalFuelInventory { get; set; }
	public InventoryContainer Fuel => InternalFuelInventory.Value;

	[Net] private NetInventoryContainer InternalInputInventory { get; set; }
	public InventoryContainer Input => InternalInputInventory.Value;

	[Net] private NetInventoryContainer InternalOutputInventory { get; set; }
	public InventoryContainer Output => InternalOutputInventory.Value;

	[Net] public TimeUntil NextProcess { get; private set; }
	[Net] public float Interval { get; set; } = 2f;
	[Net, Change( nameof( OnIsActiveChanged ) )] public bool IsActive { get; private set; }
	[Net] public bool IsEmpty { get; private set; }

	public event Action OnStarted;
	public event Action OnStopped;

	private ICookerEntity Cooker { get; set; }

	public CookingProcessor()
	{
		if ( Game.IsServer )
		{
			var fuel = new InventoryContainer();
			fuel.SetSlotLimit( 2 );
			InventorySystem.Register( fuel );

			InternalFuelInventory = new( fuel );

			var input = new InventoryContainer();
			input.SetSlotLimit( 2 );
			InventorySystem.Register( input );

			InternalInputInventory = new( input );

			var output = new InventoryContainer();
			output.SetSlotLimit( 4 );
			InventorySystem.Register( output );

			InternalOutputInventory = new( output );
		}
	}

	public void Serialize( BinaryWriter writer )
	{
		writer.Write( Fuel );
		writer.Write( Input );
		writer.Write( Output );
	}

	public void Deserialize( BinaryReader reader )
	{
		var fuel = reader.ReadInventoryContainer();
		InternalFuelInventory = new( fuel );

		var input = reader.ReadInventoryContainer();
		InternalInputInventory = new( input );

		var output = reader.ReadInventoryContainer();
		InternalOutputInventory = new( output );
	}

	public void SetCooker( ICookerEntity cooker )
	{
		Cooker = cooker;
	}

	public void Start()
	{
		Game.AssertServer();

		if ( IsActive || Fuel.IsEmpty )
			return;

		IsActive = true;
		OnStarted?.Invoke();
	}

	public void Stop()
	{
		Game.AssertServer();

		if ( !IsActive ) return;

		IsActive = false;
		OnStopped?.Invoke();
	}

	public void Process()
	{
		Game.AssertServer();

		IsEmpty = Fuel.IsEmpty && Input.IsEmpty && Output.IsEmpty;

		if ( !IsActive || !NextProcess ) return;

		NextProcess = Interval;

		if ( Fuel.IsEmpty )
		{
			Stop();
			return;
		}

		var fuel = Fuel.FindItems<InventoryItem>().FirstOrDefault();
		if ( !fuel.IsValid() ) return;

		fuel.StackSize--;

		var input = Input.FindItems<InventoryItem>().FirstOrDefault();
		if ( !input.IsValid() ) return;

		var cookable = (input as ICookableItem);
		if ( cookable is null ) return;

		input.StackSize--;

		var cookedItem = InventorySystem.CreateItem( cookable.CookedItemId );
		if ( !cookedItem.IsValid() ) return;

		cookedItem.StackSize = (ushort)cookable.CookedQuantity;

		var unstacked = Output.Stack( cookedItem );

		if ( unstacked > 0 && Cooker.IsValid() )
		{
			var entity = new ItemEntity();
			entity.SetItem( cookedItem );
			entity.Position = Cooker.Position + Vector3.Up * 40f + Cooker.Rotation.Forward * 30f;
			entity.ApplyAbsoluteImpulse( (Cooker.Rotation.Forward + Vector3.Random) * 40f );

			Stop();
		}
	}

	public string GetContainerIdString()
	{
		return $"{Fuel.InventoryId},{Input.InventoryId},{Output.InventoryId}";
	}

	private void OnIsActiveChanged()
	{
		if ( IsActive )
			OnStarted?.Invoke();
		else
			OnStopped?.Invoke();
	}
}
