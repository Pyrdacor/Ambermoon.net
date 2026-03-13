using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;
using static Ambermoon.Data.Labdata;
using static Ambermoon.Data.Tileset;
using Object = Ambermoon.Data.Labdata.Object;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class LabyrinthData : IFileSpec<LabyrinthData>, IFileSpec
{
    public static string Magic => "LAB";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Labdata? labdata = null;

    public Labdata Labdata => labdata!;

    public LabyrinthData()
    {

    }

    public LabyrinthData(Labdata labdata)
    {
        this.labdata = labdata;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        labdata = new();

        labdata.WallHeight = dataReader.ReadWord();
        labdata.CombatBackground = dataReader.ReadByte();
        labdata.CeilingColorIndex = dataReader.ReadByte();
        labdata.FloorColorIndex = dataReader.ReadByte();
        labdata.CeilingTextureIndex = dataReader.ReadByte();
        labdata.FloorTextureIndex = dataReader.ReadByte();

        int numWalls = dataReader.ReadByte();
        int numObjectData = dataReader.ReadByte();
        int numObjects = dataReader.ReadByte();

        for (int i = 0; i < numWalls; i++)
        {
            var wall = new WallData();

            wall.Flags = dataReader.ReadEnum32<TileFlags>();
            wall.TextureIndex = dataReader.ReadByte();

            if (dataReader.PeekByte() == 0xff)
            {
                dataReader.ReadByte();
                wall.AutomapType = AutomapType.Invalid;
            }
            else
            {
                wall.AutomapType = dataReader.ReadEnum8<AutomapType>();
            }

            wall.ColorIndex = dataReader.ReadByte();

            int overlayCount = dataReader.ReadByte();

            wall.Overlays = new OverlayData[overlayCount];

            for (int o = 0; o < overlayCount; o++)
            {
                var overlay = new OverlayData();

                overlay.Blend = dataReader.ReadBool();
                overlay.TextureIndex = dataReader.ReadByte();
                overlay.PositionX = dataReader.ReadByte();
                overlay.PositionY = dataReader.ReadByte();
                overlay.TextureWidth = dataReader.ReadByte();
                overlay.TextureHeight = dataReader.ReadByte();

                wall.Overlays[o] = overlay;
            }

            labdata.Walls.Add(wall);
        }

        for (int i  = 0; i < numObjectData; i++)
        {
            var objectData = new ObjectInfo();

            objectData.Flags = dataReader.ReadEnum32<TileFlags>();
            objectData.TextureIndex = dataReader.ReadWord();
            objectData.NumAnimationFrames = dataReader.ReadByte();
            objectData.ColorIndex = dataReader.ReadByte();
            objectData.TextureWidth = dataReader.ReadByte();
            objectData.TextureHeight = dataReader.ReadByte();
            objectData.MappedTextureWidth = dataReader.ReadWord();
            objectData.MappedTextureHeight = dataReader.ReadWord();

            labdata.ObjectInfos.Add(objectData);
        }

        for (int i = 0; i < numObjects; i++)
        {
            var obj = new Object();

            obj.AutomapType = dataReader.ReadEnum8<AutomapType>();

            int subObjCount = dataReader.ReadByte();

            obj.SubObjects = new(subObjCount);

            for (int s = 0; s < subObjCount; s++)
            {
                var subObject = new ObjectPosition();

                subObject.X = dataReader.ReadShort();
                subObject.Y = dataReader.ReadShort();
                subObject.Z = dataReader.ReadShort();
                subObject.Object = labdata.ObjectInfos[dataReader.ReadByte()];

                obj.SubObjects.Add(subObject);
            }

            labdata.Objects.Add(obj);
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((ushort)labdata!.WallHeight);
        dataWriter.Write((byte)labdata.CombatBackground);
        dataWriter.Write((byte)labdata.CeilingColorIndex);
        dataWriter.Write((byte)labdata.FloorColorIndex);
        dataWriter.Write((byte)labdata.CeilingTextureIndex);
        dataWriter.Write((byte)labdata.FloorTextureIndex);

        dataWriter.Write((byte)labdata.Walls.Count);
        dataWriter.Write((byte)labdata.ObjectInfos.Count);
        dataWriter.Write((byte)labdata.Objects.Count);

        foreach (var wall in labdata.Walls)
        {
            dataWriter.Write((uint)wall.Flags);
            dataWriter.Write((byte)wall.TextureIndex);
            dataWriter.WriteEnum8(wall.AutomapType);
            dataWriter.Write((byte)wall.ColorIndex);
            dataWriter.Write((byte)(wall.Overlays?.Length ?? 0));

            foreach (var overlay in wall.Overlays ?? [])
            {
                dataWriter.Write(overlay.Blend);
                dataWriter.Write((byte)overlay.TextureIndex);
                dataWriter.Write((byte)overlay.PositionX);
                dataWriter.Write((byte)overlay.PositionY);
                dataWriter.Write((byte)overlay.TextureWidth);
                dataWriter.Write((byte)overlay.TextureHeight);
            }
        }

        foreach (var objectData in labdata.ObjectInfos)
        {
            dataWriter.Write((uint)objectData.Flags);
            dataWriter.Write((ushort)objectData.TextureIndex);
            dataWriter.Write((byte)objectData.NumAnimationFrames);
            dataWriter.Write((byte)objectData.ColorIndex);
            dataWriter.Write((byte)objectData.TextureWidth);
            dataWriter.Write((byte)objectData.TextureHeight);
            dataWriter.Write((ushort)objectData.MappedTextureWidth);
            dataWriter.Write((ushort)objectData.MappedTextureHeight);
        }

        foreach (var obj in labdata.Objects)
        {
            if (obj.AutomapType == AutomapType.Invalid)
                dataWriter.Write((byte)0xff);
            else
                dataWriter.WriteEnum8(obj.AutomapType);

            var subObjects = obj.SubObjects.Where(s => s.Object.TextureWidth != 0).ToList();

            dataWriter.Write((byte)subObjects.Count);

            foreach (var subObject in subObjects)
            {
                dataWriter.WriteShort(subObject.X);
                dataWriter.WriteShort(subObject.Y);
                dataWriter.WriteShort(subObject.Z);
                dataWriter.Write((byte)labdata.ObjectInfos.IndexOf(subObject.Object));
            }
        }
    }
}
