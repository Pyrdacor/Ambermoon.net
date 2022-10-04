using System.Collections;
using System.Linq;

namespace AmbermoonSerialize
{
    public class JsonWriter
    {
        private readonly TextWriter writer;
        private readonly bool pretty;
        private int level = 0;
        private int lineSize = 0;

        public JsonWriter(TextWriter writer, bool pretty)
        {
            this.writer = writer;
            this.pretty = pretty;
        }

        public void Write(string value)
        {
            writer.Write(value);
            lineSize += value.Length;
        }

        private void Indent()
        {
            if (pretty)
                Write(new string('\t', level));
        }

        private void Newline()
        {
            if (pretty)
            {
                writer.WriteLine();
                lineSize = 0;
            }
        }

        public void WriteValue(string str)
        {
            Write("\"" + str + "\"");
        }

        public void WriteValue(bool b)
        {
            Write(b ? "true" : "false");
        }

        public void WriteValue(byte b, IntegerFormat? format = null)
        {
            object value = (uint)b;
            string fmt = format?.GetIntegerFormat(ref value) ?? "${0:x2}_ub";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(sbyte b, IntegerFormat? format = null)
        {
            object value = (int)b;
            string fmt = format?.GetIntegerFormat(ref value) ?? "{0}_b";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(ushort s, IntegerFormat? format = null)
        {
            object value = (uint)s;
            string fmt = format?.GetIntegerFormat(ref value) ?? "${0:x4}_us";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(short s, IntegerFormat? format = null)
        {
            object value = (int)s;
            string fmt = format?.GetIntegerFormat(ref value) ?? "{0}_s";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(uint i, IntegerFormat? format = null)
        {
            object value = i;
            string fmt = format?.GetIntegerFormat(ref value) ?? "${0:x8}_u";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(int i, IntegerFormat? format = null)
        {
            object value = i;
            string fmt = format?.GetIntegerFormat(ref value) ?? "{0}";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(ulong l, IntegerFormat? format = null)
        {
            object value = l;
            string fmt = format?.GetIntegerFormat(ref value) ?? "${0:x16}_ul";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(long l, IntegerFormat? format = null)
        {
            object value = l;
            string fmt = format?.GetIntegerFormat(ref value) ?? "{0}_l";
            Write(string.Format(fmt, value));
        }

        public void WriteValue(float f)
        {
            Write($"{f:0.00}_f");
        }

        public void WriteValue(double f)
        {
            Write($"{f:0.00}");
        }

        public void WriteValue<T>(T value, IntegerFormat? format = null)
        {
            switch (value)
            {
                case string str:
                    WriteValue(str);
                    break;
                case bool b:
                    WriteValue(b);
                    break;
                case byte by:
                    WriteValue(by, format);
                    break;
                case sbyte sb:
                    WriteValue(sb, format);
                    break;
                case ushort us:
                    WriteValue(us, format);
                    break;
                case short s:
                    WriteValue(s, format);
                    break;
                case uint ui:
                    WriteValue(ui, format);
                    break;
                case int i:
                    WriteValue(i, format);
                    break;
                case ulong ul:
                    WriteValue(ul, format);
                    break;
                case long l:
                    WriteValue(l, format);
                    break;
                case float f:
                    WriteValue(f);
                    break;
                case double d:
                    WriteValue(d);
                    break;
                case null:
                    WriteNull();
                    break;
                default:
                    Write(value.ToString()!);
                    break;
            }
        }

        public void WriteStartObject()
        {
            Write("{");
            ++level;
            Newline();
        }

        public void WriteEndObject()
        {
            Newline();
            --level;
            Indent();
            Write("}");
        }

        public void WriteStartArray(bool singleLine)
        {
            Write("[");
            if (!singleLine)
            {
                ++level;
                Newline();
            }
        }

        public void WriteEndArray(bool singleLine)
        {
            if (!singleLine)
            {
                Newline();
                --level;
                Indent();
            }
            else if (pretty)
            {
                Write(" ");
            }
            Write("]");
        }

        public void WritePropertyName(string name)
        {
            Write(name + ":");
            if (pretty)
                Write(" ");
        }

        public void WriteProperty(string name, string value)
        {
            WritePropertyName(name);
            Write(value);
            WriteSeparator(false);
        }

        public void WriteProperty<T>(string name, T value)
        {
            WritePropertyName(name);
            WriteValue(value);
            WriteSeparator(false);
        }

        internal void WriteSeparator(bool collection)
        {
            Write(",");

            if (pretty)
            {
                if (collection)
                {
                    if (lineSize < 80)
                    {
                        Write(" ");
                    }
                    else
                    {
                        Newline();
                    }
                }
                else
                {
                    Newline();
                }
            }
        }

        public void WriteNull()
        {
            Write("null");
        }

        public void WriteCollection(IEnumerable collection, Serializer itemSerializer, string? format)
        {
            bool singleLine = false;

            bool IsObjectOrArrayCollection()
            {
                var type = collection.GetType();

                if (!type.IsGenericType)
                    return true;

                var itemType = type.GetGenericArguments()[0];
                
                return itemType != typeof(string) && !itemType.IsPrimitive && !itemType.IsEnum;
            }
            
            if (collection is IList list)
            {
                if (list.Count <= 16 && !IsObjectOrArrayCollection())
                    singleLine = true;
            }
            else if (collection is ICollection coll)
            {
                if (coll.Count <= 16 && !IsObjectOrArrayCollection())
                    singleLine = true;
            }

            WriteStartArray(singleLine);

            bool first = true;

            foreach (var item in collection)
            {
                if (first)
                    first = false;
                else
                    WriteSeparator(true);

                itemSerializer.Write(this, item, collection, format, itemSerializer);
            }

            WriteEndArray(singleLine);
        }

        public void WriteEnumAsNumber(Enum e)
        {
            var type = e.GetType();

            if (type.GetCustomAttributes(typeof(FlagsAttribute), true).Any())
            {
                ulong flags = 0;
                var enumType = Enum.GetUnderlyingType(type);
                int bytes = 4;
                if (enumType == typeof(byte) || enumType == typeof(sbyte))
                    bytes = 1;
                else if (enumType == typeof(short) || enumType == typeof(ushort))
                    bytes = 2;
                else if (enumType == typeof(long) || enumType == typeof(ulong))
                    bytes = 4;

                for (int i = 0; i < bytes * 8; ++i)
                {
                    ulong flag = (1ul << i);

                    if (e.HasFlag((Enum)(object)flag))
                        flags |= (ulong)Convert.ChangeType(e, enumType);
                }

                switch (bytes)
                {
                    case 1:
                        WriteValue((byte)flags);
                        break;
                    case 2:
                        WriteValue((ushort)flags);
                        break;
                    case 4:
                    default:
                        WriteValue((uint)flags);
                        break;
                    case 8:
                        WriteValue(flags);
                        break;
                }
            }
            else
            {
                Write(Convert.ChangeType(e, Enum.GetUnderlyingType(type)).ToString()!);
            }
        }

        public void WriteEnumAsString(Enum e)
        {
            var type = e.GetType();

            if (type.GetCustomAttributes(typeof(FlagsAttribute), true).Any())
            {
                var flags = new List<string>();

                for (int i = 0; i < 64; ++i)
                {
                    ulong flag = (1ul << i);

                    if (e.HasFlag((Enum)(object)flag))
                        flags.Add(Enum.GetName(type, flag) ?? Convert.ChangeType(e, Enum.GetUnderlyingType(type)).ToString()!);
                }

                if (flags.Count == 0)
                    Write(Enum.GetName(type, 0) ?? "None");
                else
                    Write(string.Join(pretty ? " | " : "|", flags));
            }
            else
            {
                Write(Enum.GetName(type, e) ?? Convert.ChangeType(e, Enum.GetUnderlyingType(type)).ToString()!);
            }
        }
    }
}