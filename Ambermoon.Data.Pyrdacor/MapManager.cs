using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;

namespace Ambermoon.Data.Pyrdacor
{
    internal class MapManager(
        Func<Dictionary<uint, Map>> mapProvider,
        Func<Dictionary<uint, TextList>> mapTextProvider,
        Func<Dictionary<uint, Labdata>> labdataProvider,
        Func<Textures> texturesProvider,
        Func<Dictionary<uint, Tileset>> tilesetProvider
    ) : IMapManager
    {
        bool mapTextsAdded = false;
        bool texturesAdded = false;
        readonly Lazy<Dictionary<uint, Map>> maps = new(mapProvider);
        readonly Lazy<Dictionary<uint, TextList>> mapTexts = new(mapTextProvider);
        readonly Lazy<Dictionary<uint, Labdata>> labdata = new(labdataProvider);
        readonly Lazy<Textures> textures = new(texturesProvider);
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

        public IReadOnlyList<Labdata> Labdata
        {
            get
            {
                if (!texturesAdded)
                {
                    foreach (var labdata in labdata.Value.Values)
                    {
                        if (labdata.WallGraphics.Count == 0)
                        {
                            labdata.WallGraphics.AddRange(labdata.Walls.Select(wall => textures.Value.WallGraphics[(int)wall.TextureIndex - 1]));
                        }
                        if (labdata.ObjectGraphics.Count == 0)
                        {
                            foreach (var objectInfo in labdata.ObjectInfos)
                            {
                                if (objectInfo.NumAnimationFrames == 1)
                                {
                                    labdata.ObjectGraphics.Add(textures.Value.ObjectGraphics[(int)objectInfo.TextureIndex - 1]);
                                }
                                else
                                {
                                    var compoundGraphic = new Graphic((int)objectInfo.NumAnimationFrames * (int)objectInfo.TextureWidth,
                                        (int)objectInfo.TextureHeight, 0);

                                    for (uint i = 0; i < objectInfo.NumAnimationFrames; ++i)
                                    {
                                        var partialGraphic = textures.Value.ObjectGraphics[(int)objectInfo.TextureIndex - 1];

                                        compoundGraphic.AddOverlay(i * objectInfo.TextureWidth, 0u, partialGraphic, false);
                                    }

                                    labdata.ObjectGraphics.Add(compoundGraphic);
                                }
                            }
                        }

                        labdata.FloorGraphic = labdata.FloorTextureIndex == 0 ? null : textures.Value.FloorGraphics[labdata.FloorTextureIndex - 1];
                        labdata.CeilingGraphic = labdata.CeilingTextureIndex == 0 ? null : textures.Value.FloorGraphics[labdata.CeilingTextureIndex - 1];
                    }

                    texturesAdded = true;
                }

                return labdata.Value.Values.ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<Tileset> Tilesets => tilesets.Value.Values.ToList().AsReadOnly();
    }
}
