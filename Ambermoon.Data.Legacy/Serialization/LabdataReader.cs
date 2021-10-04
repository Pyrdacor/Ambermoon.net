using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class LabdataReader : ILabdataReader
    {
        public void ReadLabdata(Labdata labdata, IDataReader dataReader, IGameData gameData)
        {
            labdata.WallHeight = dataReader.ReadWord();
            labdata.CombatBackground = dataReader.ReadWord() & 0x0fu;
            labdata.CeilingColorIndex = dataReader.ReadByte();
            labdata.FloorColorIndex = dataReader.ReadByte();
            // Note: The ceiling texture index can be 0 in which case a sky is used.
            //       The sky is composed of a color gradient and a lab background
            //       which is given inside the map data.
            // To be more precisely if the texture index (ceiling and also floor) is
            // 0, the color index is used to draw instead. For example the town of
            // S'Angrila doesn't use a floor texture but only a color.
            uint ceilingTextureIndex = dataReader.ReadByte();
            uint floorTextureIndex = dataReader.ReadByte();

            labdata.Objects.Clear();
            int numObjects = dataReader.ReadWord();
            var objects = new List<Tuple<ushort, List<Tuple<short, short, short, int>>>>(numObjects);

            for (int i = 0; i < numObjects; ++i)
            {
                var obj = Tuple.Create(dataReader.ReadWord(), new List<Tuple<short, short, short, int>>(8));

                for (int n = 0; n < 8; ++n) // 8 sub entries (a map object can consist of up to 8 sub objects)
                {
                    obj.Item2.Add(Tuple.Create(
                        (short)dataReader.ReadWord(),
                        (short)dataReader.ReadWord(),
                        (short)dataReader.ReadWord(),
                        (int)dataReader.ReadWord()));
                }

                objects.Add(obj);
            }

            labdata.ObjectInfos.Clear();
            int numObjectInfos = dataReader.ReadWord();

            for (int i = 0; i < numObjectInfos; ++i)
            {
                var objectInfo = new Labdata.ObjectInfo
                {
                    Flags = (Tileset.TileFlags)dataReader.ReadDword(),
                    TextureIndex = dataReader.ReadWord(),
                    NumAnimationFrames = dataReader.ReadByte(),
                    ColorIndex = dataReader.ReadByte(),
                    TextureWidth = dataReader.ReadByte(),
                    TextureHeight = dataReader.ReadByte(),
                    MappedTextureWidth = dataReader.ReadWord(),
                    MappedTextureHeight = dataReader.ReadWord()
                };

                labdata.ObjectInfos.Add(objectInfo);
            }

            foreach (var obj in objects)
            {
                var subObjects = new List<Labdata.ObjectPosition>(8);

                foreach (var pos in obj.Item2)
                {
                    if (pos.Item4 != 0)
                    {
                        subObjects.Add(new Labdata.ObjectPosition
                        {
                            X = pos.Item1,
                            Y = pos.Item2,
                            Z = pos.Item3,
                            Object = labdata.ObjectInfos[pos.Item4 - 1]
                        });
                    }
                }

                labdata.Objects.Add(new Labdata.Object
                {
                    AutomapType = (AutomapType)obj.Item1,
                    SubObjects = subObjects
                });
            }

            labdata.Walls.Clear();
            int numWalls = dataReader.ReadWord();

            for (int i = 0; i < numWalls; ++i)
            {
                var wallData = new Labdata.WallData
                {
                    Flags = (Tileset.TileFlags)dataReader.ReadDword(),
                    TextureIndex = dataReader.ReadByte(),
                    AutomapType = (AutomapType)dataReader.ReadByte(),
                    ColorIndex = dataReader.ReadByte()
                };
                int numOverlays = dataReader.ReadByte();
                if (numOverlays != 0)
                {
                    wallData.Overlays = new Labdata.OverlayData[numOverlays];

                    for (int o = 0; o < numOverlays; ++o)
                    {
                        wallData.Overlays[o] = new Labdata.OverlayData
                        {
                            Blend = dataReader.ReadByte() != 0,
                            TextureIndex = dataReader.ReadByte(),
                            PositionX = dataReader.ReadByte(),
                            PositionY = dataReader.ReadByte(),
                            TextureWidth = dataReader.ReadByte(),
                            TextureHeight = dataReader.ReadByte()
                        };
                    }
                }
                labdata.Walls.Add(wallData);
            }

            // Load labyrinth graphics
            var graphicReader = new GraphicReader();
            if (floorTextureIndex != 0)
                labdata.FloorGraphic = ReadGraphic(graphicReader, gameData.Files["Floors.amb"].Files[(int)floorTextureIndex], 64, 64, false, false, true);
            if (ceilingTextureIndex != 0)
                labdata.CeilingGraphic = ReadGraphic(graphicReader, gameData.Files["Floors.amb"].Files[(int)ceilingTextureIndex], 64, 64, false, false, true);
            var objectTextureFiles = gameData.Files[$"2Object3D.amb"].Files;
            gameData.Files[$"3Object3D.amb"].Files.ToList().ForEach(f =>
            {
                if (!objectTextureFiles.ContainsKey(f.Key) || objectTextureFiles[f.Key].Size == 0)
                    objectTextureFiles[f.Key] = f.Value;
            });
            labdata.ObjectGraphics.Clear();
            foreach (var objectInfo in labdata.ObjectInfos)
            {
                if (objectInfo.NumAnimationFrames == 1)
                {
                    labdata.ObjectGraphics.Add(ReadGraphic(graphicReader, objectTextureFiles[(int)objectInfo.TextureIndex],
                        (int)objectInfo.TextureWidth, (int)objectInfo.TextureHeight, true, true, true));
                }
                else
                {
                    var compoundGraphic = new Graphic((int)objectInfo.NumAnimationFrames * (int)objectInfo.TextureWidth,
                        (int)objectInfo.TextureHeight, 0);

                    for (uint i = 0; i < objectInfo.NumAnimationFrames; ++i)
                    {
                        var partialGraphic = ReadGraphic(graphicReader, objectTextureFiles[(int)objectInfo.TextureIndex],
                            (int)objectInfo.TextureWidth, (int)objectInfo.TextureHeight, true, true, i == 0);

                        compoundGraphic.AddOverlay(i * objectInfo.TextureWidth, 0u, partialGraphic, false);
                    }

                    labdata.ObjectGraphics.Add(compoundGraphic);
                }
            }
            var wallTextureFiles = gameData.Files[$"2Wall3D.amb"].Files;
            var overlayTextureFiles = gameData.Files[$"2Overlay3D.amb"].Files;
            gameData.Files[$"3Wall3D.amb"].Files.ToList().ForEach(f =>
            {
                if (!wallTextureFiles.ContainsKey(f.Key) || wallTextureFiles[f.Key].Size == 0)
                    wallTextureFiles[f.Key] = f.Value;
            });
            gameData.Files[$"3Overlay3D.amb"].Files.ToList().ForEach(f =>
            {
                if (!overlayTextureFiles.ContainsKey(f.Key) || overlayTextureFiles[f.Key].Size == 0)
                    overlayTextureFiles[f.Key] = f.Value;
            });
            labdata.WallGraphics.Clear();
            int wallIndex = 0;
            foreach (var wall in labdata.Walls)
            {
                var wallGraphic = ReadGraphic(graphicReader, wallTextureFiles[(int)wall.TextureIndex],
                    128, 80, wall.Flags.HasFlag(Tileset.TileFlags.Transparency), true, true);

                labdata.WallGraphics.Add(wallGraphic);

                if (wall.Overlays != null && wall.Overlays.Length != 0)
                {
                    foreach (var overlay in wall.Overlays)
                    {
                        wallGraphic.AddOverlay(overlay.PositionX, overlay.PositionY, ReadGraphic(graphicReader,
                            overlayTextureFiles[(int)overlay.TextureIndex], (int)overlay.TextureWidth, (int)overlay.TextureHeight, true, true, true),
                            overlay.Blend);
                    }
                }

                ++wallIndex;
            }
        }

        static Graphic ReadGraphic(GraphicReader graphicReader, IDataReader file, int width, int height,
            bool alpha, bool texture, bool reset)
        {
            var graphic = new Graphic
            {
                Width = width,
                Height = height,
                IndexedGraphic = true
            };

            if (reset)
                file.Position = 0;

            graphicReader.ReadGraphic(graphic, file, new GraphicInfo
            {
                Width = width,
                Height = height,
                GraphicFormat = texture ? GraphicFormat.Texture4Bit : GraphicFormat.Palette4Bit,
                PaletteOffset = 0,
                Alpha = alpha
            });

            return graphic;
        }
    }
}
