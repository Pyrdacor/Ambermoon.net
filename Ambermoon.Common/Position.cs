using System;
using System.Collections.Generic;

namespace Ambermoon
{
    public class Position : IEquatable<Position>, IEqualityComparer<Position>
    {
        public static readonly Position Zero = new Position(0, 0);

        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;

        public Position()
        {

        }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position(Position position)
        {
            X = position.X;
            Y = position.Y;
        }

        public void Normalize()
        {
            int xAbs = Math.Abs(X);
            int yAbs = Math.Abs(Y);

            if (X < 0)
                X = -1;
            else if (X > 0)
                X = 1;

            if (Y < 0)
                Y = -1;
            else if (Y > 0)
                Y = 1;

            if (xAbs > 0 && yAbs > 0)
            {
                X *= xAbs < yAbs / 2 ? 0 : 1;
                Y *= yAbs < xAbs / 2 ? 0 : 1;
            }
        }

        public void Offset(int x, int y)
        {
            X += x;
            Y += y;
        }

        public void Offset(Position position)
        {
            Offset(position.X, position.Y);
        }

        public static Position operator +(Position position1, Position position2)
        {
            return new Position(position1.X + position2.X, position1.Y + position2.Y);
        }

        public static Position operator -(Position position1, Position position2)
        {
            return new Position(position1.X - position2.X, position1.Y - position2.Y);
        }

        public static FloatPosition operator *(Position position, float factor)
        {
            return new FloatPosition(position.X * factor, position.Y * factor);
        }

        public static FloatPosition operator *(float factor, Position position)
        {
            return position * factor;
        }

        public static bool operator ==(Position position1, Position position2)
        {
            if (ReferenceEquals(position1, position2))
                return true;

            if (ReferenceEquals(position1, null) || ReferenceEquals(position2, null))
                return false;

            return position1.X == position2.X && position1.Y == position2.Y;
        }

        public static bool operator !=(Position position1, Position position2)
        {
            if (ReferenceEquals(position1, position2))
                return false;

            if (ReferenceEquals(position1, null) || ReferenceEquals(position2, null))
                return true;

            return position1.X != position2.X || position1.Y != position2.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Position)
                return Equals(obj as Position);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public bool Equals(Position other)
        {
            return this == other;
        }

        public bool Equals(Position x, Position y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            return x == y;
        }

        public int GetHashCode(Position obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }

    public class FloatPosition : IEquatable<FloatPosition>, IEqualityComparer<FloatPosition>
    {
        public static readonly FloatPosition Zero = new FloatPosition(0.0f, 0.0f);

        public float X { get; set; } = 0.0f;
        public float Y { get; set; } = 0.0f;

        public FloatPosition()
        {

        }

        public FloatPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        public FloatPosition(FloatPosition position)
        {
            X = position.X;
            Y = position.Y;
        }

        public FloatPosition(Position position)
        {
            X = position.X;
            Y = position.Y;
        }

        public void Normalize()
        {
            float xAbs = Math.Abs(X);
            float yAbs = Math.Abs(Y);

            if (X < -0.0001f)
                X = -1.0f;
            else if (X > 0.0001f)
                X = 1.0f;

            if (Y < -0.0001f)
                Y = -1.0f;
            else if (Y > 0.0001f)
                Y = 1.0f;

            if (xAbs >= 0.00001f && yAbs >= 0.00001f)
            {
                X *= xAbs < yAbs ? xAbs / yAbs : 1.0f;
                Y *= yAbs < xAbs ? yAbs / xAbs : 1.0f;
            }
        }

        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        public void Offset(FloatPosition position)
        {
            Offset(position.X, position.Y);
        }

        public void Offset(Position position)
        {
            Offset(position.X, position.Y);
        }

        public Position Round()
        {
            return new Position(Util.Round(X), Util.Round(Y));
        }

        public Position Round(float factor)
        {
            return new Position(Util.Round(factor * X), Util.Round(factor * Y));
        }

        public static FloatPosition operator +(FloatPosition position1, FloatPosition position2)
        {
            return new FloatPosition(position1.X + position2.X, position1.Y + position2.Y);
        }

        public static FloatPosition operator -(FloatPosition position1, FloatPosition position2)
        {
            return new FloatPosition(position1.X - position2.X, position1.Y - position2.Y);
        }

        public static FloatPosition operator *(FloatPosition position, float factor)
        {
            return new FloatPosition(position.X * factor, position.Y * factor);
        }

        public static FloatPosition operator *(float factor, FloatPosition position)
        {
            return position * factor;
        }

        public static bool operator ==(FloatPosition position1, FloatPosition position2)
        {
            if (ReferenceEquals(position1, position2))
                return true;

            if (ReferenceEquals(position1, null) || ReferenceEquals(position2, null))
                return false;

            return Util.FloatEqual(position1.X, position2.X) && Util.FloatEqual(position1.Y, position2.Y);
        }

        public static bool operator !=(FloatPosition position1, FloatPosition position2)
        {
            if (ReferenceEquals(position1, position2))
                return false;

            if (ReferenceEquals(position1, null) || ReferenceEquals(position2, null))
                return true;

            return !Util.FloatEqual(position1.X, position2.X) || !Util.FloatEqual(position1.Y, position2.Y);
        }

        public static implicit operator FloatPosition(Position position)
        {
            return new FloatPosition(position);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is FloatPosition)
                return Equals(obj as FloatPosition);

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // overflow is fine, just wrap
            {
                int hash = 17;

                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();

                return hash;
            }
        }

        public bool Equals(FloatPosition other)
        {
            return this == other;
        }

        public bool Equals(FloatPosition x, FloatPosition y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            return x == y;
        }

        public int GetHashCode(FloatPosition obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }
}
