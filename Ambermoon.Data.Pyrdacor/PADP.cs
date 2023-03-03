using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor
{
    /// <summary>
    /// Pyrdacor's Ambermoon Data Package
    /// </summary>
    internal static class PADP
    {
        // Note: The max file count is 0xffff-1. As file indices are 1-based, file 0 is not valid.
        // So only indices 1 to 0xffff can be stored. In sum there is space for the given file count.

        public const string Header = "PADP";

        public static Dictionary<ushort, T> Read<T>(IDataReader reader, GameData gameData) where T : IFileSpec, new()
        {
            bool wasPADF = false;
            var result = InternalRead(reader, gameData, ref wasPADF, IFileSpec.GetSupportedVersion<T>());

            if (result.Count == 0)
                return new Dictionary<ushort, T>();

            string expectedFileType = IFileSpec.GetMagic<T>();
            string readFileType = result.First().Value.Magic;

            if (readFileType != expectedFileType)
            {
                string type = wasPADF ? "PADF" : "PADP";
                throw new AmbermoonException(ExceptionScope.Data, $"{type} contains different content than expected. Requested {expectedFileType}, but found {readFileType}.");
            }

            return result.ToDictionary(r => r.Key, r => (T)r.Value);
        }

        public static Dictionary<ushort, IFileSpec> Read(IDataReader reader, GameData gameData)
        {
            bool wasPADF = false;
            return InternalRead(reader, gameData, ref wasPADF);
        }

        private static Dictionary<ushort, IFileSpec> InternalRead(IDataReader reader, GameData gameData, ref bool wasPADF, byte? supportedVersion = null)
        {
            if (FileHeader.CheckHeader(reader, PADF.Header, false))
            {
                wasPADF = true;
                var spec = PADF.Read(reader, gameData);
                return new Dictionary<ushort, IFileSpec> { { (ushort)1u, spec } };
            }

            if (!FileHeader.CheckHeader(reader, Header, true))
                throw new AmbermoonException(ExceptionScope.Data, "The file is no PADP");

            string fileType = reader.ReadString(3);

            if (!PADF.FileSpecs.TryGetValue(fileType, out var fileSpecProvider))
                throw new AmbermoonException(ExceptionScope.Data, $"Unknown data files in PADP: {fileType}");

            supportedVersion ??= fileSpecProvider().SupportedVersion;
            byte fileSpecVersion = reader.ReadByte();

            if (fileSpecVersion > supportedVersion)
                throw new AmbermoonException(ExceptionScope.Data, $"This application only supports {fileType} versions up to {(int)supportedVersion} but file has version {(int)fileSpecVersion}");

            var files = new Dictionary<ushort, IFileSpec>();
            int fileCount = reader.ReadWord();

            if (fileCount >= ushort.MaxValue)
                throw new AmbermoonException(ExceptionScope.Data, $"Too many files given in PADP. Allowed are {ushort.MaxValue - 1}, given are {fileCount}.");

            if (fileCount == 0)
                return files;

            var fileSizes = new ushort[fileCount];

            for (int i = 0; i < fileCount; ++i)
                fileSizes[i] = reader.ReadWord();

            var fileIndices = new ushort[fileCount];

            if (reader.PeekWord() == 0) // no file indices
            {
                for (int i = 1; i <= fileCount; ++i)
                    fileIndices[i - 1] = (ushort)i;
            }
            else
            {
                for (int i = 0; i < fileCount; ++i)
                    fileIndices[i] = reader.ReadWord();
            }

            ushort compression = reader.Size - reader.Position < 2 ? (ushort)0 : reader.PeekWord();

            if (PADF.Compressions.TryGetValue(compression, out var decompressor))
            {
                reader.Position += 2;
                reader = decompressor.Decompress(reader);
            }

            for (int i = 0; i < fileCount; ++i)
            {
                var fileSpec = fileSpecProvider();
                fileSpec.Read(new DataReader(reader.ReadBytes(fileSizes[i])), fileIndices[i], gameData);
                files.Add(fileIndices[i], fileSpec);
            }

            return files;
        }

        public static void Write<T>(IDataWriter writer, IDictionary<ushort, T> fileSpecs, ICompression? compression = null) where T : IFileSpec, new()
        {
            InternalWrite<T>(writer, fileSpecs, compression, false);
        }

        public static void Write<T>(IDataWriter writer, IEnumerable<T> fileSpecs, ICompression? compression = null) where T : IFileSpec, new()
        {
            if (fileSpecs.Count() >= ushort.MaxValue)
                throw new AmbermoonException(ExceptionScope.Data, $"Too many files given for PADP. Allowed are {ushort.MaxValue - 1}, given are {fileSpecs.Count()}.");

            InternalWrite(writer, fileSpecs.Select((s, i) => new { i, s }).ToDictionary(s => (ushort)(1 + s.i), s => s.s), compression, true);
        }

        private static void InternalWrite<T>(IDataWriter writer, IDictionary<ushort, T> fileSpecs, ICompression? compression, bool writeNoFileIndices) where T : IFileSpec, new()
        {
            if (fileSpecs.Count >= ushort.MaxValue)
                throw new AmbermoonException(ExceptionScope.Data, $"Too many files given for PADP. Allowed are {ushort.MaxValue - 1}, given are {fileSpecs.Count}.");

            if (!writeNoFileIndices && fileSpecs.Keys.Any(k => k == 0))
                throw new AmbermoonException(ExceptionScope.Data, $"Sub-file key 0 is now allowed. Make sure to start at index 1.");

            writer.WriteWithoutLength(Header);

            string fileType = IFileSpec.GetMagic<T>();
            byte supportedVersion = IFileSpec.GetSupportedVersion<T>();

            writer.WriteWithoutLength(fileType);
            writer.Write(supportedVersion);
            writer.Write((ushort)fileSpecs.Count);

            IDataWriter dataWriter = new Legacy.Serialization.DataWriter();

            // Write file sizes and write the data to dataWriter
            foreach (var fileSpec in fileSpecs)
            {
                int position = dataWriter.Position;
                fileSpec.Value.Write(dataWriter);
                int size = dataWriter.Position - position;

                if (size > ushort.MaxValue)
                    throw new AmbermoonException(ExceptionScope.Data, $"Sub-file {fileSpec.Key} is too large. Max size is {ushort.MaxValue}, but file had {size}.");

                writer.Write((ushort)size);
            }

            // Write file indices
            if (writeNoFileIndices)
            {
                writer.Write((ushort)0); // no file indices marker
            }
            else
            {
                foreach (var fileSpec in fileSpecs)
                {
                    writer.Write(fileSpec.Key);
                }
            }

            // Compress data if needed
            compression ??= IFileSpec.GetPreferredCompression<T>();
            writer.Write(compression.Identifier);
            dataWriter = compression.Compress(dataWriter);

            // Write data
            writer.Write(dataWriter.ToArray());
        }
    }
}