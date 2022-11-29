using System;
using Sandbox;
using Sandbox.UI;
using Facepunch.Forsaken;
using System.Collections.Generic;

namespace Facepunch.Forsaken.UI;

public partial class InventorySlot
{
    private static Queue<InventorySlot> TransferQueue { get; set; } = new();
    private static TimeUntil NextTransferTime { get; set; }

    [Event.Tick.Client]
    private static void ProcessTransferQueue()
    {
        if ( NextTransferTime && TransferQueue.TryDequeue( out var slot ) )
        {
            if ( slot.TryTransfer() )
            {
                Sound.FromScreen( "inventory.move" );
            }

            slot.RemoveClass( "pending-transfer" );
            NextTransferTime = 0.25f;
        }
    }

    public override void Tick()
    {
        if ( Item.IsValid() && HasHovered && Input.Down( InputButton.Duck ) )
        {
            if ( !TransferQueue.Contains( this ) )
            {
                TransferQueue.Enqueue( this );
                AddClass( "pending-transfer" );
            }
        }

        base.Tick();
    }

    protected bool TryTransfer()
    {
        if ( !Item.IsValid() )
            return false;

        var container = Item.Parent;
        var target = container.GetTransferTarget( Item );

        if ( !target.IsValid() )
            return false;

        if ( target.IsTakeOnly )
            return false;

        InventorySystem.SendTransferEvent( container, target, Item.SlotId );
        return true;
    }

    protected override void OnRightClick( MousePanelEvent e )
    {
        if ( Item.IsValid() && !TransferQueue.Contains( this ) )
        {
            TransferQueue.Enqueue( this );
        }

        base.OnRightClick( e );
    }
}
