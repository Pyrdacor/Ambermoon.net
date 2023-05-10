using System;
using System.Linq;

namespace Ambermoon
{
    public static class Enum
    {
        public static TEnum[] GetValues<TEnum>() => (TEnum[])System.Enum.GetValues(typeof(TEnum));

        public static string GetName<TEnum>(TEnum value) => System.Enum.GetName(typeof(TEnum), value);

        public static int NameCount<TEnum>() => System.Enum.GetNames(typeof(TEnum)).Length;

        public static string GetFlagNames<TEnum>(TEnum value, int bytes)
        {
            uint ToUint(TEnum value)
            {
                return bytes switch
                {
                    1 => (byte)Convert.ChangeType(value, typeof(byte)),
                    2 => (ushort)Convert.ChangeType(value, typeof(ushort)),
                    _ => (uint)Convert.ChangeType(value, typeof(uint))
                };
            }

            TEnum FromUint(uint value)
            {
                return bytes switch
                {
                    1 => (TEnum)Convert.ChangeType((byte)value, typeof(TEnum)),
                    2 => (TEnum)Convert.ChangeType((ushort)value, typeof(TEnum)),
                    _ => (TEnum)Convert.ChangeType(value, typeof(TEnum))
                };
            }

            var values = GetValues<TEnum>().Select(v => ToUint(v)).Where(v => v != 0).Distinct().ToList();
            uint flags = ToUint(value);

            if (values.Count == 0 || flags == 0)
            {
                try
                {
                    return GetName((TEnum)(object)0) ?? "None";
                }
                catch (InvalidCastException)
                {
                    return "None";
                }
            }

            string result = "";
            values.Sort();

            foreach (var v in values)
            {
                if ((flags & v) == 0)
                    continue;

                if (result.Length != 0)
                    result += " | ";
                result += GetName(FromUint(v));
            }

            return result;
        }

        public static string GetFlagNames(Type enumType, object value)
        {
            var values = System.Enum.GetValues(enumType).Cast<object>().Select(v => (uint)Convert.ChangeType(v, typeof(uint))).Where(v => v != 0).Distinct().ToList();

            if (values.Count == 0 || (uint)Convert.ChangeType(value, typeof(uint)) == 0)
            {
                try
                {
                    return System.Enum.GetName(enumType, (object)0) ?? "None";
                }
                catch (InvalidCastException)
                {
                    return "None";
                }
            }

            string result = "";
            values.Sort();
            uint flags = (uint)Convert.ChangeType(value, typeof(uint));

            foreach (var v in values)
            {
                if ((flags & v) == 0)
                    continue;

                if (result.Length != 0)
                    result += " | ";
                result += System.Enum.GetName(enumType, v);
            }

            return result;
        }
    }
}
