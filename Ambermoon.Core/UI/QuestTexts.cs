using System.Collections.Generic;

namespace Ambermoon.UI;

internal static class QuestTexts
{
    public static readonly Dictionary<GameLanguage, Dictionary<MainQuestType, string>> MainQuests = new()
    {
        { GameLanguage.German, new()
        {
            { MainQuestType.Grandfather, "Großvaters Queste" },
            { MainQuestType.SwampFever, "Sumpffieber" },
            { MainQuestType.AlkemsRing, "Alkems Ring" },
            { MainQuestType.ThiefPlague, "Die Diebesplage" },
            { MainQuestType.OrcPlague, "Die Orkplage" },
            { MainQuestType.Sylphs, "Sylphen" },
            { MainQuestType.WineTrophies, "Weinpokale" },
            { MainQuestType.GoldenHorseshoes, "Goldene Hufeisen" },
            { MainQuestType.ChainOfOffice, "Amtskette" },
            { MainQuestType.Graveyard, "Der Friedhofsgärtner" },
            { MainQuestType.Monstereye, "Monsterauge" },
            { MainQuestType.ThiefGuild, "Die Diebesgilde" },
            { MainQuestType.SandrasDaughter, "Sandras Tochter" },
            { MainQuestType.TowerOfBlackMagic, "Turm der Schwarzmagier" },
            { MainQuestType.ValdynsReturn, "Valdyns Rückkehr" },
            { MainQuestType.AmbermoonPicture, "Ambermoon Gemälde" },
            // TODO ...
        } },
        { GameLanguage.English, new()
        {
            { MainQuestType.Grandfather, "Grandfather's Quest" },
            { MainQuestType.SwampFever, "Swamp Fever" },
            { MainQuestType.AlkemsRing, "Alkem's Ring" },
            { MainQuestType.ThiefPlague, "The Thieves' Menace" },
            { MainQuestType.OrcPlague, "Orcish Onslaught" },
            { MainQuestType.Sylphs, "Sylphs" },
            { MainQuestType.WineTrophies, "Wine Trophies" },
            { MainQuestType.GoldenHorseshoes, "Golden Horseshoes" },
            { MainQuestType.ChainOfOffice, "Chain of Office" },
            { MainQuestType.Graveyard, "The Graveyard" },
            { MainQuestType.Monstereye, "Monster's Eye" },
            { MainQuestType.ThiefGuild, "The Thieves' Guild" },
            { MainQuestType.SandrasDaughter, "Sandra's Daughter" },
            { MainQuestType.TowerOfBlackMagic, "The Tower of blac magic" },
            { MainQuestType.ValdynsReturn, "Valdyn's Return" },
            { MainQuestType.AmbermoonPicture, "Ambermoon Picture" },
            // TODO ...
        } }
        // TODO ...
    };

    public static readonly Dictionary<GameLanguage, Dictionary<SubQuestType, string>> SubQuests = new()
    {
        { GameLanguage.German, new()
        {
            { SubQuestType.Grandfather_GoToWineCellar, "Gehe in den Weinkeller" },
            // TODO ...
        } },
        { GameLanguage.English, new()
        {
            { SubQuestType.Grandfather_GoToWineCellar, "Go to the wine cellar" },
            // TODO ...
        } }
        // TODO ...
    };
}
