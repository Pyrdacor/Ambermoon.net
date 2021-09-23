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
                dataWriter.Write((uint)tile.Flags);
                dataWriter.Write((ushort)tile.GraphicIndex);
                dataWriter.Write((byte)tile.NumAnimationFrames);
                dataWriter.Write(tile.ColorIndex);
            }
        }
    }
}
