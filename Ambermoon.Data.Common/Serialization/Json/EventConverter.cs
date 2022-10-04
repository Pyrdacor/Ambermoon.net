using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Ambermoon.Data.Serialization.Json
{
    public class EventConverter : JsonConverter<Event>
    {
        public override Event ReadJson(JsonReader reader, Type objectType, Event existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            /*if (reader.TokenType == JsonToken.StartObject)
            {
                var characterValue = existingValue ?? new CharacterValue();
                JObject item = JObject.Load(reader);

                characterValue.CurrentValue = item["CurrentValue"].Value<uint>();
                characterValue.MaxValue = item["MaxValue"].Value<uint>();
                characterValue.BonusValue = item["BonusValue"].Value<int>();

                if (numValues == 4)
                    characterValue.StoredValue = item["StoredValue"].Value<uint>();

                return characterValue;
            }*/

            // TODO

            throw new JsonSerializationException(); // TODO?
        }

        public override void WriteJson(JsonWriter writer, Event value, JsonSerializer serializer)
        {
            JObject obj = JObject.FromObject(value, new JsonSerializer { ContractResolver = new IgnoreConverterContractResolver() });

            //obj.Add("EventList", JToken.FromObject(value.EventList.Select(e => value.Events.IndexOf(e)).ToArray()));
            obj.WriteTo(writer);

            // TODO

            /*writer.WriteStartObject();

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

            writer.WriteEndObject();*/
        }
    }
}
