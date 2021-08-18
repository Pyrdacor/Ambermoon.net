/*
 * GameVersion.cs - Game version and language
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
