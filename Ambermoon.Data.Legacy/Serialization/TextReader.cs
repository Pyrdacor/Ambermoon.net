using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextReader
    {
        public static List<string> ReadTexts(IDataReader textDataReader)
        {
            return ReadTexts(textDataReader, new char[] { ' ', '\0' });
        }

        public static List<string> ReadTexts(IDataReader textDataReader, char[] trimChars)
        {
            var texts = new List<string>();

            if (textDataReader != null)
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
