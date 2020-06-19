namespace Ambermoon.Data
{
    public class Map
    {
        public MapType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

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
