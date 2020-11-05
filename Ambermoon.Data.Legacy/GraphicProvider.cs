using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
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
            public int FileIndexOffset;

            public GraphicFile(string file, int fileIndexOffset = 0)
            {
                File = file;
                SubFiles = null;
                FileIndexOffset = fileIndexOffset;
            }

            public GraphicFile(string file, int fileIndexOffset, params int[] subFiles)
            {
                File = file;
                SubFiles = subFiles;
                FileIndexOffset = fileIndexOffset;
            }
        };

        readonly GameData gameData;
        readonly ExecutableData.ExecutableData executableData;
        public Dictionary<int, Graphic> Palettes { get; }

        public GraphicProvider(GameData gameData, ExecutableData.ExecutableData executableData)
        {
            this.gameData = gameData;
            this.executableData = executableData;
            var graphicReader = new GraphicReader();
            Palettes = gameData.Files[paletteFile].Files.ToDictionary(f => f.Key, f => ReadPalette(graphicReader, f.Value));

            // There is a special palette used for items and portraits.
            Palettes.Add(50, new Graphic
            {
                Width = 32,
                Height = 1,
                IndexedGraphic = false,
                Data = new byte[]
                {
                    /*0x00, 0x00, 0x00, 0xff, 0xed, 0xdc, 0xcb, 0xff, 0xfe, 0xfe, 0xed, 0xff, 0xba, 0xba, 0xcb, 0xff,
                    0x87, 0x98, 0xa9, 0xff, 0x54, 0x76, 0x87, 0xff, 0x21, 0x54, 0x65, 0xff, 0x00, 0x32, 0x43, 0xff,
                    0xfe, 0xcb, 0x98, 0xff, 0xed, 0xa9, 0x76, 0xff, 0xcb, 0x87, 0x54, 0xff, 0xa9, 0x65, 0x32, 0xff,
                    0x87, 0x43, 0x21, 0xff, 0x54, 0x21, 0x10, 0xff, 0xba, 0x87, 0x00, 0xff, 0xdc, 0xa9, 0x00, 0xff,
                    0xfe, 0xcb, 0x00, 0xff, 0xfe, 0x98, 0x00, 0xff, 0xcb, 0x65, 0x00, 0xff, 0x87, 0x10, 0x21, 0xff,
                    0xcb, 0x43, 0x32, 0xff, 0xed, 0x65, 0x32, 0xff, 0xa9, 0xa9, 0x43, 0xff, 0x54, 0x76, 0x32, 0xff,
                    0x21, 0x54, 0x43, 0xff, 0x00, 0x10, 0x00, 0xff, 0x21, 0x21, 0x21, 0xff, 0x43, 0x43, 0x32, 0xff,
                    0x65, 0x65, 0x54, 0xff, 0x87, 0x87, 0x76, 0xff, 0xa9, 0xa9, 0x98, 0xff, 0xcb, 0xcb, 0xba, 0xff*/
                    0x00, 0x00, 0x00, 0xff, 0xEE, 0xDD, 0xCC, 0xff, 0xFF, 0xFF, 0xEE, 0xff, 0xBB, 0xBB, 0xCC, 0xff,
                    0x88, 0x99, 0xAA, 0xff, 0x55, 0x77, 0x88, 0xff, 0x22, 0x55, 0x66, 0xff, 0x00, 0x33, 0x44, 0xff,
                    0xFF, 0xCC, 0x99, 0xff, 0xEE, 0xAA, 0x77, 0xff, 0xCC, 0x88, 0x55, 0xff, 0xAA, 0x66, 0x33, 0xff,
                    0x88, 0x44, 0x22, 0xff, 0x55, 0x22, 0x11, 0xff, 0xBB, 0x88, 0x00, 0xff, 0xDD, 0xAA, 0x00, 0xff,

                    0xFF, 0xCC, 0x00, 0xff, 0xFF, 0x99, 0x00, 0xff, 0xCC, 0x66, 0x00, 0xff, 0x88, 0x11, 0x22, 0xff,
                    0xCC, 0x44, 0x33, 0xff, 0xEE, 0x66, 0x33, 0xff, 0xAA, 0xAA, 0x44, 0xff, 0x55, 0x77, 0x33, 0xff,
                    /*0x44, 0x99, 0xdd, 0xff, 0x22, 0x55, 0x99, 0xff, 0x22, 0x11, 0x00, 0xff, 0x44, 0x33, 0x11, 0xff,
                    0x66, 0x55, 0x33, 0xff, 0x88, 0x77, 0x55, 0xff, 0xaa, 0x99, 0x77, 0xff, 0xCC, 0xbb, 0x99, 0xff*/
                    0x22, 0x55, 0x44, 0xff, 0x55, 0x00, 0x99, 0xff, 0x22, 0x22, 0x22, 0xff, 0x44, 0x44, 0x33, 0xff,
                    0x66, 0x66, 0x55, 0xff, 0x88, 0x88, 0x77, 0xff, 0xAA, 0xAA, 0x99, 0xff, 0xCC, 0xCC, 0xBB, 0xff
                }
            });
            // We also use another palette for text rendering. It has transparent, black, white, gray, red, yellow etc.
            // Note: If a text is hovered (e.g. when choosing something to say) the color is inverted (e.g. yellow
            // background and text is just transparent to show the gray background).
            // We also add some other colors like the blue gradient for portrait backgrounds.
            Palettes.Add(51, new Graphic
            {
                Width = 32,
                Height = 1,
                IndexedGraphic = false,
                Data = new byte[]
                {
                    // transparent, black (shadow), white, gray
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xee, 0xff, 0xcc, 0xcc, 0xbb, 0xff,
                    // red (character), yellow (active character), orange (battle text), blueish gray (dead character)
                    0xcc, 0x44, 0x33, 0xff, 0xff, 0xcc, 0x00, 0xff, 0xdd, 0x66, 0x33, 0xff, 0x99, 0xaa, 0xaa, 0xff,
                    // green (stat UI headers), rest unused
                    0xaa, 0xaa, 0x44, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                }
            });
            // And another palette for some UI graphics.
            // The portraits have a blue gradient as background. It is also 32x34 pixels in size and the gradient
            // is in y-direction. All colors have R=0x00 and G=0x11. The blue component is increased by 0x11
            // every 2 pixels starting at y=4 (first 4 pixel rows have B=0x00, next 2 have B=0x11, etc).
            // Last 2 rows have B=0xff.
            Palettes.Add(52, new Graphic
            {
                Width = 32,
                Height = 1,
                IndexedGraphic = false,
                Data = new byte[]
                {
                    // NOTE: Color 25 (0-based) is treated as the color key (tansparent color) index so don't use it
                    // as a normal color!
                    // First 16 colors are unused for now
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // 16 colors for the blue background gradient of portraits
                    0x00, 0x11, 0x00, 0xff, 0x00, 0x11, 0x11, 0xff, 0x00, 0x11, 0x22, 0xff, 0x00, 0x11, 0x33, 0xff,
                    0x00, 0x11, 0x44, 0xff, 0x00, 0x11, 0x55, 0xff, 0x00, 0x11, 0x66, 0xff, 0x00, 0x11, 0x77, 0xff,
                    0x00, 0x11, 0x88, 0xff, 0x00, 0x11, 0x99, 0xff, 0x00, 0x11, 0xaa, 0xff, 0x00, 0x11, 0xbb, 0xff,
                    0x00, 0x11, 0xcc, 0xff, 0x00, 0x11, 0xdd, 0xff, 0x00, 0x11, 0xee, 0xff, 0x00, 0x11, 0xff, 0xff,
                    // some UI colors (TODO: character with ailment?)
                    0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00, 0x00, 0x66, 0x66, 0x55, 0xff, 0x44, 0x44, 0x33, 0xff,
                    0x22, 0x22, 0x22, 0xff, 0x88, 0x88, 0x77, 0xff, 0xAA, 0xAA, 0x99, 0xff, 0xCC, 0xCC, 0xBB, 0xff
                }
            });

            // TODO: The following bytes were extracted from AM2_CPU (behind cursors).
            // These are 3 palettes.

            // The first one is the one for portraits and items. One of the others might be for texts.
            /* 00 00 0E DC 0F FE 0B BC 08 9A 05 78 02 56 00 34 0F C9 0E A7 0C 85 0A 63 08 42 05 21 0B 80 0D A0
             * 0F C0 0F 90 0C 60 08 12 0C 43 0E 63 0A A4 05 73 02 54 05 09 02 22 04 43 06 65 08 87 0A A9 0C CB
             * 
             * 00 00 0F DC 0E B9 0C 96 04 10 08 51 0A 74 06 30 0F 60 0D 30 09 00 0F C0 0B 90 08 60 09 A3 05 70
             * 02 40 08 BE 03 8C 00 48 0C 8D 0A 69 08 36 0F FF 07 9D 01 6A 02 11 04 32 06 54 08 76 0A 98 0C BA
             * 
             * 00 00 0E DC 0F FE 0B BC 08 9A 05 78 02 56 00 34 0F C9 0E A7 0C 85 0A 63 08 42 05 21 0B 80 0D A0
             * 0F C0 0F 90 0C 60 08 12 0C 43 0E 63 0A A4 05 73 04 9D 02 59 02 10 04 31 06 53 08 75 0A 97 0C B9
             */

            foreach (var type in Enum.GetValues<GraphicType>())
            {
                if (type == GraphicType.Cursor)
                {
                    var cursorGraphics = graphics[GraphicType.Cursor] = new List<Graphic>();

                    foreach (var cursor in executableData.Cursors.Entries)
                        cursorGraphics.Add(cursor.Graphic);
                }
                else if (type == GraphicType.UIElements)
                {
                    graphics[type] = UIElementProvider.Create();
                    graphics[type].AddRange(executableData.UIGraphics.Entries.Values);
                    graphics[type].AddRange(executableData.Buttons.Entries.Values);
                }
                else if (type == GraphicType.TravelGfx)
                {
                    graphics[type] = gameData.TravelGraphics;
                }
                else if (type == GraphicType.Transports)
                {
                    var reader = gameData.Files["Stationary"].Files[1];
                    graphics[type] = gameData.StationaryImageInfos.Select(info =>
                    {
                        var graphic = new Graphic();
                        graphicReader.ReadGraphic(graphic, reader, info.Value);
                        return graphic;
                    }).ToList();
                }
                else if (type == GraphicType.NPC)
                {
                    var npcGraphics = new List<Graphic>(34);
                    var graphicInfo = new GraphicInfo
                    {
                        Width = 16,
                        Height = 32,
                        GraphicFormat = GraphicFormat.Palette5Bit,
                        Alpha = true,
                        PaletteOffset = 0
                    };
                    var graphic = new Graphic();
                    foreach (var file in gameData.Files["NPC_gfx.amb"].Files)
                    {
                        var reader = file.Value;

                        while (reader.Position <= reader.Size - graphicInfo.DataSize)
                        {
                            int numFrames = reader.ReadByte();
                            reader.AlignToWord();
                            var compoundGraphic = new Graphic(16 * numFrames, 32, 0);

                            for (int i = 0; i < numFrames; ++i)
                            {
                                graphicReader.ReadGraphic(graphic, reader, graphicInfo);
                                compoundGraphic.AddOverlay((uint)i * 16, 0, graphic);
                            }

                            npcGraphics.Add(compoundGraphic);
                        }
                    }

                    graphics[type] = npcGraphics;
                }
                else if (type >= GraphicType.MonsterTiny && type <= GraphicType.MonsterBig)
                {
                    // We process all sized monster graphics in one go.
                    // They use all the same graphics but scaled versions of them.
                    if (type == GraphicType.MonsterNormal)
                    {
                        var graphicInfo = new GraphicInfo
                        {
                            GraphicFormat = GraphicFormat.Palette5Bit,
                            Alpha = true,
                            PaletteOffset = 0
                        };
                        var graphic = new Graphic();
                        var monsterGraphics = new List<Graphic>();
                        foreach (var file in gameData.Files["Monster_gfx.amb"].Files)
                        {
                            var reader = file.Value;
                            var info = GetMonsterGraphicInfo((MonsterGraphicIndex)file.Key);
                            graphicInfo.Width = (int)info.Width;
                            graphicInfo.Height = (int)info.Height;
                            int sizePerFrame = (graphicInfo.Width * graphicInfo.Height * 5 + 7) / 8;
                            int numFrames = reader.Size / sizePerFrame;
                            var compoundGraphic = new Graphic(numFrames * graphicInfo.Width, graphicInfo.Height, 0);

                            for (int i = 0; i < numFrames; ++i)
                            {
                                graphicReader.ReadGraphic(graphic, reader, graphicInfo);
                                compoundGraphic.AddOverlay((uint)i * info.Width, 0, graphic);
                            }

                            monsterGraphics.Add(compoundGraphic);
                        }
                        graphics[GraphicType.MonsterNormal] = monsterGraphics;
                        graphics[GraphicType.MonsterTiny] = monsterGraphics.Select(g => g.CreateScaled(GetMonsterRowImageScaleFactor(MonsterRow.Farthest))).ToList();
                        graphics[GraphicType.MonsterSmall] = monsterGraphics.Select(g => g.CreateScaled(GetMonsterRowImageScaleFactor(MonsterRow.Far))).ToList();
                        graphics[GraphicType.MonsterBig] = monsterGraphics.Select(g => g.CreateScaled(GetMonsterRowImageScaleFactor(MonsterRow.Near))).ToList();
                    }
                }
                else
                {
                    LoadGraphics(type);
                }
            }
        }

        Graphic ReadPalette(GraphicReader graphicReader, IDataReader reader)
        {
            reader.Position = 0;
            var paletteGraphic = new Graphic();
            graphicReader.ReadGraphic(paletteGraphic, reader, paletteGraphicInfo);
            return paletteGraphic;
        }

        static GraphicInfo paletteGraphicInfo = new GraphicInfo
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };
        static readonly string paletteFile = "Palettes.amb";
        static readonly Dictionary<GraphicType, GraphicFile[]> graphicFiles = new Dictionary<GraphicType, GraphicFile[]>();
        readonly Dictionary<GraphicType, List<Graphic>> graphics = new Dictionary<GraphicType, List<Graphic>>();

        static void AddGraphicFiles(GraphicType type, params GraphicFile[] files)
        {
            graphicFiles.Add(type, files);
        }

        static GraphicProvider()
        {
            AddGraphicFiles(GraphicType.Tileset1, new GraphicFile("1Icon_gfx.amb", 0, 1));
            AddGraphicFiles(GraphicType.Tileset2, new GraphicFile("3Icon_gfx.amb", 0, 2));
            AddGraphicFiles(GraphicType.Tileset3, new GraphicFile("2Icon_gfx.amb", 0, 3));
            AddGraphicFiles(GraphicType.Tileset4, new GraphicFile("2Icon_gfx.amb", 0, 4));
            AddGraphicFiles(GraphicType.Tileset5, new GraphicFile("2Icon_gfx.amb", 0, 5));
            AddGraphicFiles(GraphicType.Tileset6, new GraphicFile("2Icon_gfx.amb", 0, 6));
            AddGraphicFiles(GraphicType.Tileset7, new GraphicFile("2Icon_gfx.amb", 0, 7));
            AddGraphicFiles(GraphicType.Tileset8, new GraphicFile("3Icon_gfx.amb", 0, 8));
            AddGraphicFiles(GraphicType.Player, new GraphicFile("Party_gfx.amb"));
            AddGraphicFiles(GraphicType.Portrait, new GraphicFile("Portraits.amb"));
            AddGraphicFiles(GraphicType.Item, new GraphicFile("Object_icons"));
            AddGraphicFiles(GraphicType.Layout, new GraphicFile("Layouts.amb"));
            AddGraphicFiles(GraphicType.LabBackground, new GraphicFile("Lab_background.amb"));
            AddGraphicFiles(GraphicType.Pics80x80, new GraphicFile("Pics_80x80.amb"));
            AddGraphicFiles(GraphicType.EventPictures, new GraphicFile("Event_pix.amb"));
            AddGraphicFiles(GraphicType.CombatBackground, new GraphicFile("Combat_background.amb"));
        }

        public List<Graphic> GetGraphics(GraphicType type)
        {
            return graphics[type];
        }

        void LoadGraphics(GraphicType type)
        {
            if (!graphics.ContainsKey(type))
            {
                graphics.Add(type, new List<Graphic>());
                var reader = new GraphicReader();
                var info = GraphicInfoFromType(type);
                var graphicList = graphics[type];

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

                var allFiles = new SortedDictionary<int, IDataReader>();

                foreach (var graphicFile in graphicFiles[type])
                {
                    var containerFile = gameData.Files[graphicFile.File];

                    if (graphicFile.SubFiles == null)
                    {
                        foreach (var file in containerFile.Files)
                        {
                            allFiles[graphicFile.FileIndexOffset + file.Key] = file.Value;
                        }
                    }
                    else
                    {
                        foreach (var file in graphicFile.SubFiles)
                        {
                            allFiles[graphicFile.FileIndexOffset + file] = containerFile.Files[file];
                        }
                    }
                }

                foreach (var file in allFiles)
                {
                    LoadGraphic(file.Value);
                }
            }
        }

        GraphicInfo GraphicInfoFromType(GraphicType type)
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
                case GraphicType.Tileset3:
                case GraphicType.Tileset4:
                case GraphicType.Tileset5:
                case GraphicType.Tileset6:
                case GraphicType.Tileset7:
                case GraphicType.Tileset8:
                    info.Alpha = true;
                    break;
                case GraphicType.Player:
                    info.Width = 16;
                    info.Height = 32;
                    info.Alpha = true;
                    break;
                case GraphicType.Portrait:
                    info.Width = 32;
                    info.Height = 34;
                    info.Alpha = false;
                    break;
                case GraphicType.Item:
                    info.Width = 16;
                    info.Height = 16;
                    info.Alpha = true;
                    break;
                case GraphicType.Layout:
                    info.Width = 320;
                    info.Height = 163;
                    info.GraphicFormat = GraphicFormat.Palette3Bit;
                    info.PaletteOffset = 24;
                    info.Alpha = true;
                    break;
                case GraphicType.LabBackground:
                    info.Width = 144;
                    info.Height = 20;
                    info.GraphicFormat = GraphicFormat.Palette4Bit;
                    info.PaletteOffset = 0;
                    info.Alpha = false;
                    break;
                case GraphicType.Pics80x80:
                    info.Width = 80;
                    info.Height = 80;
                    info.GraphicFormat = GraphicFormat.Palette5Bit;
                    info.PaletteOffset = 0;
                    info.Alpha = false;
                    break;
                case GraphicType.EventPictures:
                    info.Width = 320;
                    info.Height = 92;
                    info.GraphicFormat = GraphicFormat.Palette5Bit;
                    info.PaletteOffset = 0;
                    info.Alpha = false;
                    break;
                case GraphicType.CombatBackground:
                    info.Width = 320;
                    info.Height = 95;
                    info.GraphicFormat = GraphicFormat.Palette5Bit;
                    info.PaletteOffset = 0;
                    info.Alpha = false;
                    break;
                // TODO
            }

            return info;
        }

        public CombatBackgroundInfo Get2DCombatBackground(uint index) => CombatBackgrounds.Info2D[index];
        public CombatBackgroundInfo Get3DCombatBackground(uint index) => CombatBackgrounds.Info3D[index];
        public CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index) => CombatGraphics.Info[index];
        public MonsterGraphicInfo GetMonsterGraphicInfo(MonsterGraphicIndex index) => MonsterGraphics.Info[index];
        public float GetMonsterRowImageScaleFactor(MonsterRow row) => row switch
        {
            MonsterRow.Farthest => 0.667f,
            MonsterRow.Far => 0.8f,
            MonsterRow.Near => 1.5f,
            _ => 1.0f,
        };
    }
}
