namespace Ambermoon.Data
{
    public class Tileset
    {
        public class Tile
        {
            public uint GraphicIndex { get; set; }
            public int NumAnimationFrames { get; set; }
        }

        public Tile[] Tiles { get; set; }

        private Tileset()
        {

        }

        public static Tileset Load(ITilesetReader tilesetReader, IDataReader dataReader)
        {
            var tileset = new Tileset();

            tilesetReader.ReadTileset(tileset, dataReader);

            return tileset;
        }
    }
}
