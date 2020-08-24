using System.Drawing;

namespace Ambermoon.Geometry
{
    public static class PointExtensions
    {
        public static Position Round(this PointF point)
        {
            return new Position(Util.Round(point.X), Util.Round(point.Y));
        }
    }
}
