using UnityEngine;
using System;


public struct Position3 : IComparable
{
	public static Position3 right { get { return new Position3( 1, 0, 0 ); } }


	public static Position3 left { get { return new Position3( -1, 0, 0 ); } }


	public static Position3 up { get { return new Position3( 0, 1, 0 ); } }


	public static Position3 down { get { return new Position3( 0, -1, 0 ); } }


	public static Position3 forward { get { return new Position3( 0, 0, 1 ); } }


	public static Position3 back { get { return new Position3( 0, 0, -1 ); } }


	public static Position3 zero { get { return new Position3( 0, 0, 0 ); } }


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
		return new Position3( Mathf.FloorToInt( position.x ), Mathf.FloorToInt( position.y ), Mathf.FloorToInt( position.z ) );
	}


	public static Position3 operator +( Position3 positionA, Position3 positionB )
	{
		return new Position3( positionA.x + positionB.x, positionA.y + positionB.y, positionA.z + positionB.z );
	}


	public static Position3 operator -( Position3 positionA, Position3 positionB )
	{
		return new Position3( positionA.x - positionB.x, positionA.y - positionB.y, positionA.z - positionB.z );
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


	public int CompareTo( System.Object obj )
	{
		if ( obj == null ) return 1;

		Position3 objAsPosition3 = (Position3)obj;

		int xCompared = x.CompareTo( objAsPosition3.x );
		if ( xCompared != 0 ) return xCompared;

		int yCompared = y.CompareTo( objAsPosition3.y );
		if ( yCompared != 0 ) return yCompared;

		int zCompared = z.CompareTo( objAsPosition3.z );
		return zCompared;
	}


	public override string ToString()
	{
		return "(" + x + ", " + y + ", " + z + ")";
	}


	public override int GetHashCode()
	{
		return x * 197 + y * 211 + z * 227;
	}


	public int sqrMagnitude { get { return x * x + y * y + z * z; } }


	public float magnitude { get { return Mathf.Sqrt( magnitude ); } }
}
