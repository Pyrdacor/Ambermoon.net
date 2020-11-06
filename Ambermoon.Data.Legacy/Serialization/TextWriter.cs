using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextWriter
    {
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

            void WriteText(string text)
            {
                if (trimChars?.Length > 0)
                    text = text.Trim(trimChars);

                if (!text.Contains('\0'))
                    text += "\0";

                textDataWriter.Write((ushort)text.Length);
                textDataWriter.WriteWithoutLength(text);
            }

            texts.ForEach(WriteText);
        }
    }
}
