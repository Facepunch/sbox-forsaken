using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	[Net] public IList<CraftingQueueEntry> CraftingQueue { get; set; }

	[ConCmd.Server( "fsk.crafting.add" )]
	public static void AddCraftingCmd( string recipeName, int quantity )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		var recipe = ResourceLibrary.GetAll<RecipeResource>()
			.Where( r => r.ResourceName == recipeName )
			.FirstOrDefault();

		player.AddToCraftingQueue( recipe, quantity );
	}

	[ConCmd.Server( "fsk.crafting.cancel" )]
	public static void CancelCraftingCmd( int index )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		if ( player.CraftingQueue.Count >= index )
		{
			var entry = player.CraftingQueue[index];
			player.CancelCrafting( entry );
		}
	}

	/// <summary>
	/// Clear the current crafting queue and return inputs back to inventory. If there is no inventory space
	/// drop the inputs onto the ground.
	/// </summary>
	public void ClearCraftingQueue()
	{
		Game.AssertServer();

		for ( var i = CraftingQueue.Count - 1; i >= 0; i-- )
		{
			CancelCrafting( CraftingQueue[i] );
		}
	}

	/// <summary>
	/// Whether or not this player has the required input items to craft a recipe.
	/// </summary>
	public bool CanCraftRecipe( RecipeResource recipe, int quantity = 1 )
	{
		foreach ( var kv in recipe.Inputs )
		{
			if ( !HasItems( kv.Key, kv.Value * quantity ) )
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Cancel the given crafting queue entry and try to return the input items back to this player's inventory.
	/// </summary>
	/// <param name="entry"></param>
	/// <returns></returns>
	public bool CancelCrafting( CraftingQueueEntry entry )
	{
		Game.AssertServer();

		var index = CraftingQueue.IndexOf( entry );
		if ( index < 0 ) return false;

		CraftingQueue.RemoveAt( index );

		var recipe = entry.Recipe;

		foreach ( var kv in recipe.Inputs )
		{
			var definition = InventorySystem.GetDefinition( kv.Key );
			var totalToGive = kv.Value * entry.Quantity;
			var stacksToGive = totalToGive / definition.MaxStackSize;
			var remainder = totalToGive % definition.MaxStackSize;

			for ( var i = 0; i < stacksToGive; i++ )
			{
				var item = InventorySystem.CreateItem( kv.Key );
				item.StackSize = item.MaxStackSize;
				TryGiveItem( item );
			}

			if ( remainder > 0 )
			{
				var item = InventorySystem.CreateItem( kv.Key );
				item.StackSize = (ushort)remainder;
				TryGiveItem( item );
			}
		}

		if ( index == 0 )
		{
			StartNextCraftingEntry();
		}

		return true;
	}

	/// <summary>
	/// Add a recipe to the crafting queue. This will automatically deduct the required input items from this
	/// player's inventory.
	/// </summary>
	public bool AddToCraftingQueue( RecipeResource recipe, int quantity = 1 )
	{
		Game.AssertServer();

		if ( !CanCraftRecipe( recipe, quantity ) )
			return false;

		foreach ( var kv in recipe.Inputs )
		{
			TakeItems( kv.Key, kv.Value * quantity );
		}

		var item = new CraftingQueueEntry
		{
			ResourceId = recipe.ResourceId,
			Quantity = quantity
		};

		if ( CraftingQueue.Count == 0 )
		{
			item.FinishTime = recipe.CraftingTime;
		}

		CraftingQueue.Add( item );

		return true;
	}

	private void StartNextCraftingEntry()
	{
		if ( CraftingQueue.Count > 0 )
		{
			var entry = CraftingQueue.First();
			entry.FinishTime = entry.Recipe.CraftingTime;
		}
	}

	private void SimulateCrafting()
	{
		if ( Game.IsServer && CraftingQueue.Count > 0 )
		{
			var entry = CraftingQueue.First();

			if ( entry.FinishTime )
			{
				var recipe = entry.Recipe;
				var item = InventorySystem.CreateItem( recipe.Output );
				item.StackSize = (ushort)recipe.StackSize;

				TryGiveItem( item );

				entry.FinishTime = recipe.CraftingTime;
				entry.Quantity--;

				if ( entry.Quantity <= 0 )
				{
					CraftingQueue.RemoveAt( 0 );
				}

				StartNextCraftingEntry();
			}
		}
	}
}
