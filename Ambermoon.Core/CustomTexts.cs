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
            RuneTableUsage
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
                    { Index.RuneTableUsage, "Solange Sie das Runenalphabet bei sich tragen, werden Runen nun automatisch als Text angezeigt." }
                }
            },
            { GameLanguage.English, new Dictionary<Index, string>
                {
                    { Index.ReallyStartNewGame, "Do you really want to start a new game?" },
                    { Index.FailedToLoadSavegame, "Failed to load savegame." },
                    { Index.FailedToLoadInitialSavegame, "Failed to load initial savegame." },
                    { Index.FailedToLoadSavegameUseInitial, "Failed to load savegame. Loaded initial savegame instead." },
                    { Index.StartNewGameOrQuit, "Do you want to start a new game or quit the game?" },
                    { Index.RuneTableUsage, "As long as you have the rune table in your inventory, all runes will automatically be displayed as text now." }
                }
            }
        };

        public static string GetText(GameLanguage language, Index index) => entries[language][index];
    }
}
