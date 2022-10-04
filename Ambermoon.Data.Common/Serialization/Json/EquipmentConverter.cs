using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class EquipmentConverter : JsonConverter<Equipment>
    {
        public override Equipment ReadJson(JsonReader reader, Type objectType, Equipment existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var equipment = existingValue ?? new Equipment();
            var slots = serializer.Deserialize<Dictionary<EquipmentSlot, ItemSlot>>(reader);

            if (slots == null || slots.Count != equipment.Slots.Count)
                throw new JsonSerializationException();

            foreach (var slot in slots)
                equipment.Slots[slot.Key] = slot.Value;

            return equipment;
        }

        public override void WriteJson(JsonWriter writer, Equipment value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Slots);
        }
    }
}
