using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class GraphicProvider : IGraphicProvider
    {
        struct GraphicFile
        {
            public string File;
            public int[] SubFiles; // null means all

            public GraphicFile(string file)
            {
                File = file;
                SubFiles = null;
            }

            public GraphicFile(string file, params int[] subFiles)
            {
                File = file;
                SubFiles = subFiles;
            }
        };

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
        static readonly Dictionary<GraphicType, GraphicFile> graphicFiles = new Dictionary<GraphicType, GraphicFile>
        {
            { GraphicType.Tileset1, new GraphicFile("1Icon_gfx.amb", 1) },
            { GraphicType.Tileset2, new GraphicFile("3Icon_gfx.amb", 2) },
            { GraphicType.Tileset3, new GraphicFile("2Icon_gfx.amb", 3) },
            { GraphicType.Tileset4, new GraphicFile("2Icon_gfx.amb", 4) },
            { GraphicType.Tileset5, new GraphicFile("2Icon_gfx.amb", 5) },
            { GraphicType.Tileset6, new GraphicFile("2Icon_gfx.amb", 6) },
            { GraphicType.Tileset7, new GraphicFile("2Icon_gfx.amb", 7) },
            { GraphicType.Tileset8, new GraphicFile("3Icon_gfx.amb", 8) },
            { GraphicType.Player, new GraphicFile("Party_gfx.amb") },
            { GraphicType.Portrait, new GraphicFile("Portraits.amb") },
            { GraphicType.Item, new GraphicFile("Object_icons") },
            { GraphicType.Layout, new GraphicFile("Layouts.amb") }
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
                var containerFile = gameData.Files[graphicFiles[type].File];

                void LoadGraphic(IDataReader graphicDataReader)
                {
                    graphicDataReader.Position = 0;
                    int end = graphicDataReader.Size - info.DataSize;
                    while (graphicDataReader.Position <= end)
                    {
                        var graphic = new Graphic();
                        reader.ReadGraphic(graphic, graphicDataReader, info);
                        graphicList.Add(graphic);
                    }
                }

                if (graphicFiles[type].SubFiles == null)
                {
                    foreach (var graphicFile in containerFile.Files)
                    {
                        LoadGraphic(graphicFile.Value);
                    }
                }
                else
                {
                    foreach (var file in graphicFiles[type].SubFiles)
                    {
                        LoadGraphic(containerFile.Files[file]);
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
                    info.Alpha = true;
                    break;
                case GraphicType.Tileset3:
                    info.Palette = palettes[3];
                    info.Alpha = true;
                    break;
                case GraphicType.Tileset4:
                case GraphicType.Tileset5:
                case GraphicType.Tileset6:
                case GraphicType.Tileset7:
                    info.Palette = palettes[7];
                    info.Alpha = true;
                    break;
                case GraphicType.Tileset8:
                    info.Palette = palettes[10];
                    info.Alpha = true;
                    break;
                case GraphicType.Player:
                    info.Width = 16;
                    info.Height = 32;
                    info.Palette = palettes[7];
                    info.Alpha = true;
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
                case GraphicType.Layout:
                    info.Width = 320;
                    info.Height = 163;
                    info.GraphicFormat = GraphicFormat.Palette3Bit;
                    info.PaletteOffset = 24;
                    info.Palette = palettes[1];
                    info.Alpha = true;
                    break;
                // TODO
            }

            return info;
        }
    }
}
