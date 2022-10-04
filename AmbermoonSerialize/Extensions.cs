using System.Reflection;

namespace AmbermoonSerialize
{
    internal static class Extensions
    {
        public static T? GetJsonAttribute<T>(this object obj) where T : Attribute, new()
        {
            return obj.GetType().GetCustomAttribute(typeof(T)) as T;
        }

        public static T? GetJsonAttribute<T>(this object obj, string property) where T : Attribute, new()
        {
            return GetJsonAttribute<T>(obj.GetType().GetProperty(property)!);
        }

        private static T? GetJsonAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute, new()
        {
            return propertyInfo.GetCustomAttribute(typeof(T)) as T;
        }

        public static bool Ignore(this PropertyInfo propertyInfo)
        {
            return GetJsonAttribute<JsonIgnoreAttribute>(propertyInfo) != null;
        }

        public static bool CanWriteExtended(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.CanWrite)
                return true;

            return GetJsonAttribute<JsonPropertyAttribute>(propertyInfo) != null;
        }

        public static string? GetIntegerFormat(this IntegerFormat integerFormat, ref object value)
        {
            if (integerFormat == IntegerFormat.Default)
                return "{0}";

            Func<int, string> formatStringProvider;

            if (integerFormat.HasFlag(IntegerFormat.Hex))
            {
                formatStringProvider = bytes => "${0:x" + (bytes * 2).ToString() + "}";
            }
            else if (integerFormat.HasFlag(IntegerFormat.Binary))
            {
                long integer = value is ulong ul ? unchecked((long)ul) : Convert.ToInt64(value);
                value = Convert.ToString(integer, 2);
                formatStringProvider = _ => "#{0}";
            }
            else
            {
                formatStringProvider = _ => "{0}";
            }

            switch ((IntegerFormat)((uint)integerFormat & 0x00f0))
            {
                case IntegerFormat.Byte:
                    return formatStringProvider(1) + "_" +
                        (integerFormat.HasFlag(IntegerFormat.Unsigned) ? "_ub" : "_b");
                case IntegerFormat.Short:
                    return formatStringProvider(2) + "_" +
                        (integerFormat.HasFlag(IntegerFormat.Unsigned) ? "_us" : "_s");
                case IntegerFormat.Long:
                    return formatStringProvider(4) +
                        (integerFormat.HasFlag(IntegerFormat.Unsigned) ? "_ul" : "_l");
                case IntegerFormat.Int:
                default:
                    return formatStringProvider(8) +
                        (integerFormat.HasFlag(IntegerFormat.Unsigned) ? "_u" : "");
            }
        }

        public static string? GetFormat(this PropertyInfo propertyInfo, ref object value)
        {
            var attribute = GetJsonAttribute<JsonPropertyAttribute>(propertyInfo);

            return (attribute?.IntegerFormat)?.GetIntegerFormat(ref value);
        }
    }
}
