using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace AmbermoonSerialize
{
    public class Serializer
    {
        public virtual void Write(JsonWriter writer,
            object? value, object? parent, string? format,
            Serializer? itemSerializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            switch (value)
            {
                case bool b:
                    writer.WriteValue(b);
                    break;
                case byte by:
                    writer.WriteValue(by, format);
                    break;
                case sbyte sb:
                    writer.WriteValue(sb, format);
                    break;
                case short s:
                    writer.WriteValue(s, format);
                    break;
                case ushort us:
                    writer.WriteValue(us, format);
                    break;
                case int i:
                    writer.WriteValue(i, format);
                    break;
                case uint ui:
                    writer.WriteValue(ui, format);
                    break;
                case long l:
                    writer.WriteValue(l, format);
                    break;
                case ulong ul:
                    writer.WriteValue(ul, format);
                    break;
                case float f:
                    writer.WriteValue(f, format);
                    break;
                case double d:
                    writer.WriteValue(d, format);
                    break;
                case char ch:
                    writer.WriteValue(ch.ToString());
                    break;
                case string str:
                    writer.WriteValue(str);
                    break;
                case Enum e:
                    writer.WriteEnumAsString(e);
                    break;
                case IDictionary dict:
                    {
                        writer.WriteStartObject();
                        foreach (var key in dict.Keys)
                        {
                            writer.WritePropertyName(key.ToString()!);
                            (itemSerializer ?? this).Write(writer, dict[key], dict, format, null);
                        }
                        writer.WriteEndObject();
                        break;
                    }
                case IEnumerable collection:
                    writer.WriteCollection(collection, itemSerializer ?? this, format);
                    break;
                default:
                    {
                        writer.WriteStartObject();
                        var type = value.GetType();
                        foreach (var property in type.GetProperties().Where(p => p.CanRead && !p.Ignore() && p.CanWriteExtended()))
                        {
                            writer.WritePropertyName(property.Name);
                            string? propertyFormat = property.GetFormat(ref value);
                            Write(writer, property.GetValue(value, null), value, propertyFormat, itemSerializer);
                        }
                        writer.WriteEndObject();
                        break;
                    }
            }
        }

        public virtual object? Read(JsonReader reader,
            object? parent, string? format, Serializer? itemSerializer)
        {
            // TODO
            return null;
        }
    }
}