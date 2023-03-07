using System.Collections.Generic;

namespace Facepunch.Forsaken.FlowFields
{
	public interface IMoveCommand
	{
		public List<Vector3> GetDestinations( MoveGroup moveGroup );
		public bool IsFinished( MoveGroup moveGroup, IMoveAgent agent );
		public void Execute( MoveGroup moveGroup, IMoveAgent agent );
	}
}
