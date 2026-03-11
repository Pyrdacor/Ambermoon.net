using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

[Flags]
internal enum WallDataFlags
{
    BlockSight = 0x1,
    Transparent = 0x2,
    AllowTravelType0 = 0x4,
    AllowTravelType1 = 0x8,
    AllowTravelType2 = 0x10,
    AllowTravelType3 = 0x20,
    AllowTravelType4 = 0x40,
    AllowTravelType5 = 0x80,
}

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
        int numObjects = dataReader.ReadByte();
        int numObjectData = dataReader.ReadByte();


    }

    public void Write(IDataWriter dataWriter)
    {
        throw new NotImplementedException();
    }
}
