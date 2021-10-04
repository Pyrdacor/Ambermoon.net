using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextReader
    {
        public static List<string> ReadTexts(IDataReader textDataReader)
        {
            return ReadTexts(textDataReader, new char[] { ' ', '\0' })
                .Select(t =>
                {
                    // There are some texts with \0 chars inside them.
                    // This is a bug but we should be able to handle
                    // such input data.
                    if (t.Contains('\0'))
                        return t.Substring(0, t.IndexOf('\0'));
                    else
                        return t;
                }).ToList();
        }

        public static List<string> ReadTexts(IDataReader textDataReader, char[] trimChars)
        {
            var texts = new List<string>();

            if (textDataReader != null && textDataReader.Size != 0)
            {
                textDataReader.Position = 0;
                int numTexts = textDataReader.ReadWord();
                int[] textLengths = new int[numTexts];

                for (int i = 0; i < numTexts; ++i)
                    textLengths[i] = textDataReader.ReadWord();

                for (int i = 0; i < numTexts; ++i)
                {
                    if (trimChars?.Length > 0)
                        texts.Add(textDataReader.ReadString(textLengths[i]).Trim(trimChars));
                    else
                        texts.Add(textDataReader.ReadString(textLengths[i]));
                }
            }

            return texts;
        }
    }
}
