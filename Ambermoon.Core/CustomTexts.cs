/*
 * CustomTexts.cs - Additional texts only used in the remake
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

using System.Collections.Generic;

namespace Ambermoon
{
    internal static class CustomTexts
    {
        public enum Index
        {
            ReallyStartNewGame,
            FailedToLoadSavegame,
            FailedToLoadInitialSavegame,
            FailedToLoadSavegameUseInitial,
            StartNewGameOrQuit,
            RuneTableUsage,
            LoadCrashedGame,
            FailedToRemoveCrashSavegame,
            QuickSaved,
            QuickLoaded
        }

        static readonly Dictionary<GameLanguage, Dictionary<Index, string>> entries = new Dictionary<GameLanguage, Dictionary<Index, string>>
        {
            { GameLanguage.German, new Dictionary<Index, string>
                {
                    { Index.ReallyStartNewGame, "Wollen Sie wirklich ein neues Spiel starten?" },
                    { Index.FailedToLoadSavegame, "Fehler beim Laden des Spielstands." },
                    { Index.FailedToLoadInitialSavegame, "Fehler beim Laden des Start-Spielstands." },
                    { Index.FailedToLoadSavegameUseInitial, "Fehler beim Laden des Spielstands. Ein neues Spiel wurde stattdessen gestartet." },
                    { Index.StartNewGameOrQuit, "Möchten Sie ein neues Spiel starten oder das Spiel verlassen?" },
                    { Index.RuneTableUsage, "Solange Sie das Runenalphabet bei sich tragen, werden Runen nun automatisch als Text angezeigt." },
                    { Index.LoadCrashedGame, "Ein Backup-Spielstand eines kürzlichen Absturzes wurde gefunden. Wollen Sie diesen laden?" },
                    { Index.FailedToRemoveCrashSavegame, "Der Backup-Spielstand konnte nicht automatisch gelöscht werden. Bitte löschen Sie ihn manuell. Er wird im Unterordner 'Save.99' abgelegt." },
                    { Index.QuickSaved, "Gespeichert als ~INK 22~'{0}'~INK 31~."},
                    { Index.QuickLoaded, "~INK 22~Spielstand '{0}~INK 31~ wurde geladen."}
                }
            },
            { GameLanguage.English, new Dictionary<Index, string>
                {
                    { Index.ReallyStartNewGame, "Do you really want to start a new game?" },
                    { Index.FailedToLoadSavegame, "Failed to load savegame." },
                    { Index.FailedToLoadInitialSavegame, "Failed to load initial savegame." },
                    { Index.FailedToLoadSavegameUseInitial, "Failed to load savegame. Loaded initial savegame instead." },
                    { Index.StartNewGameOrQuit, "Do you want to start a new game or quit the game?" },
                    { Index.RuneTableUsage, "As long as you have the rune table in your inventory, all runes will automatically be displayed as text now." },
                    { Index.LoadCrashedGame, "A crash backup savegame was detected. Do you want to load it?" },
                    { Index.FailedToRemoveCrashSavegame, "The crash backup savegame could not be deleted automatically. Please do so yourself. It is stored in sub-folder 'Save.99'." },
                    { Index.QuickSaved, "Saved as ~INK 22~'{0}'~INK 31~."},
                    { Index.QuickLoaded, "~INK 22~Quicksave {0}~INK 31~ was loaded."}
                }
            }
        };

        public static string GetText(GameLanguage language, Index index) => entries[language][index];
    }
}
