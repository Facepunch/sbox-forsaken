using System;

namespace Facepunch.Forsaken;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
public class ItemCostAttribute : Attribute
{
	public string UniqueId { get; set; }
	public int Quantity { get; set; }

	public ItemCostAttribute( string uniqueId, int quantity )
	{
		UniqueId = uniqueId;
		Quantity = quantity;
	}
}
