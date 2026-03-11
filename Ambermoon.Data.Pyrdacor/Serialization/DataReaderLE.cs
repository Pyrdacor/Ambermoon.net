using System.Text;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Serialization;

internal unsafe class DataReaderLE : IDataReader
{
    byte* dataPointer;
    readonly byte* startPointer;
    readonly byte[] data;
    private int position = 0;

    public int Position
    {
        get => position;
        set
        {
            if (value < 0 || value > Size)
                throw new IndexOutOfRangeException("Data index out of range.");

            position = value;
            dataPointer = startPointer + position; 
        }
    }

    public int Size { get; }

    public DataReaderLE(byte[] data)
    {
        position = 0;
        Size = data.Length;
        this.data = data;

        fixed (byte* ptr = &data[0])
        {
            dataPointer = ptr;
            startPointer = ptr;
        }
    }

    public DataReaderLE(Stream stream)
    {
        position = 0;
        Size = (int)(stream.Length - stream.Position);
        data = new byte[Size];
        
        stream.ReadExactly(data, 0, data.Length);

        fixed (byte* ptr = &data[0])
        {
            dataPointer = ptr;
            startPointer = ptr;
        }
    }

    public void AlignToDword()
    {
        while (position % 4 != 0)
        {
            position++;
            dataPointer++;
        }
    }

    public void AlignToWord()
    {
        if (position % 2 == 1)
        {
            position++;
            dataPointer++;
        }
    }

    public long FindByteSequence(byte[] sequence, long offset)
    {
        if (offset + sequence.Length > data.Length)
            return -1;

        var span = data.AsSpan((int)offset);
        int index = span.IndexOf(sequence);

        return index < 0 ? -1 : offset + index;
    }

    public long FindString(string str, long offset)
    {
        return FindByteSequence(DataReader.Encoding.GetBytes(str), offset);
    }

    public byte PeekByte()
    {
        return *dataPointer;
    }

    public uint PeekDword()
    {
        return *(uint*)dataPointer;
    }

    public ushort PeekWord()
    {
        return *(ushort*)dataPointer;
    }

    public bool ReadBool()
    {
        return ReadByte() != 0;
    }

    public byte ReadByte()
    {
        position++;
        return *dataPointer++;
    }

    public byte[] ReadBytes(int amount)
    {
        var bytes = data[position..(position + amount)];
        position += amount;

        return bytes;
    }

    public string ReadChar() => ReadString(1);

    public uint ReadDword()
    {
        uint value = *(uint*)dataPointer;
        dataPointer += 4;
        position += 4;
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
                    encoding.GetString([.. buffer]);
                }
                catch (ArgumentException)
                {
                    needMoreBytes = true;
                }
            }
        }

        try
        {
            return encoding.GetString([.. buffer]);
        }
        catch (ArgumentException)
        {
            return encoding.GetString([.. buffer.Take(buffer.Count - 1)]) + "?";
        }
    }

    public ulong ReadQword()
    {
        ulong value = *(ulong*)dataPointer;
        dataPointer += 8;
        position += 8;
        return value;
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

    public byte[] ReadToEnd() => data[Position..];

    public ushort ReadWord()
    {
        ushort value = *(ushort*)dataPointer;
        dataPointer += 2;
        position += 2;
        return value;
    }

    public byte[] ToArray() => data;
}
