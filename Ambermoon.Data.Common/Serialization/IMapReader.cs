using System.Collections.Generic;

namespace Ambermoon.Data.Serialization
{
    public interface IMapReader
    {
        void ReadMap(Map map, IDataReader dataReader, Dictionary<uint, Tileset> tilesets);
        void ReadMapTexts(Map map, IDataReader textDataReader);
    }
}
