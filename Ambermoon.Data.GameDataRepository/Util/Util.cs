using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Util
{
    using Data;
    using Serialization;
    using System.ComponentModel.DataAnnotations;
    using System.Linq.Expressions;
    using System.Numerics;

    public static class Util
    {
        public static CharacterValue Copy(CharacterValue value)
        {
            return new()
            {
                CurrentValue = value.CurrentValue,
                MaxValue = value.MaxValue,
                BonusValue = value.BonusValue,
                StoredValue = value.StoredValue,
            };
        }

        internal static T EnsureValue<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null) where T : struct
        {
            return value ?? throw new NullReferenceException($"{name} is null.");
        }

        internal static uint[] ReadWordArray(IDataReader dataReader, int length)
        {
            var array = new uint[length];

            for (int i = 0; i < length; i++)
            {
                array[i] = dataReader.ReadWord();
            }

            return array;
        }

        internal static void WriteWordCollection(IDataWriter dataWriter, IEnumerable<uint> values)
        {
            foreach (var value in values)
            {
                dataWriter.Write((ushort)value);
            }
        }

        internal static long CalculateItemPropertySum(IEnumerable<ItemSlotData> itemSlots,
            Func<uint, ItemData> itemProvider, Func<ItemData, ItemSlotFlags, long> itemPropertyProvider,
            Func<long>? emptySlotValueProvider = null)
        {
            emptySlotValueProvider ??= (() => 0);

            return itemSlots.Sum(itemSlot => itemSlot.ItemIndex == 0 || itemSlot.Amount == 0 ? emptySlotValueProvider() : itemSlot.Amount * itemPropertyProvider(itemProvider(itemSlot.ItemIndex), itemSlot.Flags));
        }

        internal static int UnsignedByteToSigned(uint @byte)
        {
            byte b = (byte)(@byte & 0xff);

            if ((b & 0x80) == 0)
                return b;

            return b - 0x100;
        }

        internal static byte SignedToUnsignedByte(int value)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "The given value is out of range.");

            if (value >= 0)
                return (byte)value;

            return unchecked((byte)(value + 0x100));
        }

        internal static int UnsignedWordToSigned(uint word)
        {
            ushort w = (ushort)(word & 0xffff);

            if ((w & 0x8000) == 0)
                return w;

            return w - 0x10000;
        }

        internal static ushort SignedToUnsignedWord(int value)
        {
            if (value < short.MinValue || value > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "The given value is out of range.");

            if (value >= 0)
                return (ushort)value;

            return unchecked((ushort)(value + 0x10000));
        }

        public static bool IsPropertyAdvancedOnly<T, TProperty>(this T _, Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Invalid property expression.", nameof(propertyExpression));
            }

            if (memberExpression.Member is not System.Reflection.PropertyInfo propertyInfo)
            {
                throw new ArgumentException("Expression does not represent a property.", nameof(propertyExpression));
            }

            return System.Attribute.GetCustomAttribute(propertyInfo, typeof(AdvancedOnlyAttribute)) is
                AdvancedOnlyAttribute;
        }

        public static ValueRange<TProperty> GetPropertyValueRange<T, TProperty>(this T _, Expression<Func<T, TProperty>> propertyExpression)
            where TProperty : struct, IMinMaxValue<TProperty>
        {
            if (propertyExpression.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Invalid property expression.", nameof(propertyExpression));
            }

            if (memberExpression.Member is not System.Reflection.PropertyInfo propertyInfo)
            {
                throw new ArgumentException("Expression does not represent a property.", nameof(propertyExpression));
            }

            var rangeAttribute =
                (RangeAttribute?)System.Attribute.GetCustomAttribute(propertyInfo, typeof(RangeAttribute));

            var minimum = rangeAttribute is null
                ? TProperty.MinValue
                : (TProperty)Convert.ChangeType(rangeAttribute.Minimum, typeof(TProperty));
            var maximum = rangeAttribute is null
                ? TProperty.MaxValue
                : (TProperty)Convert.ChangeType(rangeAttribute.Maximum, typeof(TProperty));

            return new ValueRange<TProperty>(minimum, maximum);
        }

        public static ValueRange<TProperty>? GetPropertyValueRange<T, TProperty>(this T _, Expression<Func<T, TProperty?>> propertyExpression)
            where TProperty : struct, IMinMaxValue<TProperty>
        {
            if (propertyExpression.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Invalid property expression.", nameof(propertyExpression));
            }

            if (memberExpression.Member is not System.Reflection.PropertyInfo propertyInfo)
            {
                throw new ArgumentException("Expression does not represent a property.", nameof(propertyExpression));
            }

            var rangeAttribute =
                (RangeAttribute?)System.Attribute.GetCustomAttribute(propertyInfo, typeof(RangeAttribute));

            var minimum = rangeAttribute is null
                ? TProperty.MinValue
                : (TProperty)Convert.ChangeType(rangeAttribute.Minimum, typeof(TProperty));
            var maximum = rangeAttribute is null
                ? TProperty.MaxValue
                : (TProperty)Convert.ChangeType(rangeAttribute.Maximum, typeof(TProperty));

            return new ValueRange<TProperty>(minimum, maximum);
        }
    }
}
