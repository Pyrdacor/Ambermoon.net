using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextReader
    {
        public static List<string> ReadTexts(IDataReader textDataReader)
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
                    texts.Add(textDataReader.ReadString(textLengths[i]).Trim(' ', '\0'));
            }

            return texts;
        }
    }
}
