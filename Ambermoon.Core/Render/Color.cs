/*
 * Color.cs - Basic color implementation
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

namespace Ambermoon.Render
{
    public class Color : IEquatable<Color>, IEqualityComparer<Color>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color()
        {
            R = 0;
            G = 0;
            B = 0;
            A = 255;
        }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b, int a = 255)
            : this((byte)r, (byte)g, (byte)b, (byte)a)
        {

        }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = (byte)Util.Round(r * 255.0f);
            G = (byte)Util.Round(g * 255.0f);
            B = (byte)Util.Round(b * 255.0f);
            A = (byte)Util.Round(a * 255.0f);
        }

        public static readonly Color Transparent = new Color(0x00, 0x00, 0x00, 0x00);
        public static readonly Color Black = new Color(0x00, 0x00, 0x00);
        public static readonly Color Green = new Color(0x73, 0xb3, 0x43);
        public static readonly Color White = new Color(0xff, 0xff, 0xff);

        public static bool operator ==(Color color1, Color color2)
        {
            if (ReferenceEquals(color1, color2))
                return true;

            if (ReferenceEquals(color1, null) || ReferenceEquals(color2, null))
                return false;

            return color1.R == color2.R && color1.G == color2.G &&
                color1.B == color2.B && color1.A == color2.A;
        }

        public static bool operator !=(Color color1, Color color2)
        {
            if (ReferenceEquals(color1, color2))
                return false;

            if (ReferenceEquals(color1, null) || ReferenceEquals(color2, null))
                return true;

            return color1.R != color2.R || color1.G != color2.G ||
                color1.B != color2.B || color1.A != color2.A;
        }

        public bool Equals(Color other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Color)
                return Equals(obj as Color);

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // overflow is fine, just wrap
            {
                int hash = 17;

                hash = hash * 23 + R.GetHashCode();
                hash = hash * 23 + G.GetHashCode();
                hash = hash * 23 + B.GetHashCode();
                hash = hash * 23 + A.GetHashCode();

                return hash;
            }
        }

        public bool Equals(Color x, Color y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null);

            return x == y;
        }

        public int GetHashCode(Color obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }
}
