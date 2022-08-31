using UnityEngine;

public struct Position2
{
    public static Position2 Right => new(1, 0);
    public static Position2 Left => new(-1, 0);
    public static Position2 Up => new(0, 1);
    public static Position2 Down => new(0, -1);

    public int x, y;

    public Position2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator Vector2(Position2 position) => new(position.x, position.y);

    public static implicit operator Position2(Vector2 position) => new(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));

    public static Position2 operator +(Position2 positionA, Position2 positionB) => new(positionA.x + positionB.x, positionA.y + positionB.y);

    public static Position2 operator *(Position2 a, int b) => new(a.x * b, a.y * b);

    public static Position2 operator /(Position2 a, int b) => new(a.x / b, a.y / b);

    public static Position2 operator %(Position2 a, int b) => new(a.x % b, a.y % b);

    public static bool operator !=(Position2 a, Position2 b) => !(a == b);

    public static bool operator ==(Position2 a, Position2 b) => a.x == b.x && a.y == b.y;

    public override bool Equals(object obj) => (obj is Position2 position) && (this == position);

    public override string ToString()
    {
        return "(" + x + ", " + y + ")";
    }

    public override int GetHashCode()
    {
        return x * 1031 + y * 1249;
    }
}
