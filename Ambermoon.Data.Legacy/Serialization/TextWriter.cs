using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextWriter
    {
        public static byte[] ToBytes(List<string> texts)
        {
            var writer = new DataWriter();
            WriteTexts(writer, texts);
            return writer.ToArray();
        }

        public static byte[] ToBytes(List<string> texts, char[] trimChars)
        {
            var writer = new DataWriter();
            WriteTexts(writer, texts, trimChars);
            return writer.ToArray();
        }

        public static void WriteTexts(IDataWriter textDataWriter, List<string> texts)
        {
            WriteTexts(textDataWriter, texts, new char[] { ' ', '\0' });
        }

        public static void WriteTexts(IDataWriter textDataWriter, List<string> texts, char[] trimChars)
        {
            if (texts == null)
            {
                textDataWriter.Write((ushort)0);
                return;
            }

            textDataWriter.Write((ushort)texts.Count);

            var trimmedTexts = texts.Select(text =>
            {
                if (trimChars?.Length > 0)
                    text = text.Trim(trimChars);

                if (!text.Contains('\0'))
                    text += "\0";

                return text;
            });

            foreach (var text in trimmedTexts)
                textDataWriter.Write((ushort)text.Length);

            foreach (var text in trimmedTexts)
                textDataWriter.WriteWithoutLength(text);
        }
    }
}
