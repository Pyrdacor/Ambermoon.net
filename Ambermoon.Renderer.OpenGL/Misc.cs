/*
 * Misc.cs - Helper functions
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

namespace Ambermoon.Renderer
{
    internal static class Misc
    {
        public static bool FloatEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.00001f;
        }

        public static int Floor(float f)
        {
            return (int)Math.Floor(f);
        }

        public static int Floor(double d)
        {
            return (int)Math.Floor(d);
        }

        public static int Ceiling(float f)
        {
            return (int)Math.Ceiling(f);
        }

        public static int Ceiling(double d)
        {
            return (int)Math.Ceiling(d);
        }

        public static int Round(float f)
        {
            return (int)Math.Round(f);
        }

        public static int Round(double d)
        {
            return (int)Math.Round(d);
        }

        public static float Min(float firstValue, float secondValue, params float[] values)
        {
            float min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static float Max(float firstValue, float secondValue, params float[] values)
        {
            float max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }
    }
}
