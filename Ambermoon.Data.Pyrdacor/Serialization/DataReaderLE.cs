using System.Buffers.Binary;
using System.Text;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Serialization;

public class DataReaderLE : IDataReader
{
    readonly byte[] data;
    int position;

    public int Position
    {
        get => position;
        set
        {
            if (value < 0 || value > Size)
                throw new IndexOutOfRangeException("Data index out of range.");

            position = value;
        }
    }

    public int Size => data.Length;

    ReadOnlySpan<byte> Span => data;

    public DataReaderLE(byte[] data)
    {
        this.data = data;
        position = 0;
    }

    public DataReaderLE(Stream stream)
    {
        int size = (int)(stream.Length - stream.Position);
        data = new byte[size];
        stream.ReadExactly(data);
        position = 0;
    }

    public void AlignToDword()
    {
        position = (position + 3) & ~3;
    }

    public void AlignToWord()
    {
        position = (position + 1) & ~1;
    }

    public long FindByteSequence(byte[] sequence, long offset)
    {
        if (offset + sequence.Length > data.Length)
            return -1;

        int index = Span.Slice((int)offset).IndexOf(sequence);

        return index < 0 ? -1 : offset + index;
    }

    public long FindString(string str, long offset)
    {
        return FindByteSequence(DataReader.Encoding.GetBytes(str), offset);
    }

    public byte PeekByte()
    {
        return data[position];
    }

    public ushort PeekWord()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(Span[position..]);
    }

    public uint PeekDword()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(Span[position..]);
    }

    public bool ReadBool()
    {
        return ReadByte() != 0;
    }

    public byte ReadByte()
    {
        return data[position++];
    }

    public byte[] ReadBytes(int amount)
    {
        var bytes = Span.Slice(position, amount).ToArray();
        position += amount;
        return bytes;
    }

    public string ReadChar() => ReadString(1);

    public uint ReadDword()
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(Span[position..]);
        position += 4;
        return value;
    }

    public ushort ReadWord()
    {
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(Span[position..]);
        position += 2;
        return value;
    }

    public ulong ReadQword()
    {
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(Span[position..]);
        position += 8;
        return value;
    }

    public string ReadNullTerminatedString() => ReadNullTerminatedString(DataReader.Encoding);

    public string ReadNullTerminatedString(Encoding encoding)
    {
        List<byte> buffer = [];
        byte b;
        bool needMoreBytes = false;

        while (Position < Size && ((b = ReadByte()) != 0 || needMoreBytes))
        {
            buffer.Add(b);

            // When parsing multi-byte encodings there might be characters which
            // end with a 00-byte. As this is also used for termination we have
            // to check for character ending if the next byte is 00.
            if (!encoding.IsSingleByte && Position < Size && PeekByte() == 0)
            {
                try
                {
                    encoding.GetString(buffer.ToArray());
                }
                catch (ArgumentException)
                {
                    needMoreBytes = true;
                }
            }
        }

        try
        {
            return encoding.GetString(buffer.ToArray());
        }
        catch (ArgumentException)
        {
            return encoding.GetString(buffer.Take(buffer.Count - 1).ToArray()) + "?";
        }
    }

    public string ReadString() => ReadString(DataReader.Encoding);

    public string ReadString(Encoding encoding)
    {
        int length = ReadByte();
        return ReadString(length, encoding);
    }

    public string ReadString(int length) => ReadString(length, DataReader.Encoding);

    public string ReadString(int length, Encoding encoding)
    {
        if (length == 0)
            return string.Empty;

        var str = encoding.GetString(data, position, length);
        str = str.Replace(encoding.GetString([0xb4]), "'");
        position += length;
        return str;
    }

    public byte[] ReadToEnd()
    {
        var result = Span[position..].ToArray();
        position = Size;
        return result;
    }

    public byte[] ToArray() => data;
}