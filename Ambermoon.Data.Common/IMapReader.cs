using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IMapReader
    {
        void ReadMap(Map map, IDataReader dataReader, IDataReader textDataReader, Dictionary<uint, Tileset> tilesets);
        List<string> ReadMapTexts(IDataReader textDataReader);
    }
}
