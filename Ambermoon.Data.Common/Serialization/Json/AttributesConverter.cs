using System;
using AmbermoonSerialize;
using Newtonsoft.Json;

namespace Ambermoon.Data.Serialization.Json
{
    public class AttributesConverter : Serializer
    {
        public override object? Read(JsonReader reader,
            object parent, string format, Serializer itemSerializer)
        {
            var collection = new CharacterValueCollection<Attribute>();
            itemSerializer ??= new CharacterValueConverter(4);

            for (int i = 0; i < 8; ++i)
            {
                collection[(Attribute)i] = Read<CharacterValue>(reader, collection, format, itemSerializer);
            }

            uint currentAge = Helper.ReadPrimitveProperty<uint>(reader, "CurrentAge");
            uint maxAge = Helper.ReadPrimitveProperty<uint>(reader, "MaxAge");

            collection[Attribute.Age] = new CharacterValue()
            {
                CurrentValue = currentAge,
                MaxValue = maxAge
            };
            collection[Attribute.Unknown] = new CharacterValue();

            return collection;
        }

        public override void Write(JsonWriter writer,
            object value, object parent, string format,
            Serializer itemSerializer)
        {
            itemSerializer ??= new CharacterValueConverter(4);
            var collection = value as CharacterValueCollection<Attribute>;

            writer.WriteStartObject();

            for (int i = 0; i < 8; ++i)
            {
                writer.WritePropertyName(Enum.GetName((Attribute)i));
                itemSerializer.Write(writer, collection[(Attribute)i], collection, format, itemSerializer);
            }

            writer.WriteEndObject();

            writer.WriteProperty("CurrentAge", collection[Attribute.Age].CurrentValue);
            writer.WriteProperty("MaxAge", collection[Attribute.Age].MaxValue);
        }
    }
}
