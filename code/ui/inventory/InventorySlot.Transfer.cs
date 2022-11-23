using System;
using Sandbox;
using Sandbox.UI;
using Facepunch.Forsaken;

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

        var container = Item.Container;
        if ( container.TransferHandler == null )
            return false;

        var transferContainer = container.TransferHandler.Invoke( Item );

        if ( transferContainer.IsValid() )
        {
            InventorySystem.SendTransferEvent( container, transferContainer, Item.SlotId );
            return true;
        }

        return false;
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
