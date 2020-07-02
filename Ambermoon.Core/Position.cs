/*
 * Position.cs - Basic position implementation
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Ambermoon
{
    public class Position : IEquatable<Position>, IEqualityComparer<Position>
    {
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

        public void Offset(int x, int y)
        {
            X += x;
            Y += y;
        }

        public void Offset(Position position)
        {
            Offset(position.X, position.Y);
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
            unchecked // overflow is fine, just wrap
            {
                int hash = 17;

                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();

                return hash;
            }
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
