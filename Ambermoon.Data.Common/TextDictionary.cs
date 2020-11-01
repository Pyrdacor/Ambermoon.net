using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class TextDictionary
    {
        public string Language { get; set; }
        public List<string> Entries { get; } = new List<string>();

        private TextDictionary()
        {

        }

        public static TextDictionary Load(ITextDictionaryReader textDictionaryReader, KeyValuePair<string, IDataReader> dictionary)
        {
            var textDictionary = new TextDictionary();

            textDictionaryReader.ReadTextDictionary(textDictionary, dictionary.Value, dictionary.Key);

            return textDictionary;
        }
    }
}
