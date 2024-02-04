using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Ambermoon.Data.Legacy
{
    public class AmbermoonEncoding : Encoding
    {
        // See https://gitlab.com/ambermoon/research/-/wikis/font
        static readonly Encoding BaseEncoding = GetEncoding("iso-8859-1");
        static readonly ImmutableDictionary<char, byte> CharsToBytes = new Dictionary<char, byte>
        {
            { '\u00fc', 0x81 }, // ü
            { '\u00e9', 0x82 }, // é
            { '\u00e2', 0x83 }, // â
            { '\u00e4', 0x84 }, // ä
            { '\u00e0', 0x85 }, // à
            { '\u00e7', 0x87 }, // ç
            { '\u00ea', 0x88 }, // ê
            { '\u00e8', 0x8a }, // è
            { '\u00c4', 0x8e }, // Ä
            { '\u00c9', 0x90 }, // É
            { '\u00f6', 0x94 }, // ö
            { '\u00fb', 0x96 }, // û
            { '\u00d6', 0x99 }, // Ö
            { '\u00dc', 0x9a }, // Ü
            { '\u00a2', 0x9b }, // ¢
            { '\u00df', 0x9e }, // ß
            { '\u00e1', 0xa0 }, // á
            { '\u00c0', 0xb6 }, // À
        }.ToImmutableDictionary();
        static readonly ImmutableDictionary<byte, char> BytesToChars = new Dictionary<byte, char>
        {
            { 0x81, '\u00fc' }, // ü
            { 0x82, '\u00e9' }, // é
            { 0x83, '\u00e2' }, // â
            { 0x84, '\u00e4' }, // ä
            { 0x85, '\u00e0' }, // à
            { 0x87, '\u00e7' }, // ç
            { 0x88, '\u00ea' }, // ê
            { 0x8a, '\u00e8' }, // è
            { 0x8e, '\u00c4' }, // Ä
            { 0x90, '\u00c9' }, // É
            { 0x94, '\u00f6' }, // ö
            { 0x96, '\u00fb' }, // û
            { 0x99, '\u00d6' }, // Ö
            { 0x9a, '\u00dc' }, // Ü
            { 0x9b, '\u00a2' }, // ¢
            { 0x9e, '\u00df' }, // ß
            { 0xa0, '\u00e1' }, // á
            { 0xb6, '\u00c0' }, // À
            { 0xb4, '\'' }, // ´ -> '
        }.ToImmutableDictionary();

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return Math.Min(chars.Length, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            var baseEncodingBytes = BaseEncoding.GetBytes(chars, charIndex, charCount);

            for (int i = 0; i < charCount; ++i)
            {
                char ch = chars[charIndex + i];

                if (CharsToBytes.ContainsKey(ch))
                    bytes[byteIndex + i] = CharsToBytes[ch];
                else
                    bytes[byteIndex + i] = baseEncodingBytes[i];
            }

            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return Math.Min(bytes.Length, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            var baseEncodingChars = BaseEncoding.GetChars(bytes, byteIndex, byteCount);

            for (int i = 0; i < byteCount; ++i)
            {
                byte by = bytes[byteIndex + i];

                if (BytesToChars.ContainsKey(by))
                    chars[charIndex + i] = BytesToChars[by];
                else
                    chars[charIndex + i] = baseEncodingChars[i];
            }

            return byteCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }
    }
}
