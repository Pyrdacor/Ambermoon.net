using System;
using System.Collections;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class CollectionConverter<T> : JsonConverter<T> where T : ICollection
    {
        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (existingValue == null)
                throw new JsonSerializationException();
            
            var collection = serializer.Deserialize<ICollection>(reader);

            if (existingValue is IList list)
            {
                list.Clear();

                foreach (var item in collection)
                    list.Add(item);
            }
            else if (existingValue is Array array)
            {
                if (array.Length != collection.Count)
                    throw new JsonSerializationException();

                int index = 0;

                foreach (var item in collection)
                    array.SetValue(item, index++);
            }
            else
            {
                throw new JsonSerializationException();
            }

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
