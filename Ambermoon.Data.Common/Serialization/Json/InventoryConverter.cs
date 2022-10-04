using System;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class InventoryConverter : JsonConverter<Inventory>
    {
        public override Inventory ReadJson(JsonReader reader, Type objectType, Inventory existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var inventory = existingValue ?? new Inventory();
            var slots = serializer.Deserialize<ItemSlot[]>(reader);

            if (slots == null || slots.Length != inventory.Slots.Length)
                throw new JsonSerializationException();

            for (int i = 0; i < slots.Length; ++i)
                inventory.Slots[i] = slots[i];

            return inventory;
        }

        public override void WriteJson(JsonWriter writer, Inventory value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Slots);
        }
    }
}
