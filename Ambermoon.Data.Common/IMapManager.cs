using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IMapManager
    {
        IReadOnlyList<Map> Maps { get; }
        Map GetMap(uint index);
        Tileset GetTilesetForMap(Map map);
        Labdata GetLabdataForMap(Map map);
    }
}
