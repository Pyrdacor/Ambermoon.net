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
