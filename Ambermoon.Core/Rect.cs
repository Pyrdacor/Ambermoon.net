/*
 * Rect.cs - Basic rectangle implementation
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
    public class Rect : IEquatable<Rect>, IEqualityComparer<Rect>
    {
        private Position position = new Position();
        private Size size = new Size();

        public Position Position
        {
            get => position;
            set => position = new Position(value);
        }

        public Size Size
        {
            get => size;
            set => size = new Size(value);
        }

        public int Left => Position.X;
        public int Right => Position.X + Size.Width;
        public int Top => Position.Y;
        public int Bottom => Position.Y + Size.Height;
        public bool Empty => Size.Empty;
        public Position Center => new Position(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);

        public Rect()
        {

        }

        public Rect(int x, int y, int width, int height)
        {
            position.X = x;
            position.Y = y;
            size.Width = width;
            size.Height = height;
        }

        public Rect(Position position, Size size)
        {
            this.position.X = position.X;
            this.position.Y = position.Y;
            this.size.Width = size.Width;
            this.size.Height = size.Height;
        }

        public Rect(Rect rect)
        {
            position.X = rect.Position.X;
            position.Y = rect.Position.Y;
            size.Width = rect.Size.Width;
            size.Height = rect.Size.Height;
        }

        public static Rect CreateFromBoundaries(int left, int top, int right, int bottom)
        {
            return new Rect(left, top, right - left, bottom - top);
        }

        public static Rect Create(Position center, Size size)
        {
            return new Rect(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
        }

        public void Clip(int left, int top, int right, int bottom)
        {
            if (Left < left)
                position.X = left;

            if (Top < top)
                position.Y = top;

            if (right <= left)
                size.Width = 0;
            else if (Right > right)
                size.Width = right - Position.X;

            if (bottom <= top)
                size.Height = 0;
            else if (Bottom > bottom)
                size.Height = bottom - Position.Y;
        }

        public void Clip(Rect rect)
        {
            Clip(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public Rect CreateShrinked(int shrinkAmount)
        {
            if (size.Width <= 2 * shrinkAmount ||
                size.Height <= 2 * shrinkAmount)
                return new Rect(0, 0, 0, 0);

            return new Rect(position.X + shrinkAmount, position.Y + shrinkAmount, size.Width - 2 * shrinkAmount, size.Height - 2 * shrinkAmount);
        }

        public bool Contains(int x, int y)
        {
            if (Empty)
                return false;

            return x >= Left && x <= Right && y >= Top && y <= Bottom;
        }

        public bool Contains(Position point)
        {
            return Contains(point.X, point.Y);
        }

        public bool IntersectsWith(Rect rect)
        {
            if (rect.Right <= Left || Right <= rect.Left ||
                rect.Bottom <= Top || Bottom <= rect.Top)
                return false;

            return true;
        }

        public static bool operator ==(Rect rect1, Rect rect2)
        {
            if (ReferenceEquals(rect1, rect2))
                return true;

            if (ReferenceEquals(rect1, null) || ReferenceEquals(rect2, null))
                return false;

            return rect1.Position == rect2.Position && rect1.Size == rect2.Size;
        }

        public static bool operator !=(Rect rect1, Rect rect2)
        {
            if (ReferenceEquals(rect1, rect2))
                return false;

            if (ReferenceEquals(rect1, null) || ReferenceEquals(rect2, null))
                return true;

            return rect1.Position != rect2.Position || rect1.Size != rect2.Size;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Rect)
                return Equals(obj as Rect);

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // overflow is fine, just wrap
            {
                int hash = 29;

                hash = (position == null) ? hash : hash * 23 + position.GetHashCode();
                hash = (size == null) ? hash : hash * 23 + size.GetHashCode();

                return hash;
            }
        }

        public bool Equals(Rect other)
        {
            return this == other;
        }

        public bool Equals(Rect x, Rect y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            return x == y;
        }

        public int GetHashCode(Rect obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }

    public class FloatRect : IEquatable<FloatRect>, IEqualityComparer<FloatRect>
    {
        private FloatPosition position = new FloatPosition();
        private FloatSize size = new FloatSize();

        public FloatPosition Position
        {
            get => position;
            set => position = new FloatPosition(value);
        }

        public FloatSize Size
        {
            get => size;
            set => size = new FloatSize(value);
        }

        public float Left => Position.X;
        public float Right => Position.X + Size.Width;
        public float Top => Position.Y;
        public float Bottom => Position.Y + Size.Height;
        public bool Empty => Size.Empty;
        public FloatPosition Center => new FloatPosition(Position.X + 0.5f * Size.Width, Position.Y + 0.5f * Size.Height);

        public FloatRect()
        {

        }

        public FloatRect(float x, float y, float width, float height)
        {
            position.X = x;
            position.Y = y;
            size.Width = width;
            size.Height = height;
        }

        public FloatRect(FloatPosition position, FloatSize size)
        {
            this.position.X = position.X;
            this.position.Y = position.Y;
            this.size.Width = size.Width;
            this.size.Height = size.Height;
        }

        public FloatRect(Position position, Size size)
        {
            this.position.X = position.X;
            this.position.Y = position.Y;
            this.size.Width = size.Width;
            this.size.Height = size.Height;
        }

        public FloatRect(FloatRect rect)
        {
            position.X = rect.Position.X;
            position.Y = rect.Position.Y;
            size.Width = rect.Size.Width;
            size.Height = rect.Size.Height;
        }

        public FloatRect(Rect rect)
        {
            position.X = rect.Position.X;
            position.Y = rect.Position.Y;
            size.Width = rect.Size.Width;
            size.Height = rect.Size.Height;
        }

        public static FloatRect CreateFromBoundaries(float left, float top, float right, float bottom)
        {
            return new FloatRect(left, top, right - left, bottom - top);
        }

        public static FloatRect Create(FloatPosition center, FloatSize size)
        {
            return new FloatRect(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
        }

        public static FloatRect Create(Position center, Size size)
        {
            return new FloatRect(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
        }

        public void Clip(float left, float top, float right, float bottom)
        {
            if (Left < left)
                position.X = left;

            if (Top < top)
                position.Y = top;

            if (right <= left)
                size.Width = 0;
            else if (Right > right)
                size.Width = right - Position.X;

            if (bottom <= top)
                size.Height = 0;
            else if (Bottom > bottom)
                size.Height = bottom - Position.Y;
        }

        public void Clip(FloatRect rect)
        {
            Clip(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public void Clip(Rect rect)
        {
            Clip(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public bool Contains(float x, float y)
        {
            if (Empty)
                return false;

            return x >= Left && x <= Right && y >= Top && y <= Bottom;
        }

        public bool Contains(FloatPosition point)
        {
            return Contains(point.X, point.Y);
        }

        public bool Contains(Position point)
        {
            return Contains(point.X, point.Y);
        }

        public bool IntersectsWith(FloatRect rect)
        {
            if (rect.Right <= Left || Right <= rect.Left ||
                rect.Bottom <= Top || Bottom <= rect.Top)
                return false;

            return true;
        }

        public bool IntersectsWith(Rect rect)
        {
            if (rect.Right <= Left || Right <= rect.Left ||
                rect.Bottom <= Top || Bottom <= rect.Top)
                return false;

            return true;
        }

        public static bool operator ==(FloatRect rect1, FloatRect rect2)
        {
            if (ReferenceEquals(rect1, rect2))
                return true;

            if (ReferenceEquals(rect1, null) || ReferenceEquals(rect2, null))
                return false;

            return rect1.Position == rect2.Position && rect1.Size == rect2.Size;
        }

        public static bool operator !=(FloatRect rect1, FloatRect rect2)
        {
            if (ReferenceEquals(rect1, rect2))
                return false;

            if (ReferenceEquals(rect1, null) || ReferenceEquals(rect2, null))
                return true;

            return rect1.Position != rect2.Position || rect1.Size != rect2.Size;
        }

        public static implicit operator FloatRect(Rect rect)
        {
            return new FloatRect(rect);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is FloatRect)
                return Equals(obj as FloatRect);

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // overflow is fine, just wrap
            {
                int hash = 29;

                hash = (position == null) ? hash : hash * 23 + position.GetHashCode();
                hash = (size == null) ? hash : hash * 23 + size.GetHashCode();

                return hash;
            }
        }

        public bool Equals(FloatRect other)
        {
            return this == other;
        }

        public bool Equals(FloatRect x, FloatRect y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            return x == y;
        }

        public int GetHashCode(FloatRect obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }

    public static class RectConversionExtensions
    {
        public static Rect ConvertToRect(this FloatRect rect)
        {
            return new Rect(Util.Round(rect.Position.X),
                Util.Round(rect.Position.Y),
                Util.Round(rect.Size.Width),
                Util.Round(rect.Size.Height));
        }

        public static FloatRect ConvertToFloatRect(this Rect rect)
        {
            return new FloatRect(rect.Position.X, rect.Position.Y, rect.Size.Width, rect.Size.Height);
        }
    }
}
