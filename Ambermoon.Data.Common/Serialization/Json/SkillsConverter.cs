using System;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class SkillsConverter : JsonConverter<CharacterValueCollection<Skill>>
    {
        public override CharacterValueCollection<Skill> ReadJson(JsonReader reader, Type objectType, CharacterValueCollection<Skill> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var collection = existingValue ?? new CharacterValueCollection<Skill>();

            serializer.Converters.Add(new CharacterValueConverter(4));

            for (int i = 0; i < 10; ++i)
            {
                collection[(Skill)i] = serializer.Deserialize<CharacterValue>(reader);
            }

            return collection;
        }

        public override void WriteJson(JsonWriter writer, CharacterValueCollection<Skill> value, JsonSerializer serializer)
        {
            serializer.Converters.Add(new CharacterValueConverter(4));

            writer.WriteStartObject();

            for (int i = 0; i < 10; ++i)
            {
                writer.WritePropertyName(Enum.GetName((Skill)i));
                serializer.Serialize(writer, value[(Skill)i]);
            }

            writer.WriteEndObject();
        }
    }
}
