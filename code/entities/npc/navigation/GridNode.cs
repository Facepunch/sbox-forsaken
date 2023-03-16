namespace Facepunch.Forsaken;

public class GridNode
{
	public bool Walkable;
	public bool Opened;
	public bool Closed;
	public float GScore;
	public float FScore;
	public GridNode Parent;
	public Vector2 Position;
	public int Index;
	public GridNode[] Neighbors;
	public float ZOffset;
	public float Slope;
}
