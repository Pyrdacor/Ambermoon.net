namespace Ambermoon
{
    public static class Enum
    {
        public static TEnum[] GetValues<TEnum>() => (TEnum[])System.Enum.GetValues(typeof(TEnum));

        public static string GetName<TEnum>(TEnum value) => System.Enum.GetName(typeof(TEnum), value);

        public static int NameCount<TEnum>() => System.Enum.GetNames(typeof(TEnum)).Length;
    }
}
