using Ambermoon.Data.Pyrdacor.Objects;

namespace Ambermoon.Data.Pyrdacor
{
    internal class MapManager(
        Func<Dictionary<uint, Map>> mapProvider,
        Func<Dictionary<uint, TextList>> mapTextProvider,
        Func<Dictionary<uint, Labdata>> labdataProvider,
        Func<Dictionary<uint, Tileset>> tilesetProvider
    ) : IMapManager
    {
        bool mapTextsAdded = false;
        readonly Lazy<Dictionary<uint, Map>> maps = new(mapProvider);
        readonly Lazy<Dictionary<uint, TextList>> mapTexts = new(mapTextProvider);
        readonly Lazy<Dictionary<uint, Labdata>> labdata = new(labdataProvider);
        readonly Lazy<Dictionary<uint, Tileset>> tilesets = new(tilesetProvider);

        public Map? GetMap(uint index)
        {
            var map = index == 0 || !maps.Value.TryGetValue(index, out Map? value) ? null : value;

            if (map != null && map.Texts.Count == 0)
            {
                map.Texts = mapTexts.Value.TryGetValue(index, out TextList? textList) ? textList.ToList() : [];
            }

            return map;
        }

        public Tileset? GetTilesetForMap(Map map) => map.Type == MapType.Map2D ? null : tilesets.Value!.GetValueOrDefault(map.TilesetOrLabdataIndex, null);

        public Labdata? GetLabdataForMap(Map map) => map.Type == MapType.Map2D ? null : labdata.Value!.GetValueOrDefault(map.TilesetOrLabdataIndex, null);

        public IReadOnlyList<Map> Maps
        {
            get
            {
                if (!mapTextsAdded)
                {
                    foreach (var map in maps.Value.Values)
                    {
                        if (map.Texts.Count == 0)
                        {
                            map.Texts = mapTexts.Value.TryGetValue(map.Index, out TextList? textList) ? textList.ToList() : [];
                        }
                    }

                    mapTextsAdded = true;
                }

                return maps.Value.Values.ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<Labdata> Labdata => labdata.Value.Values.ToList().AsReadOnly();

        public IReadOnlyList<Tileset> Tilesets => tilesets.Value.Values.ToList().AsReadOnly();
    }
}
