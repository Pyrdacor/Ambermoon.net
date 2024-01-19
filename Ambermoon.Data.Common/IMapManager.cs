using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IMapManager
    {
        IReadOnlyList<Map> Maps { get; }
        IReadOnlyList<Labdata> Labdata { get; }
        IReadOnlyList<Tileset> Tilesets { get; }
        Map GetMap(uint index);
        Tileset GetTilesetForMap(Map map);
        Labdata GetLabdataForMap(Map map);
    }
}
