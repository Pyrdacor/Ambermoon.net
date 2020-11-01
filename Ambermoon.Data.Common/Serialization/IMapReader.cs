using System.Collections.Generic;

namespace Ambermoon.Data.Serialization
{
    public interface IMapReader
    {
        void ReadMap(Map map, IDataReader dataReader, IDataReader textDataReader, Dictionary<uint, Tileset> tilesets);
    }
}
