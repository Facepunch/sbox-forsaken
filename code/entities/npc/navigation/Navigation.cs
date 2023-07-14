using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public static partial class Navigation
{
	public static bool IsReady { get; private set; }
	public static float StepSize = 8f;
	public static int CellSize => 20;
	public static Rect Bounds => WorldBounds;

	private static Vector2 GridSize;
	private static GridNode[] Grid;
	private static List<int> CalculatedPath = new();
	private static Rect WorldBounds = new();
	private static PriorityQueue<GridNode, float> OpenSet = new();
	private static Vector3 Origin;

	[GameEvent.Entity.PostSpawn]
	private static async void GenerateWalkabilityMap()
	{
		if ( Game.IsClient ) return;

		var bounds = Game.PhysicsWorld.Body.GetBounds();
		var worldW = bounds.Size.x;
		var worldH = bounds.Size.y;

		Origin = bounds.Center;
		WorldBounds = new Rect( -(worldW * 0.5f) + Origin.x, -(worldH * 0.5f) + Origin.y, worldW, worldH );
		IsReady = false;

		var gridX = (int)(WorldBounds.Width / CellSize);
		var gridY = (int)(WorldBounds.Height / CellSize);

		GridSize = new Vector2( gridX, gridY );
		Grid = new GridNode[gridX * gridY];

		await GameTask.RunInThreadAsync( () =>
		{
			for ( int x = 0; x < gridX; x++ )
			{
				for ( int y = 0; y < gridY; y++ )
				{
					var idx = GetIndex( x, y );
					var walkable = SampleWalkability( ToWorld( idx ), out float zOffset, out float slope );

					Grid[idx] = new GridNode()
					{
						Walkable = walkable,
						Position = new Vector2( x, y ),
						Index = idx,
						ZOffset = zOffset,
						Slope = slope
					};
				}
			}
		} );

		for ( int i = 0; i < Grid.Length; i++ )
		{
			var node = Grid[i];
			node.Neighbors = new GridNode[4];

			for( int j = 0; j < 4; j++ )
			{
				var neighborPosition = node.Position + DirectionLut[j];
				var idx = GetIndex( neighborPosition );

				if ( idx < 0 || idx >= Grid.Length ) 
					continue;

				node.Neighbors[j] = Grid[idx];

				var stepHeight = Grid[idx].ZOffset - node.ZOffset;
				if ( node.ZOffset < Grid[idx].ZOffset && stepHeight > StepSize )
				{
					node.Walkable = false;
				}
			}
		}

		Log.Info( "Walkability map has been generated." );
		IsReady = true;
	}

	private static bool SampleWalkability( Vector3 point, out float zOffset, out float slope )
	{
		var traceStart = point + Vector3.Up * 5000f;
		var traceEnd = point + Vector3.Down * 5000f;
		var trace = Trace.Ray( traceStart, traceEnd )
			.WorldOnly()
			.Run();

		zOffset = trace.HitPosition.z;
		slope = trace.Normal.Angle( Vector3.Up );

		if ( !trace.Hit ) return false;
		if ( trace.Normal.Angle( Vector3.Up ) > 45f ) return false;

		var capsule = new Capsule( Vector3.Zero, Vector3.Up * 48f, 12f );

		var capsulePosition = trace.EndPosition.WithZ( trace.EndPosition.z + capsule.Radius + StepSize );
		var sweep = Trace.Capsule( capsule, capsulePosition, capsulePosition )
			.WorldAndEntities()
			.WithAnyTags( "solid" )
			.WithoutTags( "trigger", "passplayers" )
			.Run();

		return !sweep.Hit && !sweep.StartedSolid;
	}

	public static Vector3 WithZOffset( Vector3 position )
	{
		var idx = FromWorld( position );

		if ( !IsOnMap( idx ) )
			return position;

		var node = Grid[idx];
		return position.WithZ( node.ZOffset );
	}

	public static async void Update( Vector3 position, float radius )
	{
		// Defer the update until the next physics frame.
		await GameTask.NextPhysicsFrame();

		var gridIndex = FromWorld( position );
		var gridPosition = GetPosition( gridIndex );
		var blocks = (int)(radius / CellSize);
		var gridX = (int)gridPosition.x;
		var gridY = (int)gridPosition.y;

		for ( int x = gridX - blocks; x < gridX + blocks; x++ )
		{
			for ( int y = gridY - blocks; y < gridY + blocks; y++ )
			{
				var idx = GetIndex( x, y );
				if ( idx == -1 ) continue;

				var worldPosition = ToWorld( idx );
				var walkable = SampleWalkability( worldPosition, out float zOffset, out float slope );
				var node = Grid[idx];

				if ( node == null ) continue;
				node.Walkable = walkable;
				node.ZOffset = zOffset;
				node.Slope = slope;
			}
		}
	}

	public static int CalculatePath( Vector3 start, Vector3 end, Vector3[] points, bool mustBeFullPath = false )
	{
		if ( !IsReady )
			return 0;

		if ( !CalculatePath( FromWorld( start ), FromWorld( end ), mustBeFullPath ) )
			return 0;

		var length = Math.Min( CalculatedPath.Count, points.Length );

		for ( int i = 0; i < length; i++ )
		{
			points[i] = ToWorld( CalculatedPath[i] );
		}

		return length;
	}

	public static float Distance( Vector3 a, Vector3 b )
	{
		var indexA = FromWorld( a );
		var indexB = FromWorld( b );

		if ( indexA == -1 || indexB == -1 ) 
			return 0;

		return Grid[indexA].Position.Distance( Grid[indexB].Position );
	}

	private static int GetIndex( Vector2 point ) => GetIndex( (int)point.x, (int)point.y );
	private static int GetIndex( int x, int y )
	{
		var result = x * (int)GridSize.y + y;
		return IsOnMap( result ) ? result : -1;
	}

	private static Vector2 GetPosition( int index )
	{
		int x = (int)(index / GridSize.y);
		int y = (int)(index % GridSize.y);
		return new Vector2( x, y );
	}

	private static bool IsOnMap( int index )
	{
		var pos = GetPosition( index );

		return pos.x >= 0 && pos.x < GridSize.x && pos.y >= 0 && pos.y < GridSize.y;
	}

	private static Vector3 ToWorld( int idx )
	{
		return GetPosition( idx ) * CellSize + WorldBounds.Position;
	}

	private static int FromWorld( Vector3 world )
	{
		world -= (Vector3)WorldBounds.Position;

		return GetIndex( (int)MathF.Round( world.x / CellSize ), (int)MathF.Round( world.y / CellSize ) );
	}

	private static void ResetCollections()
	{
		CalculatedPath.Clear();
		OpenSet.Clear();

		foreach ( var node in Grid )
		{
			node.Opened = false;
			node.Closed = false;
			node.GScore = float.PositiveInfinity;
			node.FScore = float.PositiveInfinity;
			node.Parent = null;
		}
	}

	static List<Vector2> DirectionLut = new()
	{
		Vector2.Left,
		Vector2.Right,
		Vector2.Up,
		Vector2.Down,
		Vector2.Left + Vector2.Up,
		Vector2.Right + Vector2.Up,
		Vector2.Left + Vector2.Down,
		Vector2.Right + Vector2.Down
	};

	public static Vector3 FindNearestWalkable( Vector3 worldPoint )
	{
		var idx = FromWorld( worldPoint );
		if ( idx == -1 ) return worldPoint;

		if ( !FindNearestWalkable( idx, out var newPos ) )
		{
			return worldPoint;
		}

		return ToWorld( newPos );
	}

	private static bool FindNearestWalkable( int to, out int result )
	{
		var bestDist = float.MaxValue;
		var startPoint = GetPosition( to );
		result = -1;

		foreach ( var dir in DirectionLut )
		{
			var samplePoint = startPoint + dir;
			var sampleDist = (int)startPoint.Distance( samplePoint );
			if ( sampleDist >= bestDist ) continue;

			var idx = GetIndex( samplePoint );
			if ( !IsWalkable( Grid[idx] ) ) continue;

			bestDist = sampleDist;
			result = idx;
		}

		return result != -1;
	}

	private static bool CalculatePath( int start, int end, bool mustBeFullPath = false )
	{
		if( start == -1 || end == -1 )
		{
			return false;
		}

		var endNode = Grid[end];
		var startNode = Grid[start];

		if ( IsOccupied( startNode ) )
		{
			return false;
		}

		ResetCollections();

		startNode.GScore = 0;
		startNode.FScore = Heuristic( startNode, endNode );
		startNode.Parent = startNode;
		OpenSet.Enqueue( startNode, startNode.FScore );

		bool discovered = false;
		GridNode currentNode = null;
		GridNode fallbackNode = null;
		float fallbackScore = float.MaxValue;

		while ( OpenSet.Count > 0 )
		{
			currentNode = OpenSet.Dequeue();

			if ( currentNode == endNode )
			{
				discovered = true;
				break;
			}

			currentNode.Closed = true;

			var unreachable = false;

			foreach ( var neighborNode in currentNode.Neighbors )
			{
				if ( neighborNode == null ) continue;
				if ( neighborNode.Closed ) continue;

				if ( !IsWalkable( neighborNode ) )
				{
					if ( neighborNode == endNode )
					{
						unreachable = true;
						break;
					}

					continue;
				}

				if ( !neighborNode.Opened )
				{
					neighborNode.GScore = float.PositiveInfinity;
					neighborNode.Parent = null;
				}

				var distance = Euclidian( neighborNode, endNode );

				if ( distance < fallbackScore )
				{
					fallbackScore = distance;
					fallbackNode = neighborNode;
				}

				var cost = currentNode.GScore + Euclidian( currentNode, neighborNode );
				if ( cost < neighborNode.GScore )
				{
					neighborNode.Parent = currentNode;
					neighborNode.GScore = cost;
					neighborNode.FScore = cost + Heuristic( neighborNode, endNode );
					neighborNode.Opened = true;
					OpenSet.Enqueue( neighborNode, neighborNode.FScore );
				}
			}

			if ( unreachable )
			{
				if ( fallbackNode != null )
				{
					var d1 = Euclidian( startNode, endNode );
					var d2 = Euclidian( fallbackNode, endNode );
					if ( d1 <= d2 ) return false;
				}

				break;
			}
		}

		if ( !discovered )
		{
			if ( mustBeFullPath )
				return false;

			currentNode = fallbackNode;
		}

		if ( currentNode == null )
			return false;

		while ( currentNode != startNode )
		{
			CalculatedPath.Add( currentNode.Index );
			currentNode = currentNode.Parent;
		}

		CalculatedPath.Add( start );
		CalculatedPath.Reverse();

		SimplifyCalculatedPath();

		return true;
	}

	public static bool IsWalkable( Vector3 position )
	{
		var idx = FromWorld( position );
		if ( idx == -1 ) return false;

		return IsWalkable( Grid[idx] );
	}

	private static bool IsWalkable( GridNode node )
	{
		if ( node == null )
			return false;

		if ( !node.Walkable )
			return false;

		if ( IsOccupied( node ) )
			return false;

		return true;
	}

	private static bool IsOccupied( GridNode node )
	{
		return false;
	}

	private static float Euclidian( GridNode from, GridNode to )
	{
		return from.Position.Distance( to.Position );
	}

	private static float Heuristic( GridNode from, GridNode to )
	{
		return Math.Abs( from.Position.x - to.Position.x ) + Math.Abs( from.Position.y - to.Position.y );
	}

	private static void SimplifyCalculatedPath()
	{
		if ( CalculatedPath.Count <= 2 )
			return;

		var a = 0;
		var b = CalculatedPath.Count - 1;

		while ( b > 0 && b > a )
		{
			var pointA = CalculatedPath[a];
			var pointB = CalculatedPath[b];

			if ( LineOfSight( Grid[pointA], Grid[pointB] ) )
			{
				for( int i = a + 1; i < b; i++ )
				{
					CalculatedPath[i] = -1;
				}

				a = b;
				b = CalculatedPath.Count - 1;
			}
			else
			{
				b--;
			}
		}

		for ( int i = CalculatedPath.Count - 1; i >= 0; i-- )
		{
			if ( CalculatedPath[i] != -1 )
				continue;

			CalculatedPath.RemoveAt( i );
		}
	}

	static int[] LineCache = new int[1024];
	private static bool LineOfSight( GridNode from, GridNode to )
	{
		var lineCount = GetStraightLine( from.Index, to.Index, 1, LineCache );
		for ( int i = 0; i < lineCount; i++ )
		{
			if ( !IsWalkable( Grid[LineCache[i]] ) )
			{
				return false;
			}
		}
		return true;
	}

	private static int GetStraightLine( int idx0, int idx1, int width, int[] cache )
	{
		var p0 = GetPosition( idx0 );
		var p1 = GetPosition( idx1 );
		var x0 = (int)p0.x;
		var x1 = (int)p1.x;
		var y0 = (int)p0.y;
		var y1 = (int)p1.y;

		int dx = (int)Math.Abs( x1 - x0 ), sx = x0 < x1 ? 1 : -1;
		int dy = (int)Math.Abs( y1 - y0 ), sy = y0 < y1 ? 1 : -1;
		int err = dx - dy, e2, x2, y2; /* error value e_xy */
		float ed = dx + dy == 0 ? 1 : (float)Math.Sqrt( (float)dx * dx + (float)dy * dy );

		var result = 0;

		for ( width = (width + 1) / 2; ; )
		{
			cache[result] = GetIndex( x0, y0 );
			result++;
			e2 = err; x2 = x0;

			if ( 2 * e2 >= -dx )
			{
				for ( e2 += dy, y2 = y0; e2 < ed * width && (y1 != y2 || dx > dy); e2 += dx )
				{
					cache[result] = GetIndex( x0, y2 += sy );
					result++;
				}

				if ( x0 == x1 ) break;
				e2 = err; err -= dy; x0 += sx;
			}

			if ( 2 * e2 <= dy )
			{
				for ( e2 = dx - e2; e2 < ed * width && (x1 != x2 || dx < dy); e2 += dy )
				{
					cache[result] = GetIndex( x2 += sx, y0 );
					result++;
				}

				if ( y0 == y1 ) break;
				err += dx; y0 += sy;
			}
		}

		return result;
	}
}
