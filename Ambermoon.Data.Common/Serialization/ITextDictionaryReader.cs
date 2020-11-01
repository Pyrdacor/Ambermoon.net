namespace Ambermoon.Data.Serialization
{
    public interface ITextDictionaryReader
    {
        void ReadTextDictionary(TextDictionary textDictionary, IDataReader dataReader, string language);
    }
}