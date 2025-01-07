/*
 * GameVersion.cs - Game version and language
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon
{
    public enum GameLanguage
    {
        English,
        German,        
        French,
        Polish,
        Czech
    }

    public class GameVersion
    {
        public string Version;
        public GameLanguage Language;
        public string Info;
        public Features Features;
        public bool MergeWithPrevious;
        public bool ExternalData;
        public Func<IGameData> DataProvider;

        internal const string RemakeReleaseDate = "07-01-2025";
    }

    public static class GameLanguageExtensions
    {
        public static GameLanguage ToGameLanguage(this string languageString)
        {
            if (Enum.TryParse(languageString, out GameLanguage gameLanguage))
                return gameLanguage;

            languageString = languageString.ToLower().Trim();

            if (languageString == "german" || languageString == "deutsch" || languageString == "ger" || languageString == "de")
                return GameLanguage.German;
            if (languageString == "french" || languageString == "français" || languageString == "fre" || languageString == "fr")
                return GameLanguage.French;
            if (languageString == "polish" || languageString == "polski" || languageString == "pol" || languageString == "pl")
                return GameLanguage.Polish;
            if (languageString == "czech" || languageString == "český" || languageString == "česky" || languageString == "ces" || languageString == "cze" || languageString == "cs")
                return GameLanguage.Czech;

            return GameLanguage.English;
        }
    }
}
