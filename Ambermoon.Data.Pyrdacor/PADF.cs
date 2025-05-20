using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor;

/// <summary>
/// Pyrdacor's Ambermoon Data File
/// </summary>
internal static class PADF
{
    public const string Header = "PADF";

    internal static readonly Dictionary<string, Func<IFileSpec>> FileSpecs = [];
    internal static readonly Dictionary<ushort, ICompression> Compressions = [];

    static PADF()
    {
        static void AddFileSpec<T>() where T : IFileSpec, new()
        {
            FileSpecs.Add(IFileSpec.GetMagic<T>(), () => new T());
        }

        AddFileSpec<CharacterData>();
        AddFileSpec<ChestData>();
        AddFileSpec<ExplorationData>();
        AddFileSpec<FontData>();
        AddFileSpec<GraphicAtlas>();
        AddFileSpec<ItemData>();
        AddFileSpec<LabyrinthData>();
        AddFileSpec<LocationData>();
        AddFileSpec<MapData>();
        AddFileSpec<MerchantData>();
        AddFileSpec<MonsterGroups>();
        AddFileSpec<OutroSequenceData>();
        AddFileSpec<Palette>();
        AddFileSpec<SavegameData>();
        AddFileSpec<Texts>();
        AddFileSpec<TilesetData>();

        static void AddCompression(KeyValuePair<ushort, ICompression> compression)
        {
            Compressions.Add(compression.Key, compression.Value);
        }

        AddCompression(ICompression.NoCompression);
        AddCompression(ICompression.Deflate);
        AddCompression(ICompression.RLE0);
        AddCompression(ICompression.PYC);
    }

    public static IFileSpec Read(IDataReader reader, GameData gameData)
    {
        if (!FileHeader.CheckHeader(reader, Header, true))
            throw new AmbermoonException(ExceptionScope.Data, "The file is no PADF");

        string fileType = reader.ReadString(3);

        if (!FileSpecs.TryGetValue(fileType, out var fileSpecProvider))
            throw new AmbermoonException(ExceptionScope.Data, $"Unknown data file in PADF: {fileType}");

        var fileSpec = fileSpecProvider();
        byte fileSpecVersion = reader.ReadByte();

        if (fileSpecVersion > fileSpec.GetSupportedVersion())
            throw new AmbermoonException(ExceptionScope.Data, $"This application only supports {fileType} versions up to {(int)fileSpec.GetSupportedVersion()} but file has version {(int)fileSpecVersion}");

        ushort compression = reader.Size - reader.Position < 2 ? (ushort)0 : reader.PeekWord();

        if (Compressions.TryGetValue(compression, out var decompressor))
        {
            reader.Position += 2;
            reader = decompressor.Decompress(reader);
        }

        fileSpec.Read(reader, 1u, gameData);

        return fileSpec;
    }

    public static void Write<T>(IDataWriter writer, T fileSpec, ICompression? compression = null)
        where T : IFileSpec
    {
        writer.WriteWithoutLength(Header);
        writer.WriteWithoutLength(IFileSpec.GetMagic<T>());
        writer.Write(IFileSpec.GetSupportedVersion<T>());

        IDataWriter dataWriter = new Legacy.Serialization.DataWriter();
        fileSpec.Write(dataWriter);

        compression ??= IFileSpec.GetPreferredCompression<T>();
        writer.Write(compression.GetIdentifier());
        dataWriter = compression.Compress(dataWriter);

        writer.Write(dataWriter.ToArray());
    }
}