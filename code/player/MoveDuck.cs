
namespace Sandbox
{
	[Library]
	public class MoveDuck : BaseNetworkable
	{
		public BasePlayerController Controller;
		public bool IsActive { get; set; }

		private Vector3 OriginalMins { get; set; }
		private Vector3 OriginalMaxs { get; set; }

		public MoveDuck( BasePlayerController controller )
		{
			Controller = controller;
		}

		public virtual void PreTick() 
		{
			var doesWantToDuck = Input.Down( InputButton.Duck );

			if ( doesWantToDuck != IsActive ) 
			{
				if ( doesWantToDuck )
					TryDuck();
				else
					TryUnDuck();
			}

			if ( IsActive )
			{
				Controller.SetTag( "ducked" );
				Controller.EyeLocalPosition *= 0.8f;
			}
		}

		protected virtual void TryDuck()
		{
			IsActive = true;
		}

		protected virtual void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Controller.Position, Controller.Position, OriginalMins, OriginalMaxs );
			if ( pm.StartedSolid ) return;

			IsActive = false;
		}

		public virtual void UpdateBBox( ref Vector3 mins, ref Vector3 maxs, float scale )
		{
			OriginalMins = mins;
			OriginalMaxs = maxs;

			if ( IsActive )
				maxs = maxs.WithZ( 36 * scale );
		}
		public virtual float GetWishSpeed()
		{
			if ( !IsActive ) return -1;
			return 97.0f;
		}
	}
}
