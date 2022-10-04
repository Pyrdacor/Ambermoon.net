using AmbermoonSerialize;
using System;

namespace Ambermoon.Data.Serialization.Json
{
    public class CharacterValueConverter : Serializer
    {
        readonly int numValues;

        public CharacterValueConverter(int numValues)
        {
            if (numValues < 3 || numValues > 4)
                throw new ArgumentOutOfRangeException(nameof(numValues));

            this.numValues = numValues;
        }

        public override object? Read(JsonReader reader,
            object parent, string format, Serializer itemSerializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var characterValue = existingValue ?? new CharacterValue();
                JObject item = JObject.Load(reader);

                characterValue.CurrentValue = item["CurrentValue"].Value<uint>();
                characterValue.MaxValue = item["MaxValue"].Value<uint>();
                characterValue.BonusValue = item["BonusValue"].Value<int>();

                if (numValues == 4)
                    characterValue.StoredValue = item["StoredValue"].Value<uint>();

                return characterValue;
            }

            throw new JsonSerializationException(); // TODO?
        }

        public override void WriteJson(JsonWriter writer, CharacterValue value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            void WriteProperty<T>(string name, T value)
            {
                writer.WritePropertyName(name);
                writer.WriteValue(value);
            }

            WriteProperty(nameof(value.CurrentValue), value.CurrentValue);
            WriteProperty(nameof(value.MaxValue), value.MaxValue);
            WriteProperty(nameof(value.BonusValue), value.BonusValue);

            if (numValues == 4)
                WriteProperty(nameof(value.StoredValue), value.StoredValue);

            writer.WriteEndObject();
        }
    }
}
