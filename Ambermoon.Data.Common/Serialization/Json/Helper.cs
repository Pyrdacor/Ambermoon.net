using AmbermoonSerialize;
using System;

namespace Ambermoon.Data.Serialization.Json
{
    internal static class Helper
    {
        private static object InternalReadPrimitiveProperty(JsonReader reader, string name)
        {
            if (reader.TokenType != JsonToken.PropertyName || reader.ReadAsString() != name)
                throw new JsonSerializationException();

            if (reader.TokenType == JsonToken.String)
                return reader.ReadAsString();
            else if (reader.TokenType == JsonToken.Integer)
                return reader.ReadAsInt32();
            else if (reader.TokenType == JsonToken.Float)
                return reader.ReadAsDouble();
            else if (reader.TokenType == JsonToken.Boolean)
                return reader.ReadAsBoolean();
            else
                throw new JsonSerializationException();
        }

        public static T ReadPrimitveProperty<T>(JsonReader reader, string name)
        {
            return (T)Convert.ChangeType(InternalReadPrimitiveProperty(reader, name), typeof(T));
        }
    }
}
