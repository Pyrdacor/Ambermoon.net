using Ambermoon.Data.Serialization;
using System;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Legacy.Serialization
{
#pragma warning disable CS8981
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;
#pragma warning restore CS8981

    public class DataSerializer : IDataSerializer
    {
        byte[] data;

        public DataSerializer(byte[] data, WriteOperation writeOperation = WriteOperation.Insert)
            : this(data, 0, writeOperation)
        {

        }

        public DataSerializer(byte[] data, int offset, WriteOperation writeOperation = WriteOperation.Insert)
            : this(data, offset, data.Length - offset, writeOperation)
        {

        }

        public DataSerializer(byte[] data, int offset, int length, WriteOperation writeOperation = WriteOperation.Insert)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            this.data = new byte[length];
            Buffer.BlockCopy(data, offset, this.data, 0, length);
            Size = length;
            WriteOperation = writeOperation;
        }

        public WriteOperation WriteOperation { get; set; } = WriteOperation.Insert;
        IDataReader DataReader => new DataReader(data, Position);
        IDataWriter DataWriter => new DataWriter(data, Position);
        public int Position { get; set; } = 0;
        public int Size { get; }

        void UseReader(Action<IDataReader> reader)
        {
            var dataReader = DataReader;
            reader(dataReader);
            Position = dataReader.Position;
        }

        void UseWriter(Action<IDataWriter> writer)
        {
            var dataWriter = DataWriter;
            writer(dataWriter);
            Position = dataWriter.Position;
        }

        T UseReader<T>(Func<IDataReader, T> reader)
        {
            var dataReader = DataReader;
            var result = reader(dataReader);
            Position = dataReader.Position;
            return result;
        }

        T UseWriter<T>(Func<IDataWriter, T> writer)
        {
            var dataWriter = DataWriter;
            var result = writer(dataWriter);
            Position = dataWriter.Position;
            return result;
        }

        public void AlignToDword()
        {
            UseReader(reader => reader.AlignToDword());
        }

        public void AlignToWord()
        {
            UseReader(reader => reader.AlignToWord());
        }

        public long FindByteSequence(byte[] sequence, long offset)
        {
            return UseReader(reader => reader.FindByteSequence(sequence, offset));
        }

        public long FindString(string str, long offset)
        {
            return UseReader(reader => reader.FindString(str, offset));
        }

        public byte PeekByte()
        {
            return UseReader(reader => reader.PeekByte());
        }

        public dword PeekDword()
        {
            return UseReader(reader => reader.PeekDword());
        }

        public word PeekWord()
        {
            return UseReader(reader => reader.PeekWord());
        }

        public bool ReadBool()
        {
            return UseReader(reader => reader.ReadBool());
        }

        public byte ReadByte()
        {
            return UseReader(reader => reader.ReadByte());
        }

        public byte[] ReadBytes(int amount)
        {
            return UseReader(reader => reader.ReadBytes(amount));
        }

        public string ReadChar()
        {
            return UseReader(reader => reader.ReadChar());
        }

        public dword ReadDword()
        {
            return UseReader(reader => reader.ReadDword());
        }

        public string ReadNullTerminatedString()
        {
            return UseReader(reader => reader.ReadNullTerminatedString());
        }

        public string ReadNullTerminatedString(Encoding encoding)
        {
            return UseReader(reader => reader.ReadNullTerminatedString(encoding));
        }

        public qword ReadQword()
        {
            return UseReader(reader => reader.ReadQword());
        }

        public string ReadString()
        {
            return UseReader(reader => reader.ReadString());
        }

        public string ReadString(Encoding encoding)
        {
            return UseReader(reader => reader.ReadString(encoding));
        }

        public string ReadString(int length)
        {
            return UseReader(reader => reader.ReadString(length));
        }

        public string ReadString(int length, Encoding encoding)
        {
            return UseReader(reader => reader.ReadString(length, encoding));
        }

        public byte[] ReadToEnd()
        {
            return UseReader(reader => reader.ReadToEnd());
        }

        public word ReadWord()
        {
            return UseReader(reader => reader.ReadWord());
        }

        public byte[] ToArray()
        {
            return data;
        }

        public byte[] GetBytes(int offset, int length)
        {
            return data[offset..(offset + length)];
        }

        public void Append(bool value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(byte value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(word value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(dword value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(qword value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(char value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(string value)
        {
            UseWriter(writer => writer.Write(value));
        }

        public void Append(string value, int length, char fillChar = ' ')
        {
            UseWriter(writer => writer.Write(value, length, fillChar));
        }

        public void Append(string value, Encoding encoding)
        {
            UseWriter(writer => writer.Write(value, encoding));
        }

        public void Append(string value, Encoding encoding, int length, char fillChar = ' ')
        {
            UseWriter(writer => writer.Write(value, encoding, length, fillChar));
        }

        public void Append(byte[] bytes)
        {
            UseWriter(writer => writer.Write(bytes));
        }

        public void Replace(int offset, bool value)
        {
            UseWriter(writer => writer.Replace(offset, value));
        }

        public void Replace(int offset, byte value)
        {
            UseWriter(writer => writer.Replace(offset, value));
        }

        public void Replace(int offset, word value)
        {
            UseWriter(writer => writer.Replace(offset, value));
        }

        public void Replace(int offset, dword value)
        {
            UseWriter(writer => writer.Replace(offset, value));
        }

        public void Replace(int offset, qword value)
        {
            UseWriter(writer => writer.Replace(offset, value));
        }

        public void Replace(int offset, byte[] data)
        {
            UseWriter(writer => writer.Replace(offset, data));
        }

        public void Replace(int offset, byte[] data, int dataOffset)
        {
            UseWriter(writer => writer.Replace(offset, data, dataOffset));
        }

        public void Replace(int offset, byte[] data, int dataOffset, int length)
        {
            UseWriter(writer => writer.Replace(offset, data, dataOffset, length));
        }

        public void CopyTo(Stream stream)
        {
            UseWriter(writer => writer.CopyTo(stream));
        }

        public void AppendEnumAsByte<T>(T value) where T : struct, Enum, IConvertible
        {
            UseWriter(writer => writer.WriteEnumAsByte(value));
        }

        public void AppendEnumAsWord<T>(T value) where T : struct, Enum, IConvertible
        {
            UseWriter(writer => writer.WriteEnumAsWord(value));
        }

        public void AppendNullTerminated(string value)
        {
            UseWriter(writer => writer.WriteNullTerminated(value));
        }

        public void AppendNullTerminated(string value, Encoding encoding)
        {
            UseWriter(writer => writer.WriteNullTerminated(value, encoding));
        }

        public void AppendWithoutLength(string value)
        {
            UseWriter(writer => writer.WriteWithoutLength(value));
        }

        public void AppendWithoutLength(string value, Encoding encoding)
        {
            UseWriter(writer => writer.WriteWithoutLength(value, encoding));
        }

        void Write(Action overrider, Action appender)
        {
            Write(_ => overrider(), _ => appender(), (_, bytes) => Append(bytes));
        }

        void Write(Action<IDataWriter> overrider, Action<IDataWriter> appender, Action<IDataWriter, byte[]> byteAppender)
        {
            var writeOperation = WriteOperation;

            if (writeOperation == WriteOperation.Insert && Position == Size)
                writeOperation = WriteOperation.Append;

            switch (writeOperation)
            {
                case WriteOperation.Insert:
                {
                    var temp = new DataWriter();
                    var reader = DataReader;
                    byteAppender(temp, reader.ReadBytes(Position));
                    appender(temp);
                    int position = temp.Size;
                    byteAppender(temp, reader.ReadToEnd());
                    data = temp.ToArray();
                    Position = position;
                    break;
                }
                case WriteOperation.Override:
                    overrider(this);
                    break;
                case WriteOperation.Append:
                    appender(this);
                    break;
            }
        }

        void Override(int offset, params byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= this.data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + data.Length > this.data.Length)
                throw new IndexOutOfRangeException("Data size is out of range.");
            for (int i = 0; i < data.Length; ++i)
                this.data[offset + i] = data[i];
        }

        void Override(params byte[] data) => Override(Position, data);

        public void Write(bool value)
        {
            Write(() => Override((byte)(value ? 1 : 0)), () => Write(value));
        }

        public void Write(byte value)
        {
            Write(() => Override(value), () => Write(value));
        }

        public void Write(word value)
        {
            Write(() => Override((byte)(value >> 8), (byte)value), () => Write(value));
        }

        public void Write(dword value)
        {
            Write(() => Override((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value), () => Write(value));
        }

        public void Write(qword value)
        {
            Write(() => Override((byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value), () => Write(value));
        }

        public void Write(char value)
        {
            Write(value.ToString());
        }

        public void Write(string value)
        {
            Write(value, Serialization.DataWriter.Encoding);
        }

        public void Write(string value, int length, char fillChar = ' ')
        {
            Write(value, Serialization.DataWriter.Encoding, length, fillChar);
        }

        public void Write(string value, Encoding encoding)
        {
            var bytes = encoding.GetBytes(value);

            if (bytes.Length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            Write((byte)bytes.Length);

            if (bytes.Length != 0)
                Write(bytes);
        }

        public void Write(string value, Encoding encoding, int length, char fillChar = ' ')
        {
            if (length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            if (length > value.Length)
                value += new string(fillChar, length - value.Length);
            else
                value = value.Substring(0, length);

            Write(value, encoding);
        }

        public void Write(byte[] bytes)
        {
            Write(() => Override(bytes), () => Write(bytes));
        }

        public void WriteEnumAsByte<T>(T value) where T : struct, Enum, IConvertible
        {
            Write(value.ToByte(null));
        }

        public void WriteEnumAsWord<T>(T value) where T : struct, Enum, IConvertible
        {
            Write(value.ToUInt16(null));
        }

        public void WriteNullTerminated(string value)
        {
            WriteNullTerminated(value, Serialization.DataWriter.Encoding);
        }

        public void WriteNullTerminated(string value, Encoding encoding)
        {
            Write(encoding.GetBytes(value + "\0"));
        }

        public void WriteWithoutLength(string value)
        {
            WriteWithoutLength(value, Serialization.DataWriter.Encoding);
        }

        public void WriteWithoutLength(string value, Encoding encoding)
        {
            Write(encoding.GetBytes(value));
        }
    }
}
