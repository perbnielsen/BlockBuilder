using UnityEngine;


public struct Position3
{
	public static readonly Position3 right = new Position3( 1, 0, 0 );
	public static readonly Position3 left = new Position3( -1, 0, 0 );
	public static readonly Position3 up = new Position3( 0, 1, 0 );
	public static readonly Position3 down = new Position3( 0, -1, 0 );
	public static readonly Position3 forward = new Position3( 0, 0, 1 );
	public static readonly Position3 back = new Position3( 0, 0, -1 );
	public int x, y, z;


	public Position3( int x, int y, int z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}


	public static implicit operator Vector3( Position3 position )
	{
		return new Vector3( position.x, position.y, position.z );
	}


	public static implicit operator Position3( Vector3 position )
	{
		return new Position3( Mathf.RoundToInt( position.x ), Mathf.RoundToInt( position.y ), Mathf.RoundToInt( position.z ) );
	}


	public static Position3 operator +( Position3 positionA, Position3 positionB )
	{
		return new Position3( positionA.x + positionB.x, positionA.y + positionB.y, positionA.z + positionB.z );
	}


	public static Position3 operator *( Position3 a, int b )
	{
		return new Position3( a.x * b, a.y * b, a.z * b );
	}


	public static Position3 operator /( Position3 a, int b )
	{
		return new Position3( a.x / b, a.y / b, a.z / b );
	}


	public static Position3 operator %( Position3 a, int b )
	{
		return new Position3( a.x % b, a.y % b, a.z % b );
	}


	public static bool operator !=( Position3 a, Position3 b )
	{
		return !(a == b);
	}


	public static bool operator ==( Position3 a, Position3 b )
	{
		return (a.x == b.x && a.y == b.y && a.z == b.z);
	}


	public override bool Equals( System.Object obj )
	{
		return ((obj is Position3) && (this == (Position3)obj));
	}


	public override string ToString()
	{
		return "(" + x + ", " + y + ", " + z + ")";
	}


	public override int GetHashCode()
	{
		return x * 197 + y * 211 + z * 227;
	}
}
