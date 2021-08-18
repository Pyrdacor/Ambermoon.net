/*
 * PositionExtensions.cs - Extensions for positions
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon.Geometry
{
    public static class PositionExtensions
    {
        static readonly Direction?[] directions = new Direction?[9]
        {
            Direction.UpLeft,
            Direction.Up,
            Direction.UpRight,
            Direction.Left,
            null,
            Direction.Right,
            Direction.DownLeft,
            Direction.Down,
            Direction.DownRight
        };

        public static Direction? GetDirectionTo(this Position position, Position target)
        {
            int xOffset = 1;

            if (target.X > position.X) // right
            {
                xOffset = 2;
            }
            else if (target.X < position.X) // left
            {
                xOffset = 0;
            }

            int yOffset = 1;

            if (target.X > position.X) // down
            {
                yOffset = 2;
            }
            else if (target.X < position.X) // up
            {
                yOffset = 0;
            }

            return directions[xOffset + yOffset * 3];
        }

        public static uint GetMaxDistance(this Position position, Position target)
        {
            return (uint)Math.Max(Math.Abs(target.X - position.X), Math.Abs(target.Y - position.Y));
        }

        public static float GetMaxDistance(this FloatPosition position, FloatPosition target)
        {
            return Math.Max(Math.Abs(target.X - position.X), Math.Abs(target.Y - position.Y));
        }
    }
}
