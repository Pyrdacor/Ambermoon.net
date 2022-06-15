using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Ambermoon.AdditionalData
{
    public class DataEntry
    {
        public DataEntry(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; }
        public byte[] Data { get; }
    }

    public class Loader
    {
        void FindNextSegmentName(BinaryReader reader)
        {
            var buffer = reader.ReadBytes(Math.Min(128, (int)reader.BaseStream.Length));
        }

        public static Dictionary<string, BinaryReader> Load()
        {
            static int ReadWord(BinaryReader reader)
            {
                return ((int)reader.ReadByte() << 8) | reader.ReadByte();
            }

            static uint ReadDword(BinaryReader reader)
            {
                return ((uint)reader.ReadByte() << 24) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 8) | reader.ReadByte();
            }

            static int ReadInt(BinaryReader reader)
            {
                return (int)(ReadDword(reader) & int.MaxValue);
            }

            var entries = new Dictionary<string, BinaryReader>();
            var executableFileName = Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetEntryAssembly()?.Location;

            if (executableFileName == null)
                return entries;

            var executableStream = File.OpenRead(executableFileName);
            using var reader = new BinaryReader(executableStream, Encoding.UTF8, true);

            executableStream.Position = executableStream.Length - 2;

            // 0xB055 is the marker
            if (reader.ReadByte() != 0xB0 || reader.ReadByte() != 0x55)
                return entries;

            executableStream.Position -= 6;
            uint offset = ReadDword(reader);
            executableStream.Position -= offset + 4;

            int entryCount = ReadWord(reader);

            for (int i = 0; i < entryCount; ++i)
            {
                int nameLength = reader.ReadByte();
                string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                int entrySize = ReadInt(reader);
                var data = reader.ReadBytes(entrySize);
                entries.Add(name, new BinaryReader(new MemoryStream(data)));
            }

            return entries;
        }

        public static void Create(string targetFilePath, params DataEntry[] entries)
        {
            Create(targetFilePath, (IEnumerable<DataEntry>)entries);
        }

        public static void Create(string targetFilePath, IEnumerable<DataEntry> entries)
        {
            static void WriteWord(BinaryWriter writer, ushort word)
            {
                writer.Write((byte)((word >> 8) & 0xff));
                writer.Write((byte)(word & 0xff));
            }

            static void WriteDword(BinaryWriter writer, uint dword)
            {
                writer.Write((byte)((dword >> 24) & 0xff));
                writer.Write((byte)((dword >> 16) & 0xff));
                writer.Write((byte)((dword >> 8) & 0xff));
                writer.Write((byte)(dword & 0xff));
            }

            static void WriteInt(BinaryWriter writer, int integer)
            {
                if (integer < 0)
                    throw new NotSupportedException("Negative integers are not supported.");

                WriteDword(writer, (uint)integer);
            }

            using var file = File.Open(targetFilePath, FileMode.Append);
            using var writer = new BinaryWriter(file);

            long startPosition = writer.BaseStream.Position;
            int entryCount = entries.Count();

            if (entryCount > ushort.MaxValue)
                throw new NotSupportedException($"Total number of entries must not exceed {ushort.MaxValue}.");

            WriteWord(writer, (ushort)entryCount);

            foreach (var entry in entries)
            {
                var nameData = Encoding.UTF8.GetBytes(entry.Name);
                writer.Write((byte)nameData.Length);
                writer.Write(nameData);
                WriteInt(writer, entry.Data.Length);
                writer.Write(entry.Data);
            }

            long offset = writer.BaseStream.Position - startPosition;

            if (offset > uint.MaxValue)
                throw new NotSupportedException($"Total entry size is not allowed to exceed {uint.MaxValue} bytes.");

            WriteDword(writer, (uint)offset);
            WriteWord(writer, 0xB055);
        }
    }
}
