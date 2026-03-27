using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor;

// If the value is <= sbyte.MaxValue, it is stored as is.
// Otherwise it is stored as 0x8000 | value.
// Value must not exceed short.MaxValue and should never be negative.
internal class SmallEncodedInt
{
    public static void Write(IDataWriter dataWriter, short value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be >= 0.");

        if (value <= sbyte.MaxValue)
            dataWriter.Write((byte)value);
        else
            dataWriter.Write((ushort)(0x8000 | (int)value));
    }

    public static int Read(IDataReader dataReader)
    {
        if ((dataReader.PeekByte() & 0x80) == 0)
            return dataReader.ReadByte();
        else
            return (int)(dataReader.ReadWord() & 0x7fff);
    }
}
