using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class TilesetWriter
    {
        public static void WriteTileset(Tileset tileset, IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)tileset.Tiles.Length);

            foreach (var tile in tileset.Tiles)
            {
                uint flags = (uint)tile.Flags;
                flags &= 0x0c7100ff;
                flags |= (tile.CombatBackgroundIndex & 0xf) << 28;
                if (tile.Sleep)
                    flags |= (5u << 23);
                else if (tile.SitDirection != null)
                    flags |= ((uint)tile.SitDirection.Value << 23);
                flags |= ((uint)tile.AllowedTravelTypes & 0x7ff) << 8;

                dataWriter.Write(flags);
                dataWriter.Write((ushort)tile.GraphicIndex);
                dataWriter.Write((byte)tile.NumAnimationFrames);
                dataWriter.Write(tile.ColorIndex);
            }
        }
    }
}
