using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Extensions;

internal static class SerializationExtensions
{
    public static TEnum ReadEnum8<TEnum>(this IDataReader dataReader)
        where TEnum : struct, Enum
    {
        var value = dataReader.ReadByte();

        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    public static TEnum ReadEnum16<TEnum>(this IDataReader dataReader)
        where TEnum : struct, Enum
    {
        var value = dataReader.ReadWord();

        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    public static void WriteEnum8<TEnum>(this IDataWriter dataWriter, TEnum value)
        where TEnum : struct, Enum
    {
        dataWriter.Write(Convert.ToByte(value));
    }

    public static void WriteEnum16<TEnum>(this IDataWriter dataWriter, TEnum value)
        where TEnum : struct, Enum
    {
        dataWriter.Write(Convert.ToUInt16(value));
    }
}
