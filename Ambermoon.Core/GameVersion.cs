using Ambermoon.Data;
using System;

namespace Ambermoon
{
    public enum GameLanguage
    {
        German,
        English
    }

    public class GameVersion
    {
        public string Version;
        public string Language;
        public string Info;
        public Func<IGameData> DataProvider;
    }

    public static class GameLanguageExtensions
    {
        public static GameLanguage ToGameLanguage(this string languageString)
        {
            languageString = languageString.ToLower().Trim();

            if (languageString == "deutsch" || languageString == "german" || languageString == "deu" || languageString == "de")
                return GameLanguage.German;

            return GameLanguage.English;
        }
    }
}
