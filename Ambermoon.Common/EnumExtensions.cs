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
            var values = GetValues<TEnum>().Select(v => (uint)(object)v).Where(v => v != 0).Distinct().ToList();

            if (values.Count == 0)
                return GetName((TEnum)(object)0) ?? "None";

            string result = "";
            values.Sort();
            uint flags = (uint)(object)value;

            foreach (var v in values)
            {
                if ((flags & v) == 0)
                    continue;

                if (result.Length != 0)
                    result += " | ";
                result += GetName(value);
            }

            return result;
        }
    }
}
