using UnityEngine;


public class Position3
{
	public int x, y, z;


	public Position3( int x, int y, int z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}


	public Position3( Vector3 vector3 )
	{
		x = Mathf.RoundToInt( vector3.x );
		y = Mathf.RoundToInt( vector3.y );
		z = Mathf.RoundToInt( vector3.z );
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
		// If both are null, or both are same instance, return true.
		if ( System.Object.ReferenceEquals( a, b ) )
		{
			return true;
		}

		// If one is null, but not both, return false.
		if ( ((object)a == null) || ((object)b == null) )
		{
			return false;
		}

		return (a.x == b.x && a.y == b.y && a.z == b.z);
	}


	public override bool Equals( System.Object obj )
	{
		Position3 position = obj as Position3;

		if ( position == null ) return false;

		return (this == position);
	}


	public override int GetHashCode()
	{
		return x * 5 + y * 7 + z * 13;
	}


	public Vector3 toVector3()
	{
		return new Vector3( x, y, z );
	}
}


public class Block : MonoBehaviour
{
	public static bool isTransparent( byte block )
	{
		return (block == 0);
	}
}
