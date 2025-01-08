using System.Collections.Generic;

namespace Ambermoon.UI;

internal static class QuestTexts
{
    public static readonly Dictionary<GameLanguage, string> LegendActive = new()
    {
        { GameLanguage.German, "Aktiv" },
        { GameLanguage.English, "Active" },
        // TODO ...
    };

    public static readonly Dictionary<GameLanguage, string> LegendBlocked = new()
    {
        { GameLanguage.German, "Blockiert" },
        { GameLanguage.English, "Blocked" },
        // TODO ...
    };

    public static readonly Dictionary<GameLanguage, string> LegendCompleted = new()
    {
        { GameLanguage.German, "Abgeschlossen" },
        { GameLanguage.English, "Completed" },
        // TODO ...
    };

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
            // Grandfather's Quest
            { SubQuestType.Grandfather_GoToWineCellar, "Gehe in den Weinkeller" },
            { SubQuestType.Grandfather_FindHisEquipment, "Finde Großvaters Ausrüstung" },
            { SubQuestType.Grandfather_RemoveCaveIn, "Beseitige den Einsturz" },
            { SubQuestType.Grandfather_VisitAntonius, "Besuche Vater Antonius" },
            // Swamp Fever
            { SubQuestType.SwampFever_ObtainEmptyBottle, "Besorge eine leere Phiole" },
            { SubQuestType.SwampFever_ObtainSwampLilly, "Besorge eine Sumpflilie" },
            { SubQuestType.SwampFever_ObtainWaterOfLife, "Besorge Wasser des Lebens" },
            // TODO ...
        } },
        { GameLanguage.English, new()
        {
            { SubQuestType.Grandfather_GoToWineCellar, "Go to the wine cellar" },
            { SubQuestType.Grandfather_FindHisEquipment, "Find grandfather's equipment" },
            { SubQuestType.Grandfather_RemoveCaveIn, "Remove the cave-in" },
            { SubQuestType.Grandfather_VisitAntonius, "Visit father Anthony" },
            // Swamp Fever
            { SubQuestType.SwampFever_ObtainEmptyBottle, "Obtain an empty bottle" },
            { SubQuestType.SwampFever_ObtainSwampLilly, "Obtain a swamp lilly" },
            { SubQuestType.SwampFever_ObtainWaterOfLife, "Obtain water of life" },
            // TODO ...
        } }
        // TODO ...
    };
}
