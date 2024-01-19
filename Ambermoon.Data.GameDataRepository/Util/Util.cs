using Ambermoon.Data.GameDataRepository.Data;
using Ambermoon.Data.Serialization;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Util
{
    internal static class Util
    {
        public static T EnsureValue<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null) where T : struct
        {
            return value ?? throw new NullReferenceException($"{name} is null.");
        }

        public static uint[] ReadWordArray(IDataReader dataReader, int length)
        {
            var array = new uint[length];

            for (int i = 0; i < length; i++)
            {
                array[i] = dataReader.ReadWord();
            }

            return array;
        }

        public static void WriteWordCollection(IDataWriter dataWriter, IEnumerable<uint> values)
        {
            foreach (var value in values)
            {
                dataWriter.Write((ushort)value);
            }
        }

        public static uint CalculateItemPropertySum<T>(IEnumerable<ItemSlotData> itemSlots,
            Func<uint, ItemData> itemProvider, Func<ItemData, uint> itemPropertyProvider)
        {
            // TODO: cursed items (equip only)
            return (uint)itemSlots.Sum(itemSlot => itemSlot.Amount * itemPropertyProvider(itemProvider(itemSlot.ItemIndex)));
        }

        public static int UnsignedByteToSigned(uint @byte)
        {
            byte b = (byte)(@byte & 0xff);

            if ((b & 0x80) == 0)
                return b;

            return b - 0x100;
        }

        public static byte SignedToUnsignedByte(int value)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "The given value is out of range.");

            if (value >= 0)
                return (byte)value;

            return unchecked((byte)(value + 0x100));
        }

        public static int UnsignedWordToSigned(uint word)
        {
            ushort w = (ushort)(word & 0xffff);

            if ((w & 0x8000) == 0)
                return w;

            return w - 0x10000;
        }

        public static ushort SignedToUnsignedWord(int value)
        {
            if (value < short.MinValue || value > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "The given value is out of range.");

            if (value >= 0)
                return (ushort)value;

            return unchecked((ushort)(value + 0x10000));
        }
    }
}
