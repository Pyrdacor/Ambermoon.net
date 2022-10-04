using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace AmbermoonSerialize
{
    public class JsonReader
    {
        // TODO
        private readonly TextReader reader;
        private int level = 0;
        private int lineSize = 0;

        public JsonReader(TextReader reader)
        {
            this.reader = reader;
        }

        public enum TokenType
        {
            None,
            Null,
            StartElement,
            EndElement,
            StartArray,
            EndArray,
            String,
            Bool,
            Byte,
            SByte,
            UShort,
            Short,
            UInt,
            Int,
            ULong,
            Long,
            Float,
            Double,
            Separator
        }

        private static readonly Dictionary<string, TokenType> TokensBySymbol = new()
        {
            { "{", TokenType.StartElement },
            { "}", TokenType.EndElement },
            { "[", TokenType.StartArray },
            { "]", TokenType.EndArray },
            { ",", TokenType.Separator }
        };

        public class Token
        {
            public TokenType Type = TokenType.None;
            public string Value = "";
            internal bool Processed = false;
        }

        public void SkipToken() => SkipTokens(1);

        public void SkipTokens(int amount)
        {
            for (int i = 0; i < amount; ++i)
                ReadToken();
        }

        public Token ReadToken(IntegerFormat? expectedFormat)
        {
            int ch;
            var token = new Token();

            void ProcessToken()
            {
                if (token.Value == "null")
                    token.Type = TokenType.Null;
                else if (token.Value == "true")
                    token.Type = TokenType.Bool;
                else if (token.Value == "false")
                    token.Type = TokenType.Bool;
                else if (token.Value.StartsWith("\""))
                {
                    if (!token.Value.EndsWith("\""))
                        throw new FormatException("Invalid JSON: Missing end of string.");

                    token.Type = TokenType.String;
                }
                else if (token.Value.StartsWith("'"))
                {
                    if (!token.Value.EndsWith("'"))
                        throw new FormatException("Invalid JSON: Missing end of string.");

                    token.Type = TokenType.String;
                    token.Value = "\"" + token.Value.Substring(1, token.Value.Length - 2) + "\"";
                }
                else if (token.Value.Contains("."))
                {

                }
                else
                {
                    var match = Regex.Match(token.Value, "[0-9]")
                }                    

                token.Processed = true;
            }

            bool ProcessChar(int ch)
            {
                if (token.Value.Length == 0)
                {
                    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                    {
                        reader.Read();
                        return false;
                    }

                    token.Value += (char)reader.Read();

                    if (TokensBySymbol.TryGetValue(token.Value, out var type))
                    {
                        token.Processed = true;
                        token.Type = type;
                        return true;
                    }
                }
                else
                {
                    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                    {
                        ProcessToken();
                        return true;
                    }
                    else if (TokensBySymbol.TryGetValue(new string(new char[1] { (char)ch }), out var type))
                    {
                        ProcessToken();
                        return true;
                    }

                    token.Value += (char)reader.Read();
                }

                return false;
            }

            while ((ch = reader.Peek()) != -1)
            {
                if (ProcessChar(ch))
                    break;
            }

            if (token.Value.Length != 0 && !token.Processed)
                ProcessToken();

            return token;
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
                reader.WriteLine();
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

        public void WriteValue(byte b, string? format = null)
        {
            Write(string.Format(format ?? "${0:x2}_ub", (uint)b));
        }

        public void WriteValue(sbyte b, string? format = null)
        {
            Write(string.Format(format ?? "{0}_b", (int)b));
        }

        public void WriteValue(ushort s, string? format = null)
        {
            Write(string.Format(format ?? "${0:x4}_us", (uint)s));
        }

        public void WriteValue(short s, string? format = null)
        {
            Write(string.Format(format ?? "{0}_s", (int)s));
        }

        public void WriteValue(uint i, string? format = null)
        {
            Write(string.Format(format ?? "${0:x8}_u", i));
        }

        public void WriteValue(int i, string? format = null)
        {
            Write(string.Format(format ?? "{0}", i));
        }

        public void WriteValue(ulong l, string? format = null)
        {
            Write(string.Format(format ?? "${0:x16}_ul", l));
        }

        public void WriteValue(long l, string? format = null)
        {
            Write(string.Format(format ?? "{0}_l", l));
        }

        public void WriteValue(float f, string? format = null)
        {
            Write(string.Format(format ?? "{0.0}_f", f));
        }

        public void WriteValue(double f, string? format = null)
        {
            Write(string.Format(format ?? "{0.0}", f));
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
                int digits = 16;
                if (enumType == typeof(byte) || enumType == typeof(sbyte))
                    digits = 2;
                else if (enumType == typeof(short) || enumType == typeof(ushort))
                    digits = 4;
                else if (enumType == typeof(int) || enumType == typeof(uint))
                    digits = 8;

                for (int i = 0; i < 64; ++i)
                {
                    ulong flag = (1ul << i);

                    if (e.HasFlag((Enum)(object)flag))
                        flags |= (ulong)Convert.ChangeType(e, enumType);
                }

                WriteValue(flags, "${0:x" + digits.ToString() + "}");
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