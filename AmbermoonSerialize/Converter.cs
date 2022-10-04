namespace AmbermoonSerialize
{
    public static class Converter
    {
        public static string Serialize<T>(T value, bool pretty) where T : class
        {
            using var writer = new StringWriter();
            Serialize(writer, value, pretty);
            return writer.ToString();
        }

        public static void Serialize<T>(StringWriter stringWriter, T value, bool pretty) where T : class
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var jsonWriter = new JsonWriter(stringWriter, pretty);
            var propertyAttribute = value.GetJsonAttribute<JsonPropertyAttribute>();
            var serializer = propertyAttribute?.Serializer ?? new Serializer();
            serializer.Write(jsonWriter, value, null, null, null);
        }
    }
}
