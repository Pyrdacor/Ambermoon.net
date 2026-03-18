using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class TextDictionary
    {
        public GameLanguage Language { get; set; }
        public List<string> Entries { get; } = [];

        private TextDictionary()
        {

        }

        public static TextDictionary Load(ITextDictionaryReader textDictionaryReader, KeyValuePair<GameLanguage, IDataReader> dictionary)
        {
            var textDictionary = new TextDictionary();

            textDictionaryReader.ReadTextDictionary(textDictionary, dictionary.Value, dictionary.Key);

            return textDictionary;
        }

        public static TextDictionary Load(GameLanguage language, IEnumerable<string> entries)
        {
            var textDictionary = new TextDictionary
            {
                Language = language
            };

            textDictionary.Entries.AddRange(entries);

            return textDictionary;
        }
    }
}
