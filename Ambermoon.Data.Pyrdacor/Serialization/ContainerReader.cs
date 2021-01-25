using Ambermoon.Data.Serialization;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public class ContainerReader : BinaryReader, IDataReader
    {
        public Endianness Endianness { get; } = Endianness.Little;
        public int Position { get => (int)BaseStream.Position; set => BaseStream.Position = value; }
        public int Size => (int)BaseStream.Length;

        public ContainerReader(Stream input)
            : base(input)
        {
        }

        public ContainerReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public ContainerReader(Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen)
        {
        }

        public ContainerReader(Stream input, Endianness endianness)
            : base(input)
        {
            Endianness = endianness;
        }

        public ContainerReader(Stream input, Encoding encoding, Endianness endianness)
            : base(input, encoding)
        {
            Endianness = endianness;
        }

        public ContainerReader(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness)
            : base(input, encoding, leaveOpen)
        {
            Endianness = endianness;
        }

        public override short ReadInt16() => ReadInt16(Endianness);

        public short ReadInt16(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(sizeof(short)))
            : BinaryPrimitives.ReadInt16BigEndian(ReadBytes(sizeof(short)));

        public override ushort ReadUInt16() => ReadUInt16(Endianness);

        public ushort ReadUInt16(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(sizeof(ushort)))
            : BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(sizeof(ushort)));

        public override int ReadInt32() => ReadInt32(Endianness);

        public int ReadInt32(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(sizeof(int)))
            : BinaryPrimitives.ReadInt32BigEndian(ReadBytes(sizeof(int)));

        public override uint ReadUInt32() => ReadUInt32(Endianness);

        public uint ReadUInt32(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(sizeof(uint)))
            : BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(sizeof(uint)));

        public override long ReadInt64() => ReadInt64(Endianness);

        public long ReadInt64(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(sizeof(long)))
            : BinaryPrimitives.ReadInt64BigEndian(ReadBytes(sizeof(long)));

        public override ulong ReadUInt64() => ReadUInt64(Endianness);

        public ulong ReadUInt64(Endianness endianness) => endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(sizeof(ulong)))
            : BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(sizeof(ulong)));
    }
}
