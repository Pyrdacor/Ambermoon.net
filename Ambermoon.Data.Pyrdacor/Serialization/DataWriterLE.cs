using System.Buffers.Binary;
using System.Text;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Serialization;

public class DataWriterLE : IDataWriter
{
    protected readonly List<byte> data = [];
    public int Position { get; private set; } = 0;
    public int Size => data.Count;

    public byte this[int index]
    {
        get => data[index];
        set
        {
            if (index == data.Count)
                data.Add(value);
            else
                data[index] = value;
        }
    }

    public DataWriterLE()
    {
    }

    public DataWriterLE(byte[] data)
    {
        this.data.AddRange(data);
        Position = data.Length;
    }

    public void Write(bool value)
    {
        data.Add(value ? (byte)1 : (byte)0);
        Position++;
    }

    public void Write(byte value)
    {
        data.Add(value);
        Position++;
    }

    public void Write(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        Write(buffer);
    }

    public void Write(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        Write(buffer);
    }

    public void Write(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        Write(buffer);
    }

    public void Write(char value)
    {
        Write(value.ToString());
    }

    public void Write(string value)
    {
        Write(value, DataWriter.Encoding);
    }

    public void Write(string value, int length, char fillChar = ' ')
    {
        Write(value, DataWriter.Encoding, length, fillChar);
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
            value = value[..length];

        Write(value, encoding);
    }

    public void WriteNullTerminated(string value)
    {
        WriteNullTerminated(value, DataWriter.Encoding);
    }

    public void WriteNullTerminated(string value, Encoding encoding)
    {
        Write(encoding.GetBytes(value));
        Write((byte)0);
    }

    public void WriteWithoutLength(string value)
    {
        WriteWithoutLength(value, DataWriter.Encoding);
    }

    public void WriteWithoutLength(string value, Encoding encoding)
    {
        Write(encoding.GetBytes(value));
    }

    public void Write(byte[] bytes)
    {
        data.AddRange(bytes);
        Position += bytes.Length;
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        data.AddRange(bytes);
        Position += bytes.Length;
    }

    public void Replace(int offset, bool value)
    {
        Replace(offset, (byte)(value ? 1 : 0));
    }

    public void Replace(int offset, byte value)
    {
        if (offset < 0 || offset + 1 > Size)
            throw new IndexOutOfRangeException("Index was outside the data writer size.");

        data[offset] = value;
    }

    public void Replace(int offset, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        Replace(offset, buffer);
    }

    public void Replace(int offset, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        Replace(offset, buffer);
    }

    public void Replace(int offset, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        Replace(offset, buffer);
    }

    public void Replace(int offset, byte[] data)
    {
        Replace(offset, data, 0);
    }

    public void Replace(int offset, byte[] data, int dataOffset)
    {
        Replace(offset, data, dataOffset, data.Length - dataOffset);
    }

    public void Replace(int offset, byte[] data, int dataOffset, int length)
    {
        if (dataOffset < 0 || dataOffset + length > data.Length)
            throw new IndexOutOfRangeException("Index was outside the given data array.");

        if (offset < 0 || offset + length > Size)
            throw new IndexOutOfRangeException("Index was outside the data writer size.");

        for (int i = 0; i < length; i++)
            this.data[offset + i] = data[dataOffset + i];
    }

    public void Replace(int offset, ReadOnlySpan<byte> bytes)
    {
        if (offset < 0 || offset + bytes.Length > Size)
            throw new IndexOutOfRangeException("Index was outside the data writer size.");

        for (int i = 0; i < bytes.Length; i++)
            data[offset + i] = bytes[i];
    }

    public void CopyTo(Stream stream)
    {
        stream.Write(data.ToArray(), 0, data.Count);
    }

    public byte[] ToArray() => data.ToArray();

    public byte[] GetBytes(int offset, int length)
    {
        return data.GetRange(offset, length).ToArray();
    }

    public void WriteEnumAsByte<T>(T value) where T : struct, Enum, IConvertible
        => Write(value.ToByte(null));

    public void WriteEnumAsWord<T>(T value) where T : struct, Enum, IConvertible
        => Write(value.ToUInt16(null));

    public void Remove(int index, int count)
    {
        if (index >= Size)
            return;

        data.RemoveRange(index, count);
    }

    public void Clear()
    {
        data.Clear();
        Position = 0;
    }
}