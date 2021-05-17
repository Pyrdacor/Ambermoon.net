using System;
using System.IO;

namespace SonicArranger
{
    internal class BuiltinReader : ICustomReader, IDisposable
    {
        BinaryReader reader = null;

        public BuiltinReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public int Position
        {
            get => (int)reader.BaseStream.Position;
            set => reader.BaseStream.Position = value;
        }

        public int Size => (int)reader.BaseStream.Length;

        public short ReadBEInt16() => reader.ReadBEInt16();

        public int ReadBEInt32() => reader.ReadBEInt32();

        public ushort ReadBEUInt16() => reader.ReadBEUInt16();

        public uint ReadBEUInt32() => reader.ReadBEUInt32();

        public byte ReadByte() => reader.ReadByte();

        public byte[] ReadBytes(int amount) => reader.ReadBytes(amount);

        public char[] ReadChars(int amount) => reader.ReadChars(amount);

        public void Dispose()
        {
            reader?.Dispose();
            reader = null;
        }
    }
}
