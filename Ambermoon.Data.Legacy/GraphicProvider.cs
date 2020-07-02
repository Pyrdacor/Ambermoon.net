using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class GraphicProvider : IGraphicProvider
    {
        readonly GameData gameData;

        public GraphicProvider(GameData gameData)
        {
            this.gameData = gameData;
            var palettes = gameData.Files[paletteFile].Files.ToDictionary(f => f.Key, f => ReadPalette(f.Value));

            foreach (GraphicType type in Enum.GetValues(typeof(GraphicType)))
            {
                LoadGraphics(type, palettes);
            }
        }

        Palette ReadPalette(IDataReader reader)
        {
            var paletteGraphic = new Graphic();
            new GraphicReader().ReadGraphic(paletteGraphic, reader, paletteGraphicInfo);
            return new Palette(paletteGraphic);
        }

        static GraphicInfo paletteGraphicInfo = new GraphicInfo
        {
            Width = 32, Height = 1, GraphicFormat = GraphicFormat.XRGB16
        };
        static readonly string paletteFile = "Palettes.amb";
        static readonly Dictionary<GraphicType, string[]> graphicFiles = new Dictionary<GraphicType, string[]>
        {
            { GraphicType.Tileset1, new string [] { "1Icon_gfx.amb" } },
            { GraphicType.Tileset2, new string [] { "3Icon_gfx.amb" } },
            { GraphicType.Tileset3, new string [] { "2Icon_gfx.amb" } },
            { GraphicType.Tileset4, new string [] { "2Icon_gfx.amb" } },
            { GraphicType.Tileset5, new string [] { "2Icon_gfx.amb" } },
            { GraphicType.Tileset6, new string [] { "2Icon_gfx.amb" } },
            { GraphicType.Tileset7, new string [] { "2Icon_gfx.amb" } },
            { GraphicType.Tileset8, new string [] { "3Icon_gfx.amb" } },
            { GraphicType.Player, new string [] { "Party_gfx.amb" } },
            { GraphicType.Portrait, new string [] { "Portraits.amb" } },
            { GraphicType.Item, new string [] { "Object_icons" } },
        };
        readonly Dictionary<GraphicType, List<Graphic>> graphics = new Dictionary<GraphicType, List<Graphic>>();

        public List<Graphic> GetGraphics(GraphicType type)
        {
            return graphics[type];
        }

        void LoadGraphics(GraphicType type, Dictionary<int, Palette> palettes)
        {
            if (!graphics.ContainsKey(type))
            {
                graphics.Add(type, new List<Graphic>());
                var reader = new GraphicReader();
                var info = GraphicInfoFromType(type, palettes);
                var graphicList = graphics[type];

                foreach (var file in graphicFiles[type])
                {
                    foreach (var graphicFile in gameData.Files[file].Files)
                    {
                        int end = graphicFile.Value.Size - info.DataSize;
                        while (graphicFile.Value.Position <= end)
                        {
                            var graphic = new Graphic();
                            reader.ReadGraphic(graphic, graphicFile.Value, info);
                            graphicList.Add(graphic);
                        }
                    }
                }
            }
        }

        GraphicInfo GraphicInfoFromType(GraphicType type, Dictionary<int, Palette> palettes)
        {
            var info = new GraphicInfo
            {
                Width = 16,
                Height = 16,
                GraphicFormat = GraphicFormat.Palette5Bit
            };

            switch (type)
            {
                case GraphicType.Tileset1:
                case GraphicType.Tileset2:
                    info.Palette = palettes[1];
                    break;
                case GraphicType.Tileset3:
                    info.Palette = palettes[3];
                    break;
                case GraphicType.Tileset4:
                case GraphicType.Tileset5:
                case GraphicType.Tileset6:
                case GraphicType.Tileset7:
                    info.Palette = palettes[7];
                    break;
                case GraphicType.Tileset8:
                    info.Palette = palettes[10];
                    break;
                case GraphicType.Player:
                    info.Width = 16;
                    info.Height = 32;
                    info.Palette = palettes[7];
                    break;
                case GraphicType.Portrait:
                    info.Width = 32;
                    info.Height = 32;
                    info.Palette = palettes[1]; // TODO
                    break;
                case GraphicType.Item:
                    info.Width = 16;
                    info.Height = 16;
                    info.Palette = palettes[33]; // TODO
                    break;
                // TODO
            }

            return info;
        }
    }
}
