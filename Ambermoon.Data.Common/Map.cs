namespace Ambermoon.Data
{
    public class Map
    {
        public class Tile
        {
            public uint BackGraphicIndex { get; set; }
            public uint FrontGraphicIndex { get; set; }
        }

        public MapType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint TilesetIndex { get; set; }
        public Tile[,] Tiles { get; set; }

        private Map()
        {

        }

        public static Map Load(IMapReader mapReader, IDataReader dataReader)
        {
            var map = new Map();

            mapReader.ReadMap(map, dataReader);

            return map;
        }
    }
}
