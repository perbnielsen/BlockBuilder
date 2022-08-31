using UnityEngine;
using System;

public struct Position3 : IComparable<Position3>
{
    public static Position3 Right => new(1, 0, 0);
    public static Position3 Left => new(-1, 0, 0);
    public static Position3 Up => new(0, 1, 0);
    public static Position3 Down => new(0, -1, 0);
    public static Position3 Forward => new(0, 0, 1);
    public static Position3 Back => new(0, 0, -1);
    public static Position3 Zero => new(0, 0, 0);

    public readonly int x;
    public readonly int y;
    public readonly int z;

    public Position3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static implicit operator Vector3(Position3 position)
    {
        return new Vector3(position.x, position.y, position.z);
    }

    public static implicit operator Position3(Vector3 position) => new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));

    public static Position3 operator +(Position3 positionA, Position3 positionB) => new(positionA.x + positionB.x, positionA.y + positionB.y, positionA.z + positionB.z);

    public static Position3 operator -(Position3 positionA, Position3 positionB) => new(positionA.x - positionB.x, positionA.y - positionB.y, positionA.z - positionB.z);

    public static Position3 operator *(int b, Position3 a) => new(a.x * b, a.y * b, a.z * b);

    public static Position3 operator *(Position3 a, int b) => new(a.x * b, a.y * b, a.z * b);

    public static Position3 operator /(Position3 a, int b) => new(a.x / b, a.y / b, a.z / b);

    public static Position3 operator %(Position3 a, int b) => new(a.x % b, a.y % b, a.z % b);

    public static bool operator !=(Position3 a, Position3 b) => !(a == b);

    public static bool operator ==(Position3 a, Position3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

    public override bool Equals(object obj)
    {
        return (obj is Position3 position) && (this == position);
    }

    int IComparable<Position3>.CompareTo(Position3 position)
    {
        //		if ( position == null ) return 1;
        int xCompared = x.CompareTo(position.x);
        if (xCompared != 0) return xCompared;

        int yCompared = y.CompareTo(position.y);
        if (yCompared != 0) return yCompared;

        int zCompared = z.CompareTo(position.z);
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

    public int SqrMagnitude => x * x + y * y + z * z;

    public float Magnitude => Mathf.Sqrt(Magnitude);
}
