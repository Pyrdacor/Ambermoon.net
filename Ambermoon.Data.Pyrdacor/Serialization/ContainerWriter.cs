using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public class ContainerWriter : BinaryWriter
    {
        public Endianness Endianness { get; } = Endianness.Little;

        public ContainerWriter(Stream input)
            : base(input)
        {
        }

        public ContainerWriter(Stream input, Encoding encoding)
            : base(input, encoding)
        {
        }

        public ContainerWriter(Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen)
        {
        }

        public ContainerWriter(Stream input, Endianness endianness)
            : base(input)
        {
            Endianness = endianness;
        }

        public ContainerWriter(Stream input, Encoding encoding, Endianness endianness)
            : base(input, encoding)
        {
            Endianness = endianness;
        }

        public ContainerWriter(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness)
            : base(input, encoding, leaveOpen)
        {
            Endianness = endianness;
        }

        void Write(int size, Action<byte[]> writer)
        {
            var buffer = new byte[size];
            writer(buffer);
            Write(buffer);
        }

        public override void Write(short value) => Write(value, Endianness);

        public void Write(short value, Endianness endianness)
        {
            Write(sizeof(short), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteInt16LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteInt16BigEndian(data, value);
            });
        }

        public override void Write(ushort value) => Write(value, Endianness);

        public void Write(ushort value, Endianness endianness)
        {
            Write(sizeof(ushort), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteUInt16LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteUInt16BigEndian(data, value);
            });
        }

        public override void Write(int value) => Write(value, Endianness);

        public void Write(int value, Endianness endianness)
        {
            Write(sizeof(int), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteInt32LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteInt32BigEndian(data, value);
            });
        }

        public override void Write(uint value) => Write(value, Endianness);

        public void Write(uint value, Endianness endianness)
        {
            Write(sizeof(uint), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteUInt32LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteUInt32BigEndian(data, value);
            });
        }

        public override void Write(long value) => Write(value, Endianness);

        public void Write(long value, Endianness endianness)
        {
            Write(sizeof(long), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteInt64LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteInt64BigEndian(data, value);
            });
        }

        public override void Write(ulong value) => Write(value, Endianness);

        public void Write(ulong value, Endianness endianness)
        {
            Write(sizeof(ulong), data =>
            {
                if (endianness == Endianness.Little)
                    BinaryPrimitives.WriteUInt64LittleEndian(data, value);
                else
                    BinaryPrimitives.WriteUInt64BigEndian(data, value);
            });
        }
    }
}
