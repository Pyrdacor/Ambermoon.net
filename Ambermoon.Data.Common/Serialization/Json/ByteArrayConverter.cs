using System;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class ByteArrayConverter : JsonConverter<byte[]>
    {
        public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                reader.Read();
                return null;
            }
            else
            {
                //TODO
                return null;
            }

            /*var collection = existingValue ?? new CharacterValueCollection<Skill>();

            serializer.Converters.Add(new CharacterValueConverter(4));

            for (int i = 0; i < 10; ++i)
            {
                collection[(Skill)i] = serializer.Deserialize<CharacterValue>(reader);
            }

            return collection;*/
        }

        public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            foreach (var item in value)
                writer.WriteValue("0x" + item.ToString("x2"));

            writer.WriteEndArray();
        }
    }
}
