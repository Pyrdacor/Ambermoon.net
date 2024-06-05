using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ambermoon.Data.Serialization
{
    [Serializable]
    public class CharacterValueCollectionConverter<TType> :JsonConverter<CharacterValueCollection<TType>> where TType : Enum
    {
		public override CharacterValueCollection<TType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				throw new JsonException();
			}

			var values = new List<CharacterValue>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
				{
					return new CharacterValueCollection<TType>(values.ToArray());
				}

				var value = JsonSerializer.Deserialize<CharacterValue>(ref reader, options);
				values.Add(value);
			}

			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, CharacterValueCollection<TType> value, JsonSerializerOptions options)
		{
			writer.WriteStartArray();

			foreach (var characterValue in value)
			{
				JsonSerializer.Serialize(writer, characterValue, options);
			}

			writer.WriteEndArray();
		}
    }
}
