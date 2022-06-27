using System;
using System.Linq;

namespace Ambermoon
{
    public static class Enum
    {
        public static TEnum[] GetValues<TEnum>() => (TEnum[])System.Enum.GetValues(typeof(TEnum));

        public static string GetName<TEnum>(TEnum value) => System.Enum.GetName(typeof(TEnum), value);

        public static int NameCount<TEnum>() => System.Enum.GetNames(typeof(TEnum)).Length;

        public static string GetFlagNames<TEnum>(TEnum value)
        {
            var values = GetValues<TEnum>().Select(v => (uint)Convert.ChangeType(v, typeof(uint))).Where(v => v != 0).Distinct().ToList();
            uint flags = (uint)Convert.ChangeType(value, typeof(uint));

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
                result += GetName(v);
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
