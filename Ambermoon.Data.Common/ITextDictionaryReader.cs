namespace Ambermoon.Data
{
    public interface ITextDictionaryReader
    {
        void ReadTextDictionary(TextDictionary textDictionary, IDataReader dataReader, string language);
    }
}