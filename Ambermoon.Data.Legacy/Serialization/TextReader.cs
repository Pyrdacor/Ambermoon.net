using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal class TextReader
    {
        public static List<string> ReadTexts(IDataReader textDataReader)
        {
            var texts = new List<string>();

            if (textDataReader != null)
            {
                textDataReader.Position = 0;
                int numMapTexts = textDataReader.ReadWord();
                int[] mapTextLengths = new int[numMapTexts];

                for (int i = 0; i < numMapTexts; ++i)
                    mapTextLengths[i] = textDataReader.ReadWord();

                for (int i = 0; i < numMapTexts; ++i)
                    texts.Add(textDataReader.ReadString(mapTextLengths[i]).Trim(' ', '\0'));
            }

            return texts;
        }
    }
}
