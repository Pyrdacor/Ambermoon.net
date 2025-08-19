﻿using System.Collections.Generic;

namespace Ambermoon.UI;

internal static class QuestTexts
{
    public static readonly Dictionary<GameLanguage, string> ShowCompletedQuestsTooltip = new()
    {
        { GameLanguage.German, "Abgeschlossene Quests ein- oder ausblenden" },
        { GameLanguage.English, "Show or hide completed quests" },
        // TODO ...
    };

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
        // Note: Max length = 44 characters
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
        // Note: Max length = 43 characters
        { GameLanguage.German, new()
        {
            // Grandfather's Quest
            { SubQuestType.Grandfather_TalkToGrandfather, "Rede mit deinem Großvater" },
            { SubQuestType.Grandfather_GoToWineCellar, "Gehe in den Weinkeller" },
            { SubQuestType.Grandfather_FindHisEquipment, "Finde Großvaters Ausrüstung" },
            { SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, "Erzähle Großvater vom Einsturz" },
            { SubQuestType.Grandfather_FindTolimar, "Finde Tolimar den Hufschmied in Spannenberg" }, // Note: This is the longest possible string
            { SubQuestType.Grandfather_RemoveCaveIn, "Beseitige den Einsturz" },
            { SubQuestType.Grandfather_ReturnToGrandfather, "Kehre zu Großvater zurück" },
            { SubQuestType.Grandfather_VisitGrave, "Besuche Großvaters Grab" },
            // Swamp Fever
            { SubQuestType.SwampFever_TalkToFatherAnthony, "Sprich mit Vater Antonius" },
            { SubQuestType.SwampFever_ObtainEmptyBottle, "Besorge eine leere Phiole" },
            { SubQuestType.SwampFever_ObtainSwampLilly, "Besorge eine Sumpflilie" },
            { SubQuestType.SwampFever_ObtainWaterOfLife, "Besorge Wasser des Lebens" },
            { SubQuestType.SwampFever_ReturnToAnthony, "Kehre zu Vater Antonius zurück" },
            { SubQuestType.SwampFever_HealSally, "Heile Sally, die Tochter des Fischers" },
            { SubQuestType.SwampFever_TalkToSally, "Rede mit Sally, der Tochter des Fischers" },
            { SubQuestType.SwampFever_TalkToWat, "Rede mit Wat dem Fischer" },
            // Alkem's Ring
            { SubQuestType.AlkemsRing_EnterTheCrypt, "Betritt die alte Krypta" },
            { SubQuestType.AlkemsRing_FindTheRing, "Finde den Ring in der Krypta" },
            { SubQuestType.AlkemsRing_ReturnTheRing, "Bring den Ring zu Alkem" },
            // Golden Horseshoes
            { SubQuestType.GoldenHorseshoes_FindHorseshoes, "Finde die goldenen Hufeisen" },
            { SubQuestType.GoldenHorseshoes_ReturnHorseshoes, "Bring die goldenen Hufeisen zu Tolimar" },
            // TODO ...
        } },
        { GameLanguage.English, new()
        {
            // Grandfather's Quest
            { SubQuestType.Grandfather_TalkToGrandfather, "Talk to your grandfather" },
            { SubQuestType.Grandfather_GoToWineCellar, "Go to the wine cellar" },
            { SubQuestType.Grandfather_FindHisEquipment, "Find grandfather's equipment" },
            { SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, "Inform grandfather about the cave-in" },
            { SubQuestType.Grandfather_FindTolimar, "Find Tolimar, the farrier, in Spannenberg" },
            { SubQuestType.Grandfather_RemoveCaveIn, "Remove the cave-in" },
            { SubQuestType.Grandfather_ReturnToGrandfather, "Return to grandfather" },
            { SubQuestType.Grandfather_VisitGrave, "Visit grandfather's grave" },
            // Swamp Fever
            { SubQuestType.SwampFever_TalkToFatherAnthony, "Talk to Father Anthony" },
            { SubQuestType.SwampFever_ObtainEmptyBottle, "Obtain an empty bottle" },
            { SubQuestType.SwampFever_ObtainSwampLilly, "Obtain a swamp lilly" },
            { SubQuestType.SwampFever_ObtainWaterOfLife, "Obtain water of life" },
            { SubQuestType.SwampFever_ReturnToAnthony, "Return to Father Anthony" },
            { SubQuestType.SwampFever_HealSally, "Heal Sally, the fisherman's daughter" },
            { SubQuestType.SwampFever_TalkToSally, "Talk to Sally, the fisherman's daughter" },
            { SubQuestType.SwampFever_TalkToWat, "Talk to Wat the fisherman" },
            // Alkem's Ring
            { SubQuestType.AlkemsRing_EnterTheCrypt, "Enter the old crypt" },
            { SubQuestType.AlkemsRing_FindTheRing, "Find the ring inside the crypt" },
            { SubQuestType.AlkemsRing_ReturnTheRing, "Return the ring to Alkem" },
            // Golden Horseshoes
            { SubQuestType.GoldenHorseshoes_FindHorseshoes, "Find the golden horseshoes" },
            { SubQuestType.GoldenHorseshoes_ReturnHorseshoes, "Return the golden horseshoes to Tolimar" },
            // TODO ...
        } }
        // TODO ...
    };
}
