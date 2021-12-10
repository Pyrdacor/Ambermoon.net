using Ambermoon;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AmbermoonAndroid
{
    class BuiltinVersion
    {
        public string Version;
        public string Language;
        public string Info;
        public Features Features;
        public uint Offset;
        public uint Size;
        public Stream SourceStream;
    }

    class BuiltinVersionLoader : IDisposable
    {
        Stream executableStream;
        bool disposed;

        public List<BuiltinVersion> Load()
        {
            int ReadWord(BinaryReader reader)
            {
                return ((int)reader.ReadByte() << 8) | reader.ReadByte();
            }

            uint ReadDword(BinaryReader reader)
            {
                return ((uint)reader.ReadByte() << 24) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 8) | reader.ReadByte();
            }

            executableStream = FileProvider.GetVersions();
            using var reader = new BinaryReader(executableStream, Encoding.UTF8, true);

            executableStream.Position = executableStream.Length - 2;

            if (reader.ReadByte() != 0xB0 || reader.ReadByte() != 0x55)
                return new List<BuiltinVersion>();

            executableStream.Position -= 6;
            uint offset = ReadDword(reader);
            executableStream.Position -= offset + 4;

            int versionCount = ReadWord(reader);
            var versions = new List<BuiltinVersion>(versionCount);

            for (int i = 0; i < versionCount; ++i)
            {
                versions.Add(new BuiltinVersion
                {
                    Version = reader.ReadString(),
                    Language = reader.ReadString(),
                    Info = reader.ReadString(),
                    Features = (Features)reader.ReadByte(),
                    Size = ReadDword(reader),
                    SourceStream = executableStream
                });
            }

            offset = (uint)executableStream.Position;

            for (int i = 0; i < versionCount; ++i)
            {
                versions[i].Offset = offset;
                offset += versions[i].Size;
            }

            return versions;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                executableStream?.Dispose();
                executableStream = null;
                disposed = true;
            }
        }
    }
}
