namespace Ambermoon.Data.Legacy
{
    public class TextDictionaryReader : ITextDictionaryReader
    {
        public void ReadTextDictionary(TextDictionary textDictionary, IDataReader dataReader, string language)
        {
            textDictionary.Language = language;
            int numEntries = dataReader.ReadWord();

            for (int i = 0; i < numEntries; ++i)
                textDictionary.Entries.Add(dataReader.ReadString());
        }
    }
}
