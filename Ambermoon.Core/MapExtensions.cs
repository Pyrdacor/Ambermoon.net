using Ambermoon.Data;

namespace Ambermoon
{
    public static class MapExtensions
    {
        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;
    }
}
