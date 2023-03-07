using Ambermoon.Data.Enumerations;
using System.Collections.Generic;
using System.IO;

namespace Ambermoon
{
    class BuiltinVersion
    {
        public string Version;
        public string Language;
        public string Info;
        public Features Features;
        public bool MergeWithPrevious;
        public uint Offset;
        public uint Size;
        public Stream SourceStream;
    }

    class BuiltinVersionLoader
    {
        public List<BuiltinVersion> Load(BinaryReader reader)
        {
            int ReadWord(BinaryReader reader)
            {
                return ((int)reader.ReadByte() << 8) | reader.ReadByte();
            }

            uint ReadDword(BinaryReader reader)
            {
                return ((uint)reader.ReadByte() << 24) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 8) | reader.ReadByte();
            }

            int versionCount = ReadWord(reader);
            var versions = new List<BuiltinVersion>(versionCount);

            for (int i = 0; i < versionCount; ++i)
            {
                versions.Add(new BuiltinVersion
                {
                    Version = reader.ReadString(),
                    Language = reader.ReadString(),
                    Info = reader.ReadString(),
                    Features = (Features)ReadWord(reader),
                    MergeWithPrevious = reader.ReadByte() != 0,
                    Size = ReadDword(reader),
                    SourceStream = reader.BaseStream
                });
            }

            uint offset = (uint)reader.BaseStream.Position;

            for (int i = 0; i < versionCount; ++i)
            {
                versions[i].Offset = offset;
                offset += versions[i].Size;
            }

            return versions;
        }
    }
}
