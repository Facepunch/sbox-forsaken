using System;
using System.IO;
using System.Linq;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class RecyclingProcessor : BaseNetworkable
{
	[Net] private NetInventoryContainer InternalInputInventory { get; set; }
	public RecyclerInputContainer Input => InternalInputInventory.Value as RecyclerInputContainer;

	[Net] private NetInventoryContainer InternalOutputInventory { get; set; }
	public InventoryContainer Output => InternalOutputInventory.Value;

	[Net] public TimeUntil NextProcess { get; private set; }
	[Net] public float Interval { get; set; } = 2f;
	[Net, Change( nameof( OnIsActiveChanged ) )] public bool IsActive { get; private set; }
	[Net] public bool IsEmpty { get; private set; }

	public event Action OnStarted;
	public event Action OnStopped;

	private Recycler Recycler { get; set; }

	public RecyclingProcessor()
	{
		if ( Game.IsServer )
		{
			var input = new RecyclerInputContainer();
			InventorySystem.Register( input );

			InternalInputInventory = new( input );

			var output = new InventoryContainer();
			output.IsTakeOnly = true;
			output.SetSlotLimit( 6 );
			InventorySystem.Register( output );

			InternalOutputInventory = new( output );
		}
	}

	public void SerializeState( BinaryWriter writer )
	{
		writer.Write( Input );
		writer.Write( Output );
	}

	public void DeserializeState( BinaryReader reader )
	{
		var input = reader.ReadInventoryContainer( Input );
		InternalInputInventory = new( input );

		var output = reader.ReadInventoryContainer( Output );
		InternalOutputInventory = new( output );
	}

	public void SetRecycler( Recycler recycler )
	{
		Recycler = recycler;
	}

	public void Start()
	{
		Game.AssertServer();

		if ( IsActive || Input.IsEmpty )
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

		IsEmpty = Input.IsEmpty && Output.IsEmpty;

		if ( !IsActive || !NextProcess ) return;

		NextProcess = Interval;

		if ( Input.IsEmpty )
		{
			Stop();
			return;
		}

		var input = Input.FindItems<InventoryItem>().FirstOrDefault();
		if ( !input.IsValid() ) return;

		var recyclable = (input as IRecyclableItem);
		if ( recyclable is null ) return;

		input.StackSize--;

		if ( recyclable.RecycleOutput is not null )
		{
			foreach ( var kv in recyclable.RecycleOutput )
			{
				GiveItemOrStop( kv.Key, kv.Value );
			}
		}

		if ( recyclable.BaseComponentReturn > 0f )
		{
			var recipe = ResourceLibrary.GetAll<RecipeResource>()
				.FirstOrDefault( r => r.Output.ToLower() == input.UniqueId.ToLower() );

			if ( recipe == null ) return;

			foreach ( var kv in recipe.Inputs )
			{
				var amount = kv.Value * recyclable.BaseComponentReturn;

				if ( amount > 0 )
				{
					GiveItemOrStop( kv.Key, kv.Value );
				}
			}
		}
	}

	public string GetContainerIdString()
	{
		return $"{Input.ContainerId},{Output.ContainerId}";
	}

	private bool GiveItemOrStop( string uniqueId, int stackSize )
	{
		var outputItem = InventorySystem.CreateItem( uniqueId );
		if ( !outputItem.IsValid() ) return false;

		outputItem.StackSize = (ushort)stackSize;

		var unstacked = Output.Stack( outputItem );

		if ( unstacked > 0 && Recycler.IsValid() )
		{
			var entity = new ItemEntity();
			entity.SetItem( outputItem );
			entity.Position = Recycler.Position + Vector3.Up * 40f + Recycler.Rotation.Forward * 30f;
			entity.ApplyAbsoluteImpulse( (Recycler.Rotation.Forward + Vector3.Random) * 40f );

			Stop();

			return true;
		}

		return false;
	}

	private void OnIsActiveChanged()
	{
		if ( IsActive )
			OnStarted?.Invoke();
		else
			OnStopped?.Invoke();
	}
}
