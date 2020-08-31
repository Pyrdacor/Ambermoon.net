namespace Ambermoon
{
    public static class Enum
    {
        public static TEnum[] GetValues<TEnum>()
        {
            return (TEnum[])System.Enum.GetValues(typeof(TEnum));
        }

        public static string GetName<TEnum>(TEnum value)
        {
            return System.Enum.GetName(typeof(TEnum), value);
        }
    }
}
