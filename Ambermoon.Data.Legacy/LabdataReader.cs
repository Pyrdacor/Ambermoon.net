using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class LabdataReader : ILabdataReader
    {
        public void ReadLabdata(Labdata labdata, IDataReader dataReader)
        {
            dataReader.ReadBytes(8); // Unknown

            labdata.Objects.Clear();
            int numObjects = dataReader.ReadWord();
            var objects = new List<Tuple<ushort, List<Tuple<float, float, float, int>>>>(numObjects);

            for (int i = 0; i < numObjects; ++i)
            {
                var obj = Tuple.Create(dataReader.ReadWord(), new List<Tuple<float, float, float, int>>(8));

                for (int n = 0; n < 8; ++n) // 8 sub entries (a map object can consist of up to 8 sub objects)
                {
                    obj.Item2.Add(Tuple.Create(
                        (float)(short)dataReader.ReadWord(),
                        (float)(short)dataReader.ReadWord(),
                        (float)(short)dataReader.ReadWord(),
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
                    Type = dataReader.ReadByte(),
                    Collision = dataReader.ReadBytes(3),
                    TextureIndex = dataReader.ReadWord(),
                    NumAnimationFrames = dataReader.ReadByte(),
                    Unknown = dataReader.ReadByte(),
                    TextureWidth = dataReader.ReadByte(),
                    TextureHeight = dataReader.ReadByte(),
                    MappedTextureWidth = dataReader.ReadWord(),
                    MappedTextureHeight = dataReader.ReadWord()
                };

                labdata.ObjectInfos.Add(objectInfo);

                Console.WriteLine($"Type: {objectInfo.Type}, Texture: {objectInfo.TextureIndex},{objectInfo.TextureWidth}x{objectInfo.TextureHeight} -> {objectInfo.MappedTextureWidth}x{objectInfo.MappedTextureHeight}, Frames: {objectInfo.NumAnimationFrames}");
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
                    Header = obj.Item1,
                    SubObjects = subObjects
                });
            }

            int numWalls = dataReader.ReadWord();
            Console.WriteLine();
            Console.WriteLine("NUM WALLS: " + numWalls);

            for (int i = 0; i < numWalls; ++i)
            {
                var wallData = new Labdata.WallData
                {
                    Unknown1 = dataReader.ReadBytes(3),
                    Flags = (Labdata.WallFlags)dataReader.ReadByte(),
                    TextureIndex = dataReader.ReadByte(),
                    Unknown2 = dataReader.ReadBytes(2)
                };
                int numOverlays = dataReader.ReadByte();
                if (numOverlays != 0)
                {
                    wallData.Overlays = new Labdata.OverlayData[numOverlays];

                    for (int o = 0; o < numOverlays; ++o)
                    {
                        wallData.Overlays[o] = new Labdata.OverlayData
                        {
                            NumAnimationFrames = dataReader.ReadByte(),
                            TextureIndex = dataReader.ReadByte(),
                            PositionX = dataReader.ReadByte(),
                            PositionY = dataReader.ReadByte(),
                            TextureWidth = dataReader.ReadByte(),
                            TextureHeight = dataReader.ReadByte()
                        };
                    }
                }

                Console.WriteLine($"Wall{i+1} -> {wallData}");
            }

            Console.WriteLine("Remaining bytes: " + (dataReader.Size - dataReader.Position));
            Console.WriteLine(string.Join(" ", dataReader.ReadToEnd().Select(b => b.ToString("x2"))));
        }
    }
}
