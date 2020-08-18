using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public class MapManager : IMapManager
    {
        readonly Dictionary<uint, Map> maps = new Dictionary<uint, Map>();
        readonly Dictionary<uint, Tileset> tilesets = new Dictionary<uint, Tileset>(8);
        readonly Dictionary<uint, Labdata> labdatas = new Dictionary<uint, Labdata>(29);

        public MapManager(IGameData gameData, IMapReader mapReader, ITilesetReader tilesetReader, ILabdataReader labdataReader)
        {
            foreach (var tilesetFile in gameData.Files["Icon_data.amb"].Files)
            {
                var tileset = Tileset.Load(tilesetReader, tilesetFile.Value);
                tilesets.Add((uint)tilesetFile.Key, tileset);
                tileset.Index = (uint)tilesetFile.Key;
            }

            // Map 1-256 -> File 1
            // Map 300-369 -> File 2
            // Map 257-299, 400-455, 513-528 -> File 3
            for (int i = 1; i <= 3; ++i)
            {
                var file = gameData.Files[$"{i}Map_data.amb"];
                var textFiles = gameData.Files[$"{i}Map_texts.amb"];

                foreach (var mapFile in file.Files)
                {
                    uint index = (uint)mapFile.Key;
                    var textFile = textFiles.Files.ContainsKey(mapFile.Key) ? textFiles.Files[mapFile.Key] : null;
                    maps.Add(index, Map.Load(index, mapReader, mapFile.Value, textFile, tilesets));
                }
            }

            foreach (var labdataFile in gameData.Files["2Lab_data.amb"].Files) // Note: 2Lab_data.amb and 3Lab_data.amb both contain all lab data files
            {
                var labdata = Labdata.Load(labdataReader, labdataFile.Value, gameData);
                labdatas.Add((uint)labdataFile.Key, labdata);
            }
        }

        public Map GetMap(uint index) => maps[index];
        public Tileset GetTilesetForMap(Map map) => tilesets[map.TilesetOrLabdataIndex];
        public Labdata GetLabdataForMap(Map map) => labdatas[map.TilesetOrLabdataIndex];

        public IEnumerable<Map> Maps => maps.Values;
    }
}
