using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class LabdataWriter
    {
        public static void WriteLabdata(Labdata labdata, IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)labdata.WallHeight);
            dataWriter.Write(labdata.Flags);
            dataWriter.Write(labdata.CeilingColorIndex);
            dataWriter.Write(labdata.FloorColorIndex);
            dataWriter.Write(labdata.CeilingTextureIndex);
            dataWriter.Write(labdata.FloorTextureIndex);

            // Objects
            dataWriter.Write((ushort)labdata.Objects.Count);

            foreach (var obj in labdata.Objects)
            {
                dataWriter.Write((ushort)obj.AutomapType);

                for (int i = 0; i < 8; ++i)
                {
                    if (obj.SubObjects == null || i >= obj.SubObjects.Count)
                    {
                        dataWriter.Write(0u);
                        dataWriter.Write(0u);
                    }
                    else
                    {
                        var subObject = obj.SubObjects[i];
                        unchecked
                        {
                            dataWriter.Write((ushort)subObject.X);
                            dataWriter.Write((ushort)subObject.Y);
                            dataWriter.Write((ushort)subObject.Z);
                            dataWriter.Write((ushort)(1 + labdata.ObjectInfos.IndexOf(subObject.Object)));
                        }
                    }
                }
            }

            // Object infos
            dataWriter.Write((ushort)labdata.ObjectInfos.Count);

            foreach (var objectInfo in labdata.ObjectInfos)
            {
                dataWriter.Write((uint)objectInfo.Flags);
                dataWriter.Write((ushort)objectInfo.TextureIndex);
                dataWriter.Write((byte)objectInfo.NumAnimationFrames);
                dataWriter.Write(objectInfo.ColorIndex);
                dataWriter.Write((byte)objectInfo.TextureWidth);
                dataWriter.Write((byte)objectInfo.TextureHeight);
                dataWriter.Write((ushort)objectInfo.MappedTextureWidth);
                dataWriter.Write((ushort)objectInfo.MappedTextureHeight);
            }

            // Walls
            dataWriter.Write((ushort)labdata.Walls.Count);

            foreach (var wall in labdata.Walls)
            {
                dataWriter.Write((uint)wall.Flags);
                dataWriter.Write((byte)wall.TextureIndex);
                dataWriter.Write((byte)wall.AutomapType);
                dataWriter.Write(wall.ColorIndex);

                var overlays = wall.Overlays ?? new Labdata.OverlayData[0];
                dataWriter.Write((byte)overlays.Length);

                foreach (var overlay in overlays)
                {
                    dataWriter.Write((byte)(overlay.Blend ? 1 : 0));
                    dataWriter.Write((byte)overlay.TextureIndex);
                    dataWriter.Write((byte)overlay.PositionX);
                    dataWriter.Write((byte)overlay.PositionY);
                    dataWriter.Write((byte)overlay.TextureWidth);
                    dataWriter.Write((byte)overlay.TextureHeight);
                }
            }
        }
    }
}
