using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor;

// If the value is <= short.MaxValue, it is stored as is.
// Otherwise it is stored as 0x80000000 | value.
// Value must not exceed int.MaxValue and should never be negative.
internal class EncodedInt
{
    public static void Write(IDataWriter dataWriter, int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be >= 0.");

        if (value <= short.MaxValue)
            dataWriter.Write((ushort)value);
        else
            dataWriter.Write((uint)(0x8000_0000 | (long)value));
    }

    public static int Read(IDataReader dataReader)
    {
        if ((dataReader.PeekWord() & 0x8000) == 0)
            return dataReader.ReadWord();
        else
            return (int)(dataReader.ReadDword() & 0x7fff_ffff);
    }
}
