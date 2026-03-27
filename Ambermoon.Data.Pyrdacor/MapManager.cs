using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;
using static Ambermoon.Data.Labdata;

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

        public Tileset? GetTilesetForMap(Map map) => map.Type == MapType.Map3D ? null : tilesets.Value!.GetValueOrDefault(map.TilesetOrLabdataIndex, null);

        public Labdata? GetLabdataForMap(Map map) => map.Type == MapType.Map2D ? null : EnsureGraphicsIfNotNull(labdata.Value!.GetValueOrDefault(map.TilesetOrLabdataIndex, null));

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

        private Labdata EnsureGraphics(Labdata labdata) => EnsureGraphicsIfNotNull(labdata)!;

        private Labdata? EnsureGraphicsIfNotNull(Labdata? labdata)
        {
            if (labdata == null)
                return null;

            if (labdata.WallGraphics.Count == 0)
            {
                labdata.WallGraphics.AddRange(labdata.Walls.Select(wall =>
                {
                    var wallGraphic = textures.Value.WallGraphics[(int)wall.TextureIndex - 1].Clone();

                    if (wall.Overlays != null && wall.Overlays.Length != 0)
                    {
                        foreach (var overlay in wall.Overlays)
                        {
                            wallGraphic.AddOverlay(overlay.PositionX, overlay.PositionY,
                                textures.Value.OverlayGraphics[(int)overlay.TextureIndex - 1],
                                overlay.Blend);
                        }
                    }

                    return wallGraphic;
                }));
            }

            if (labdata.ObjectGraphics.Count == 0)
            {
                labdata.ObjectGraphics.AddRange(labdata.ObjectInfos.Select(objectInfo => textures.Value.ObjectGraphics[(int)objectInfo.TextureIndex - 1]));
            }

            labdata.FloorGraphic ??= labdata.FloorTextureIndex == 0 ? null : textures.Value.FloorGraphics[labdata.FloorTextureIndex - 1];
            labdata.CeilingGraphic ??= labdata.CeilingTextureIndex == 0 ? null : textures.Value.FloorGraphics[labdata.CeilingTextureIndex - 1];

            return labdata;
        }

        public IReadOnlyList<Labdata> Labdata => labdata.Value.Values.Select(EnsureGraphics).ToList().AsReadOnly();

        public IReadOnlyList<Tileset> Tilesets => tilesets.Value.Values.ToList().AsReadOnly();
    }
}
