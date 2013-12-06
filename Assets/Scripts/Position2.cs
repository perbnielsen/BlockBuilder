using UnityEngine;


public struct Position2
{
	public static Position2 right { get { return new Position2( 1, 0 ); } }


	public static Position2 left { get { return new Position2( -1, 0 ); } }


	public static Position2 up { get { return new Position2( 0, 1 ); } }


	public static Position2 down { get { return new Position2( 0, -1 ); } }


	public int x, y;


	public Position2( int x, int y )
	{
		this.x = x;
		this.y = y;
	}


	public static implicit operator Vector2( Position2 position )
	{
		return new Vector2( position.x, position.y );
	}


	public static implicit operator Position2( Vector2 position )
	{
		return new Position2( Mathf.RoundToInt( position.x ), Mathf.RoundToInt( position.y ) );
	}


	public static Position2 operator +( Position2 positionA, Position2 positionB )
	{
		return new Position2( positionA.x + positionB.x, positionA.y + positionB.y );
	}


	public static Position2 operator *( Position2 a, int b )
	{
		return new Position2( a.x * b, a.y * b );
	}


	public static Position2 operator /( Position2 a, int b )
	{
		return new Position2( a.x / b, a.y / b );
	}


	public static Position2 operator %( Position2 a, int b )
	{
		return new Position2( a.x % b, a.y % b );
	}


	public static bool operator !=( Position2 a, Position2 b )
	{
		return !(a == b);
	}


	public static bool operator ==( Position2 a, Position2 b )
	{
		return (a.x == b.x && a.y == b.y);
	}


	public override bool Equals( System.Object obj )
	{
		return ((obj is Position2) && (this == (Position2)obj));
	}


	public override string ToString()
	{
		return "(" + x + ", " + y + ")";
	}


	public override int GetHashCode()
	{
		return x * 1031 + y * 1249;
	}
}
