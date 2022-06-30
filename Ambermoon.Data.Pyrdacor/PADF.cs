using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor
{
    /// <summary>
    /// Pyrdacor's Ambermoon Data File
    /// </summary>
    internal class PADF
    {
        public const string Header = "PADF";

        internal static readonly Dictionary<string, Func<IFileSpec>> FileSpecs = new Dictionary<string, Func<IFileSpec>>();
        internal static readonly Dictionary<ushort, ICompression> Compressions = new Dictionary<ushort, ICompression>();

        static PADF()
        {
            void AddFileSpec<T>() where T : IFileSpec, new()
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

            Compressions.Add(ICompression.NoCompression.Identifier, ICompression.NoCompression);
            Compressions.Add(ICompression.Deflate.Identifier, ICompression.Deflate);
            Compressions.Add(ICompression.RLE0.Identifier, ICompression.RLE0);
        }

        public IFileSpec Read(IDataReader reader, GameData gameData)
        {
            if (!FileHeader.CheckHeader(reader, Header, true))
                throw new AmbermoonException(ExceptionScope.Data, "The file is no PADF");

            string fileType = reader.ReadString(3);

            if (!FileSpecs.TryGetValue(fileType, out var fileSpecProvider))
                throw new AmbermoonException(ExceptionScope.Data, $"Unknown data file in PADF: {fileType}");

            var fileSpec = fileSpecProvider();
            byte fileSpecVersion = reader.ReadByte();

            if (fileSpecVersion > fileSpec.SupportedVersion)
                throw new AmbermoonException(ExceptionScope.Data, $"This application only supports {fileType} versions up to {(int)fileSpec.SupportedVersion} but file has version {(int)fileSpecVersion}");

            ushort compression = reader.Size - reader.Position < 2 ? (ushort)0 : reader.PeekWord();

            if (Compressions.TryGetValue(compression, out var decompressor))
            {
                reader.Position += 2;
                reader = decompressor.Decompress(reader);
            }

            fileSpec.Read(reader, 1u, gameData);

            return fileSpec;
        }

        public void Write(IDataWriter writer, IFileSpec fileSpec, ICompression? compression = null)
        {
            writer.WriteWithoutLength(Header);
            writer.WriteWithoutLength(fileSpec.Magic);
            writer.Write(fileSpec.SupportedVersion);

            IDataWriter dataWriter = new Legacy.Serialization.DataWriter();
            fileSpec.Write(dataWriter);

            if (compression == null && !Compressions.TryGetValue(fileSpec.PreferredCompression, out compression))
                compression = ICompression.NoCompression;
            writer.Write(compression.Identifier);
            dataWriter = compression.Compress(dataWriter);

            writer.Write(dataWriter.ToArray());
        }
    }
}