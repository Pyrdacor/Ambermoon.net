namespace AmbermoonSerialize
{
    [Flags]
    public enum IntegerFormat
    {
        Default = 0,

        Signed = 0x0001,
        Unsigned = 0x0002,

        Byte = 0x0010,
        Short = 0x0020,
        Long = 0x0040,
        Int = 0x0080,

        Decimal = 0x0100,
        Hex = 0x0200,
        Binary = 0x0400
    }

    public class JsonPropertyAttribute : Attribute
    {
        public Type? Serializer { get; set; }
        public object? SerializerArgument { get; set; }
        public IntegerFormat IntegerFormat { get; set; } = IntegerFormat.Default;
        public string? Format { get; set; }
    }
}
