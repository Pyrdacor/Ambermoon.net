namespace Ambermoon.Data.GameDataRepository.Collections;

using Serialization;
using Util;

internal static class Extensions
{
    public static int ReadSignedByte(this IDataReader reader)
    {
        return Util.UnsignedByteToSigned(reader.ReadByte());
    }

    public static void WriteSignedByte(this IDataWriter writer, int value)
    {
        writer.Write(Util.SignedToUnsignedByte(value));
    }

    public static int ReadSignedWord(this IDataReader reader)
    {
        return Util.UnsignedWordToSigned(reader.ReadWord());
    }

    public static void WriteSignedWord(this IDataWriter writer, int value)
    {
        writer.Write(Util.SignedToUnsignedWord(value));
    }
}
