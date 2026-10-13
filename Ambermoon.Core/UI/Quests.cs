using System.Collections.Generic;
using System;
using System.Linq;
using Ambermoon.Data;

namespace Ambermoon.UI;

public enum MainQuestType
{
    LyramionsFaith,
    Grandfather,
    SwampFever,
    AlkemsRing,
    ThiefPlague,
    OrcPlague,
    Sylphs,
    WineTrophies,
    GoldenHorseshoes,
    ChainOfOffice,
    Graveyard,
    Monstereye,
    ThiefGuild,
    SandrasDaughter,
    TowerOfBlackMagic,
    ValdynsReturn,
    AmbermoonPicture,
    BrotherhoodOfTarbos,
    Madman,
    KiresLetter,
    FerrinsDaughter,
    TheBeast,
    WitchMaster,
    SansrisCurse,
    Windgates,
    TripToGadlon,
    // Advanced only
    DrelbsSearch,
    GoldenArmour,
    GralTemple,
    YourHouse,
    Pets,
    MysticTower,
    MasterThief,
    MysticCreatures,
    SandrasBrooch,
    Eyrie,
    BlackMountains,
    DesertTemple,
    PaladinGuild,
    SeaCreatures,
    ManyeyesCastle,
    Gatekeepers,
    IsdirsTrials,
}

public enum SubQuestType
{
    // Lyramion's Faith
    LyramionsFaith_TalkToShandraInNewlake,
    LyramionsFaith_BringShandrasStoneToGrandfather,
    LyramionsFaith_UseShandrasStone,
    LyramionsFaith_EnterTheTempleOfBrotherhood,
    LyramionsFaith_ExploreTheTempleOfBrotherhood,
    LyramionsFaith_ExploreTheHangar,
    LyramionsFaith_FindTheNavStone,
    LyramionsFaith_FlyToTheForestMoon,
    LyramionsFaith_MeetTheDwarfLeader,
    LyramionsFaith_FindAWayToLeaveForestMoon,
    LyramionsFaith_FlyToMorag,
    LyramionsFaith_TalkToSriel,
    LyramionsFaith_SearchSrielsPalace,
    LyramionsFaith_CollectTheCrystalStrings,
    LyramionsFaith_HaveTheHarpStrung,
    LyramionsFaith_ShatterTheCrystalWall,
    LyramionsFaith_DestroyTheMoranianMachine,
    LyramionsFaith_EnterSecretRoomInLibrary,
    LyramionsFaith_FindRecipe,
    LyramionsFaith_BrewDemonSleep,
    // Grandfather's Quest
    Grandfather_TalkToGrandfather,
    Grandfather_GoToWineCellar,
    Grandfather_FindHisEquipment,
    Grandfather_TellGrandfatherAboutCaveIn,
    Grandfather_FindTolimar,
    Grandfather_RemoveCaveIn,
    Grandfather_ReturnToGrandfather,
    Grandfather_VisitGrave,
    // Swamp Fever
    SwampFever_TalkToFatherAnthony,
    SwampFever_ObtainEmptyBottle,
    SwampFever_ObtainSwampLilly,
    SwampFever_ObtainWaterOfLife,
    SwampFever_ReturnToAnthony,
    SwampFever_HealSally,
    SwampFever_TalkToSally,
    SwampFever_TalkToWat,
    // Alkem's Ring
    AlkemsRing_EnterTheCrypt,
    AlkemsRing_FindTheRing,
    AlkemsRing_ReturnTheRing,
    // Thief Plague
    ThiefPlague_DealWithNagier, // Baron George teaches keyword "Probleme"; deal with the bandit leader Nagier (kill him for the silver hand, or make peace for his letter)
    ThiefPlague_ReturnToGeorg,
    // Sylphs
    Sylphs_TalkToLadyHeidi, // get it when talking to the cook
    Sylphs_FindTheHiddenItemInTheTree, // after talking to lady heidi
    Sylphs_FindTheSylphs, // after finding the hidden item
    Sylphs_RescueSelena, // after finding the sylphs
    // Wine Trophies
    WineTrophies_FindThem, // x/2, get it when talking to Canth or Norael about it
    WineTrophies_ReturnThem, // x/2, after finding at least 1 trophy
    // ChainOfOffice
    ChainOfOffice_FindIt, // started by talking to heidi or baron about fairies
    ChainOfOffice_ReturnIt, // after finding it
    // Graveyard
    Graveyard_FindTheGardener, // after Aman asked for the brooch
    Graveyard_GetBroochFromGravedigger, // after talking to Aman
    Graveyard_ReturnBroochToAman, // after getting the brooch
    // Monstereye
    Monstereye_BuyIt, // after talking to Sandire
    // Thief Guild
    ThiefGuild_Enter, // after returning the brooch to Aman
    ThiefGuild_PassTheTest, // after entering the guild
    // Golden Horseshoes
    GoldenHorseshoes_FindHorseshoes,
    GoldenHorseshoes_ReturnHorseshoes,
    // Orcs
    OrcPlague_DefeatTheOrcLeader, // Freiherr Georg teaches keyword "Probleme"; defeat the hill-giant orc leader in the Orc Cave (map 280)
    OrcPlague_ReturnToGeorg,
    // Sandra's Daughter
    SandrasDaughter_GoToBurnvilleAndFindSabine, // after talking to sandra
    SandrasDaughter_RescueSabine, // after finding sabine's note
    SandrasDaughter_ReturnToSandra, // after rescuing Sabine
    // Tower of Black Magic
    TowerOfBlackMagic_OpenTheTower, // after Nelvin told about the sphere of opening
    TowerOfBlackMagic_FreeThePeopleOfBurnville, // after entering the torture chamber
    TowerOfBlackMagic_WeakenLuminor, // after learning about the flame of life or finding icewater
    TowerOfBlackMagic_DefeatLuminor, // after entering the torture chamber
    // Valdyn's Return
    ValdynsReturn_FindAWayHome, // after Valdyn came through the light gate
    ValdynsReturn_UseTheYellowSphere, // after getting the yellow sphere
    // Madman
    Madman_VisitTheMadman, // after Clementine told about him
    Madman_FindTheSecondPartOfTheAmulet, // after finding a part of the amulet
    Madman_UniteTheAmulet, // after finding both parts
    Madman_GiveTheAmuletToTheMadman, // after uniting the amulet
    Madman_KillTheMadman, // only if you kill him
    // Kire's Letter
    KiresLetter_FindDorina, // after Kire gave his letter
    KiresLetter_ShowTheLetterToKetnar, // optional, after Kire gave his letter
    KiresLetter_GiveTheLetterToDorina, // after finding Dorina
    KiresLetter_ReturnToKire, // after Dorina got the letter
    // Ferrin's Daughter
    FerrinsDaughter_AskFerrinAboutHisMourning, // after meeting Ferrin
    FerrinsDaughter_SearchTheStorageCellar, // after Ferrin gave his key
    FerrinsDaughter_FindFerrinsDaughter, // after entering the cellar
    FerrinsDaughter_ReturnToFerrin, // after finding his daughter
    // The Beast
    TheBeast_AskNorelAboutKay, // after Norel asked for Kay
    TheBeast_KillTheBeast, // after Norel told about the beast or meeting it
    TheBeast_BringTheAmuletToNorel, // after killing the beast
    // The Witch Master
    WitchMaster_HearBralumsProblem, // after Bralum mentioned the problem
    WitchMaster_FindNerasLair, // after Bralum told about Nera
    WitchMaster_DefeatNera, // after opening the door to his lair
    WitchMaster_ShowTheRingToBralum, // after finding Nera's ring
    // Sansri's Curse
    SansrisCurse_AskPelanisAboutHisConcerns, // after Pelanis mentioned the curse
    SansrisCurse_DefeatSansri, // after Pelanis told about Sansri
    SansrisCurse_GiveSansrisBloodToPelanis, // after defeating Sansri
    // Windgates
    Windgates_EnterTheWindShrine, // after finding the windgate book or the key to the shrine
    Windgates_CreateTheConstructionStaff, // after entering the wind shrine
    Windgates_CreateTheWindChain, // after entering the wind shrine
    Windgates_RepairTheWindgates, // after creating the construction staff
    // Trip to Gadlon
    TripToGadlon_ActivateTheTeleporter, // after finding the red carved sphere
    TripToGadlon_FindRinakles, // after activating the teleporter
    TripToGadlon_AskRinaklesForTheWayHome, // after meeting Rinakles
    // Drelb's Search (Advanced)
    DrelbsSearch_DefeatTheMonstersInTheMine, // after meeting Drelb
    DrelbsSearch_AskDrelbAboutHisSearch, // after defeating the monsters
    DrelbsSearch_FindTheHidingPlaces, // after Drelb hid the things
    DrelbsSearch_GiveTheCertificateToDrelb, // after finding the certificate
    // The Golden Armour (Advanced)
    GoldenArmour_CollectGoldenScales, // after meeting Morgat or finding a golden scale
    GoldenArmour_BringTheScalesToMorgat, // after collecting seven scales
    // The Gral Temple (Advanced)
    GralTemple_AskMelvinAboutHisFamily, // after meeting Melvin
    GralTemple_SayGralAtNight, // after Melvin told the legend
    GralTemple_OpenTheInnerDoor, // after the gate in the mountains opened
    GralTemple_PlaceTheHeavyStoneOnTheAltar, // after opening the inner door
    GralTemple_DefeatDiokhar, // after placing the stone or meeting Diokhar
    GralTemple_FindMelvinsRemains, // after the gate in the mountains opened
    GralTemple_BringTheNecklaceToMelvin, // after finding the emerald necklace
    // Your House (Advanced)
    YourHouse_ShowTheTestamentToTheodorus, // after finding the testament or meeting Theodorus
    YourHouse_HaveTheHouseRefurbished, // after Theodorus saw the testament
    YourHouse_AskGustavAboutTheodorus, // after meeting Gustav
    YourHouse_HaveAWindgateBuilt, // after the refurbishment and Gustav's story
    YourHouse_HaveAStorageCaveDug, // after the windgate was built
    YourHouse_ExploreTheStorageCave, // after the storage cave was dug
    YourHouse_DefeatTheGiantSpider, // after meeting the spider
    // Pets (Advanced)
    Pets_FeedTheHungryTomcat, // after talking to Kasimir
    Pets_AdoptACat, // after Ferdinand offered his pets
    Pets_BefriendTheCat, // after adopting the cat
    Pets_AdoptADog, // after befriending the cat
    Pets_BefriendTheDog, // after adopting the dog
    Pets_ReturnToFerdinand, // after befriending the dog
    // The Mystic Tower (Advanced)
    MysticTower_VisitThePond, // after getting Targor's notes
    MysticTower_VisitTheLonelyTree, // after the pond
    MysticTower_VisitTheMountains, // after the tree
    MysticTower_EnterTheMysticTower, // after the tower rose
    MysticTower_PassTheTestOfHighMysticism, // after entering the tower
    // The Master Thief (Advanced)
    MasterThief_HelpAmanAgainstTheKnights, // after Aman asked for help
    MasterThief_MeetAmanInTheThievesGuild, // after the knights are gone
    MasterThief_SolveTheBarrelRiddle, // after getting the silver lockpick
    MasterThief_ShowTheCloakToAman, // after solving the barrel riddle
    MasterThief_PassTheTestOfStealth, // after Aman gave his key
    MasterThief_TalkToAman, // after the final switch
    // The Mystic Creatures (Advanced)
    MysticCreatures_FindTheWineCellarOfCrystal, // after finding the book "Towns of Lyramion"
    MysticCreatures_FindTheBookOfMysticCreatures, // after finding the wine cellar
    MysticCreatures_CollectTheEssences, // after finding the book "Mystic Creatures"
    // Sandra's Brooch (Advanced)
    SandrasBrooch_ReturnTheBroochToSandra, // after finding the brooch
    SandrasBrooch_TransferTheEssencesIntoKasimir, // after Sandra got her brooch
    // The Eyrie (Advanced)
    Eyrie_GetTheEagleKeyFromPelanis, // after reading about Lugthir with the curse of Sansri lifted
    Eyrie_OpenTheEyrie, // after getting the eagle key
    Eyrie_TakeLugthirsEssence, // after opening the eyrie
    // The Black Mountains (Advanced)
    BlackMountains_AskBaronKarstenAboutPyrdacor, // after reading about Pyrdacor
    BlackMountains_GetTheKeyCrystal, // after Karsten told about Pyrdacor
    BlackMountains_HaveLebabChargeTheCrystal, // after getting the crystal
    BlackMountains_OpenTheBlackMountains, // after getting the fire crystal
    BlackMountains_DefeatPyrdacor, // after opening the mountains
    // The Desert Temple (Advanced)
    DesertTemple_SolveTheFacePuzzle, // after reading about Marla
    DesertTemple_AnswerTheRiddlemouths, // after solving the face puzzle
    DesertTemple_DefeatMarla, // after answering the riddlemouths
    // The Paladin Guild of Crystal (Advanced)
    PaladinGuild_BreakThroughTheCrackedWall, // after finding the wine cellar
    PaladinGuild_LightTheCandles, // after breaking through the wall
    PaladinGuild_SummonThePaladinMaster, // after lighting the candles
    PaladinGuild_DefeatThePaladinMaster, // after summoning him
    PaladinGuild_OpenTheExitOfTheGuild, // after defeating the paladin master
    PaladinGuild_TakeGunbarsEssence, // after finding the book of mystic creatures
    PaladinGuild_FindFargonsEssence, // after finding the book of mystic creatures
    // The Sea Creatures (Advanced)
    SeaCreatures_AskCaptainTorle, // after Michael told about Torle
    SeaCreatures_FindTheSeaCreatures, // after getting Torle's logbook
    SeaCreatures_VisitTheMermaid, // after finding the creatures
    SeaCreatures_EnterTheEyeOfTheVortex, // after the mermaid opened the vortex
    // Manyeyes' Castle (Advanced)
    ManyeyesCastle_OpenTristansOffice, // after Tristan gave his office key
    ManyeyesCastle_OpenTheCastle, // after opening the office
    ManyeyesCastle_DefeatTheGuardianOfTheUpperFloor, // after opening the castle
    ManyeyesCastle_BanishManyeyesCurse, // after defeating the guardian or falling into the cellar
    // The Gatekeepers of Cavetown (Advanced)
    Gatekeepers_BringJeffABlindBrandy, // after meeting Jeff
    Gatekeepers_FindLukesTreasureMap, // after meeting Luke
    Gatekeepers_GiveTheTreasureMapToLuke, // after finding the treasure map
    Gatekeepers_OpenThePassageForShips, // after getting both keys
    // Isdir's Trials (Advanced)
    IsdirsTrials_AskJeffAboutIsdir, // after reading about Isdir
    IsdirsTrials_MeetIsdirAtNight, // after Jeff told about the northern beach
    IsdirsTrials_BringGoldenWater, // after touching the first crystal
    IsdirsTrials_BringARestlessSoul, // after touching the second crystal
    IsdirsTrials_BringAPoisonousSnake, // after touching the third crystal
    IsdirsTrials_BringTheBlueCrystal, // after touching the fourth crystal
    IsdirsTrials_ReceiveIsdirsEssence, // after all four crystals are satisfied
    // Brotherhood of Tarbos
    BrotherhoodOfTarbos_BringTheAmberstarToTheTemple, // after finding the Amberstar
    BrotherhoodOfTarbos_DefeatThornahuun, // after meeting Thornahuun in the coffin hall
    // Ambermoon Picture
    AmbermoonPicture_DeliverThePicture, // after getting the picture from Dieter Rottermund
    // ...
}

file static class QuestFactory
{
    public static MainQuest CreateMainQuest(QuestLog questLog, MainQuestType mainQuestType, params Func<MainQuestType, SubQuest>[] subQuestFactory)
    {
        var mainQuest = new MainQuest(questLog)
        {
            Type = mainQuestType,
            SubQuests = []
        };

        var subQuests = subQuestFactory.Select(factory => factory(mainQuest.Type)).ToArray();

        return mainQuest with { SubQuests = subQuests };
    }
}

file static class TriggerHelper
{
    public static QuestState GetOldState(this TriggerType triggerType)
    {
        return triggerType switch
        {
            TriggerType.Activation => QuestState.Inactive,
            TriggerType.Completion => QuestState.Active,
            TriggerType.AlwaysComplete => QuestState.Any,
            _ => throw new ArgumentOutOfRangeException(nameof(triggerType), triggerType, null),
        };
    }

    public static QuestState GetNewState(this TriggerType triggerType)
    {
        return triggerType switch
        {
            TriggerType.Activation => QuestState.Active,
            TriggerType.Completion => QuestState.Completed,
            TriggerType.AlwaysComplete => QuestState.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(triggerType), triggerType, null),
        };
    }
}


#region Triggers

file enum TriggerType
{
    Activation,
    Completion,
    AlwaysComplete
}

file class OrTrigger<TFirst, TSecond> : IQuestTrigger
    where TFirst : IQuestTrigger
    where TSecond : IQuestTrigger
{
    private readonly TriggerType triggerType;
    private readonly TFirst firstTrigger;
    private readonly TSecond secondTrigger;

    public OrTrigger(GameCore game, TriggerType triggerType,
        Func<GameCore, TriggerType, TFirst> firstTriggerFactory,
        Func<GameCore, TriggerType, TSecond> secondTriggerFactory)
    {
        this.triggerType = triggerType;
        firstTrigger = firstTriggerFactory(game, triggerType);
        secondTrigger = secondTriggerFactory(game, triggerType);
    }

    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (firstTrigger.CheckEvent(@event, subQuest))
            return true;

        return secondTrigger.CheckEvent(@event, subQuest);
    }

    bool IQuestTrigger.CheckExploration(uint mapIndex, uint x, uint y, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (firstTrigger.CheckExploration(mapIndex, x, y, subQuest))
            return true;

        return secondTrigger.CheckExploration(mapIndex, x, y, subQuest);
    }

    bool IQuestTrigger.CheckItem(Item? item, uint itemCount, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (firstTrigger.CheckItem(item, itemCount, subQuest))
            return true;

        return secondTrigger.CheckItem(item, itemCount, subQuest);
    }

    bool IQuestTrigger.CheckChestUnlock(uint chestIndex, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (firstTrigger.CheckChestUnlock(chestIndex, subQuest))
            return true;

        return secondTrigger.CheckChestUnlock(chestIndex, subQuest);
    }

    bool IQuestTrigger.CheckDoorUnlock(uint doorIndex, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (firstTrigger.CheckDoorUnlock(doorIndex, subQuest))
            return true;

        return secondTrigger.CheckDoorUnlock(doorIndex, subQuest);
    }
}

class PreviousSubQuestCompletedTrigger : IQuestTrigger
{
    public QuestState OldState => QuestState.Inactive;
    public QuestState NewState => QuestState.Active;
}

class NextSubQuestActivatedTrigger : IQuestTrigger
{
    public QuestState OldState => QuestState.Active;
    public QuestState NewState => QuestState.Completed;
}

file class GlobalVariableTrigger(GameCore game, TriggerType triggerType, uint index, bool expectedValue = true) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.ObjectIndex == index && e.Value == (expectedValue ? 1 : 0))
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.GetGlobalVariable(index) == expectedValue)
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class TileChangeTrigger(GameCore game, TriggerType triggerType, uint mapIndex, uint x, uint y, uint expectedTile) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ChangeTileEvent e && e.MapIndex == mapIndex && e.X == x && e.Y == y && e.FrontTileIndex == expectedTile)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.TileChangeEvents.TryGetValue(mapIndex, out var changes) &&
            changes.Any(c => c.X == x && c.Y == y && c.FrontTileIndex == expectedTile))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class EventDisabledTrigger(GameCore game, TriggerType triggerType, uint mapIndex, uint eventIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        uint index = (mapIndex - 1) * 64 + eventIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetEventBit && e.ObjectIndex == index && e.Value == 1)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.GetEventBit(mapIndex, eventIndex - 1))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class CharacterVisibilityTrigger(GameCore game, TriggerType triggerType, uint mapIndex, uint characterIndex, bool visible) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        uint index = (mapIndex - 1) * 32 + characterIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetCharacterBit && e.ObjectIndex == index && e.Value == (visible ? 0 : 1))
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.GetCharacterBit(mapIndex, characterIndex - 1) != visible) // if the bit is true, the character is invisible and vice versa
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class ItemObtainedTrigger(GameCore game, TriggerType triggerType, params uint[] itemIndices) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckItem(Item? item, uint itemCount, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (itemIndices == null || itemIndices.Length == 0)
            return false;

        if (item == null) // only check items you already have
        {
            uint totalAmount = 0;
            uint minAmount = subQuest.MinAmount ?? 1;

            if (NewState != QuestState.Completed)
                minAmount = 1;

            foreach (var itemIndex in itemIndices)
            {
                totalAmount += game.GetTotalItemCount(itemIndex);

                if (NewState == QuestState.Completed && minAmount > 1)
                    subQuest.CurrentAmount = Math.Min(minAmount, subQuest.CurrentAmount + totalAmount);

                if (totalAmount >= minAmount)
                {
                    subQuest.State = NewState;
                    return true;
                }
            }

            return false;
        }

        foreach (var itemIndex in itemIndices)
        {
            if (item.Index == itemIndex)
            {
                if (NewState == QuestState.Completed)
                {
                    uint minAmount = subQuest.MinAmount ?? 1;

                    if (minAmount == 0 || minAmount <= subQuest.CurrentAmount + itemCount)
                    {
                        if (minAmount > 1)
                            subQuest.CurrentAmount = minAmount;

                        subQuest.State = NewState;
                        return true;
                    }

                    subQuest.CurrentAmount += itemCount;
                }
                else
                {
                    subQuest.State = NewState;
                    return true;
                }
            }
        }

        return false;
    }
}

file class ExplorationTrigger(GameCore game, TriggerType triggerType, uint mapIndex, uint x, uint y) : IQuestTrigger
{
    private readonly GameCore game = game;
    private readonly uint mapIndex = mapIndex;
    private readonly uint x = x;
    private readonly uint y = y;

    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckExploration(uint mapIndex, uint x, uint y, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (this.mapIndex == mapIndex && this.x == x && this.y == y)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.Automaps.TryGetValue(this.mapIndex, out var exploration) && exploration.IsBlockExplored(game.MapManager.GetMap(this.mapIndex), this.x, this.y))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

// TODO: Careful with this, as you can also add keywords by typing them in conversation which might activate the quest.
// Always add a required quest when using this.
file class KeywordLearnedTrigger(GameCore game, TriggerType triggerType, uint keywordIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.AddKeyword && e.ObjectIndex == keywordIndex && e.Value == 1)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame!.IsDictionaryWordKnown(keywordIndex))
        {
            if (triggerType == TriggerType.Completion)
            {
                subQuest.State = NewState;
                return true;
            }
            else
            {
                var quests = subQuest.Quest.SubQuests;
                var requiredQuests = subQuest.RequiredCompletedQuests.Select(type => quests.FirstOrDefault(q => q.Type == type));

                if (requiredQuests.All(q => q?.State == QuestState.Completed))
                {
                    subQuest.State = NewState;
                    return true;
                }
            }
        }

        return false;
    }
}

file class ChestUnlockedTrigger(GameCore game, TriggerType triggerType, uint chestIndex) : IQuestTrigger
{
    private readonly uint chestIndex = chestIndex;

    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckChestUnlock(uint chestIndex, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (this.chestIndex == chestIndex)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (!game.CurrentSavegame!.IsChestLocked(this.chestIndex - 1))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class DoorUnlockedTrigger(GameCore game, TriggerType triggerType, uint doorIndex) : IQuestTrigger
{
    private readonly uint doorIndex = doorIndex;

    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckDoorUnlock(uint doorIndex, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (this.doorIndex == doorIndex)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (!game.CurrentSavegame!.IsDoorLocked(this.doorIndex))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Checks a custom condition whenever an event or item is checked. The event
/// (if any) is passed as it is checked before it is executed.
/// </summary>
file class PredicateTrigger(GameCore game, TriggerType triggerType, Func<GameCore, Event?, bool> predicate) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    private bool Check(SubQuest subQuest, Event? @event)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (predicate(game, @event))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest) => Check(subQuest, @event);

    bool IQuestTrigger.CheckItem(Item? item, uint itemCount, SubQuest subQuest) => Check(subQuest, null);
}

/// <summary>
/// Counts the set global variables and the owned items. The sub quest's
/// current amount is updated and the state changes once the count reaches
/// the sub quest's min amount (or 1 if there is none).
/// </summary>
file class CountTrigger(GameCore game, TriggerType triggerType, uint[] globalVariables, uint[]? itemIndices = null, uint? requiredCount = null) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    private bool Check(SubQuest subQuest, uint? globalVariableBeingSet)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        var savegame = game.CurrentSavegame!;
        uint count = (uint)globalVariables.Count(index => index == globalVariableBeingSet || savegame.GetGlobalVariable(index));

        if (itemIndices != null)
        {
            foreach (var itemIndex in itemIndices)
                count += game.GetTotalItemCount(itemIndex);
        }

        uint minAmount = requiredCount ?? (NewState == QuestState.Completed ? subQuest.MinAmount ?? 1 : 1);

        if (NewState == QuestState.Completed && subQuest.MinAmount != null)
            subQuest.CurrentAmount = Math.Max(subQuest.CurrentAmount, Math.Min(count, minAmount));

        if (count >= minAmount)
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }

    bool IQuestTrigger.CheckEvent(Event? @event, SubQuest subQuest)
    {
        // the event is checked before it is executed, so the variable is not set yet
        uint? globalVariableBeingSet = @event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.Value == 1 ? e.ObjectIndex : null;

        return Check(subQuest, globalVariableBeingSet);
    }

    bool IQuestTrigger.CheckItem(Item? item, uint itemCount, SubQuest subQuest)
    {
        if (itemIndices == null || (item != null && !itemIndices.Contains(item.Index)))
            return false;

        return Check(subQuest, null);
    }
}

#endregion


partial class QuestLog
{
    // Global variables of the destroyed windgates which are repaired with the construction staff.
    // The windgate to map 234 (global variable 119) is intact from the start.
    static readonly uint[] WindgateVariables = [115, 116, 117, 118, 120, 121, 122, 123, 124, 125, 126];

    private MainQuest[] CreateQuests(bool advanced, int episode)
    {
        static uint CreateMapEventSourceIndex(uint mapIndex, uint x, uint y)
        {
            uint sourceIndex = x & 0xff;
            sourceIndex <<= 8;
            sourceIndex |= y & 0xff;
            sourceIndex <<= 16;
            sourceIndex |= mapIndex & 0xffff;

            return sourceIndex;
        }

        static uint CreateTextPopupNPCSourceIndex(uint mapIndex, uint mapCharacterIndex)
        {
            uint sourceIndex = mapIndex & 0xffff;
            sourceIndex <<= 16;
            sourceIndex |= mapCharacterIndex & 0xffff;

            return sourceIndex;
        }

        static bool IsEventBitSet(GameCore game, Event? @event, uint mapIndex, uint eventIndex)
        {
            return (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetEventBit && e.ObjectIndex == (mapIndex - 1) * 64 + eventIndex - 1 && e.Value != 0) ||
                game.CurrentSavegame!.GetEventBit(mapIndex, eventIndex - 1);
        }

        static bool IsGlobalVariableSet(GameCore game, Event? @event, uint index)
        {
            return (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.ObjectIndex == index && e.Value != 0) ||
                game.CurrentSavegame!.GetGlobalVariable(index);
        }

        static bool WindgateRepaired(GameCore game, Event? @event) => WindgateVariables.Any(index => IsGlobalVariableSet(game, @event, index));

        static bool IsKeywordLearned(GameCore game, Event? @event, uint index)
        {
            return (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.AddKeyword && e.ObjectIndex == index && e.Value != 0) ||
                game.CurrentSavegame!.IsDictionaryWordKnown(index);
        }

        // Anthony made the antidote from the swamp lilly and the water of life
        // if you own it or used it to heal Sally (34).
        static bool AntidoteMade(GameCore game, Event? @event) => game.GetTotalItemCount(253) != 0 || IsGlobalVariableSet(game, @event, 34);

        // The madman's cell event (267:6) is disabled after you killed him, but also after he
        // got the amulet. In the latter case global variable 29 is set before the event bit.
        static bool MadmanKilled(GameCore game, Event? @event) => IsEventBitSet(game, @event, 267, 6) && !game.CurrentSavegame!.GetGlobalVariable(29);

        Action<SubQuest> AddAsRequiredTo(MainQuestType mainQuest, SubQuestType targetQuest)
        {
            return (subQuest) => Quests.First(q => q.Type == mainQuest).SubQuests.First(q => q.Type == targetQuest).AddRequiredQuest(subQuest.Type);
        }

        Action<SubQuest> CompleteOtherQuest(MainQuestType mainQuest, SubQuestType otherQuest)
        {
            return (_) => Quests.First(q => q.Type == mainQuest).SubQuests.First(q => q.Type == otherQuest).State = QuestState.Completed;
        }

        // Sets active or blocked quests back to inactive (hidden) if they became pointless.
        Action<SubQuest> HideOtherOpenQuests(MainQuestType mainQuest, params SubQuestType[] otherQuests)
        {
            var lookup = new HashSet<SubQuestType>(otherQuests);
            return (_) => Quests.First(q => q.Type == mainQuest).SubQuests.Where(q => lookup.Contains(q.Type) && (q.State == QuestState.Active || q.State == QuestState.Blocked))
                .ToList().ForEach(q => q.State = QuestState.Inactive);
        }

        // Only completes quests which are already active (or blocked), inactive ones stay hidden.
        Action<SubQuest> CompleteOtherActiveQuests(MainQuestType mainQuest, params SubQuestType[] otherQuests)
        {
            var lookup = new HashSet<SubQuestType>(otherQuests);
            return (_) => Quests.First(q => q.Type == mainQuest).SubQuests.Where(q => lookup.Contains(q.Type) && q.State != QuestState.Inactive).ToList().ForEach(q => q.State = QuestState.Completed);
        }

        Action<SubQuest> CompleteOtherQuests(MainQuestType mainQuest, params SubQuestType[] otherQuests)
        {
            var lookup = new HashSet<SubQuestType>(otherQuests);
            return (_) => Quests.First(q => q.Type == mainQuest).SubQuests.Where(q => lookup.Contains(q.Type)).ToList().ForEach(q => q.State = QuestState.Completed);
        }

        // Advanced only quests, some of them only exist since a specific episode.
        List<MainQuest> CreateAdvancedQuests()
        {
            var quests = new List<MainQuest>();

            #region Your House
            quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.YourHouse,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_ShowTheTestamentToTheodorus,
                    Triggers =
                    [
                        // Activate: found grandfather's testament or met Theodorus, the carpenter of Spannenberg
                        // (keyword "Building projects")
                        new OrTrigger<ItemObtainedTrigger, KeywordLearnedTrigger>(game, TriggerType.Activation,
                            (g, t) => new ItemObtainedTrigger(g, t, 206),
                            (g, t) => new KeywordLearnedTrigger(g, t, 115)),
                        // Complete: Theodorus saw the testament
                        new GlobalVariableTrigger(game, TriggerType.Completion, 224),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_HaveTheHouseRefurbished,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Theodorus refurbished grandfather's house (keyword "Refurbishment")
                        new GlobalVariableTrigger(game, TriggerType.Completion, 225),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_AskGustavAboutTheodorus,
                    Triggers =
                    [
                        // Activate: met Gustav, Theodorus' former apprentice, in the inn of Snakesign (keyword "Apprentice")
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 127),
                        // Complete: Gustav told about Theodorus and the spider bite
                        new GlobalVariableTrigger(game, TriggerType.Completion, 226),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 45, // Gustav
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_HaveAWindgateBuilt,
                    Triggers =
                    [
                        // Activate: the house is refurbished and Gustav told that Theodorus can build windgates
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 225) && IsGlobalVariableSet(g, e, 226)),
                        // Complete: Theodorus built a windgate next to the house
                        new GlobalVariableTrigger(game, TriggerType.Completion, 227),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_HaveAStorageCaveDug,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Theodorus dug a storage cave next to the house
                        new GlobalVariableTrigger(game, TriggerType.Completion, 228),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_ExploreTheStorageCave,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: a giant spider attacked in the lower level of the cave
                        new EventDisabledTrigger(game, TriggerType.Completion, 460, 19),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.YourHouse_DefeatTheGiantSpider,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the spider (Theodorus himself) is hidden again after it was shown, or Gustav's
                        // ring from the remains next to it is owned
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                            (g.CurrentSavegame!.GetEventBit(460, 18) && g.CurrentSavegame.GetCharacterBit(460, 0)) || g.GetTotalItemCount(407) != 0),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 37, // Theodorus
                }
            ));
            #endregion

            #region Pets
            quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.Pets,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_FeedTheHungryTomcat,
                    Triggers =
                    [
                        // Activate: talked to the tomcat Kasimir in Spannenberg (needs the language of animals)
                        new GlobalVariableTrigger(game, TriggerType.Activation, 53),
                        // Complete: Kasimir got some food (he moves to another place) or joined the party
                        new CharacterVisibilityTrigger(game, TriggerType.Completion, 273, 4, true),
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.PartyMembers.Any(p => p.Index == 16)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 36, // Kasimir
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_AdoptACat,
                    Triggers =
                    [
                        // Activate: Ferdinand, the gardener of Spannenberg, offers his pets (keyword "Cat")
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 17),
                        // Complete: paid Ferdinand for the cat, it lives in your house now
                        new GlobalVariableTrigger(game, TriggerType.Completion, 230),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 40, // Ferdinand
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_BefriendTheCat,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: played with the cat toy in the house
                        new GlobalVariableTrigger(game, TriggerType.Completion, 232),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 40, // Ferdinand
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_AdoptADog,
                    Triggers =
                    [
                        // Activate: Ferdinand only gives his dog to friends of the cat
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Ferdinand brought the dog to your house
                        new GlobalVariableTrigger(game, TriggerType.Completion, 231),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 40, // Ferdinand
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_BefriendTheDog,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: played with the dog toy in the house
                        new GlobalVariableTrigger(game, TriggerType.Completion, 233),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 40, // Ferdinand
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Pets_ReturnToFerdinand,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Ferdinand gave his finest silver
                        new GlobalVariableTrigger(game, TriggerType.Completion, 234),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 40, // Ferdinand
                }
            ));
            #endregion

            #region The Sea Creatures
            quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.SeaCreatures,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SeaCreatures_AskCaptainTorle,
                    Triggers =
                    [
                        // Activate: Michael told that captain Torle knows about creatures of the sea (keyword)
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 121),
                        // Complete: Torle gave his logbook with the places of the creatures
                        new GlobalVariableTrigger(game, TriggerType.Completion, 238),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 19, // Michael
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SeaCreatures_FindTheSeaCreatures,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 238),
                        // Complete: the five creatures from the logbook were seen at their coordinates
                        new CountTrigger(game, TriggerType.Completion, [239, 240, 242, 243, 244]),
                    ],
                    MinAmount = 5,
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 408, // Torle's logbook
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SeaCreatures_VisitTheMermaid,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the mermaid took the party into her grotto
                        new EventDisabledTrigger(game, TriggerType.Completion, 163, 1),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 408, // Torle's logbook
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SeaCreatures_EnterTheEyeOfTheVortex,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: sailed into the eye of the vortex which the mermaid opened
                        new GlobalVariableTrigger(game, TriggerType.Completion, 241),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 408, // Torle's logbook
                }
            ));
            #endregion

            #region Manyeyes' Castle
            quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.ManyeyesCastle,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ManyeyesCastle_OpenTristansOffice,
                    Triggers =
                    [
                        // Activate: Tristan in Cavetown gave the key of his office in Manyeyes' castle
                        new GlobalVariableTrigger(game, TriggerType.Activation, 245),
                        // Complete: the office door was unlocked
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 39),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 41, // Tristan
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ManyeyesCastle_OpenTheCastle,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the switch in the office unlocked the door to the castle
                        new GlobalVariableTrigger(game, TriggerType.Completion, 246),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 41, // Tristan
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ManyeyesCastle_DefeatTheGuardianOfTheUpperFloor,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the boss on the upper floor was defeated
                        new GlobalVariableTrigger(game, TriggerType.Completion, 247),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 41, // Tristan
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ManyeyesCastle_BanishManyeyesCurse,
                    Final = true,
                    Triggers =
                    [
                        // Activate: fell through the roof into the cellar or defeated the upper boss
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 247) || IsGlobalVariableSet(g, e, 248)),
                        // Complete: the monsters of the cellar are defeated and the curse is banished
                        new GlobalVariableTrigger(game, TriggerType.Completion, 249),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 41, // Tristan
                }
            ));
            #endregion
            #region The Gatekeepers of Cavetown
            quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.Gatekeepers,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Gatekeepers_BringJeffABlindBrandy,
                    Triggers =
                    [
                        // Activate: Jeff, a gatekeeper of Cavetown, told about the passage for ships (or found the brandy)
                        new OrTrigger<GlobalVariableTrigger, ItemObtainedTrigger>(game, TriggerType.Activation,
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_TalkedToJeff),
                            (g, t) => new ItemObtainedTrigger(g, t, 409)),
                        // Complete: Jeff got the blind brandy and gave his gatekeeper key
                        new GlobalVariableTrigger(game, TriggerType.Completion, 250),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 43, // Jeff
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Gatekeepers_FindLukesTreasureMap,
                    Triggers =
                    [
                        // Activate: Luke, the deputy gatekeeper, told about the treasure (or the map was found)
                        new OrTrigger<GlobalVariableTrigger, ItemObtainedTrigger>(game, TriggerType.Activation,
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_TalkedToLuke),
                            (g, t) => new ItemObtainedTrigger(g, t, 410)),
                        // Complete: dug up the old treasure map in the eye of the vortex (or Luke got it already)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(410) != 0 || IsGlobalVariableSet(g, e, 251)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 44, // Luke
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Gatekeepers_GiveTheTreasureMapToLuke,
                    Triggers =
                    [
                        // Activate: found the treasure map
                        new ItemObtainedTrigger(game, TriggerType.Activation, 410),
                        // Complete: Luke got the map and dropped his gatekeeper key
                        new GlobalVariableTrigger(game, TriggerType.Completion, 251),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 44, // Luke
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Gatekeepers_OpenThePassageForShips,
                    Final = true,
                    Triggers =
                    [
                        // Activate: both gatekeeper keys were given
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 250) && IsGlobalVariableSet(g, e, 251)),
                        // Complete: the pressure plate in the gatekeepers' house opened the passage through the rocks
                        new EventDisabledTrigger(game, TriggerType.Completion, 376, 11),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 43, // Jeff
                }
            ));
            #endregion

            if (episode >= 2)
            {
                #region The Golden Armour
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.GoldenArmour,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GoldenArmour_CollectGoldenScales,
                        Triggers =
                        [
                            // Activate: met Morgat in Nalven's magic school (he tells about the golden lizards) or
                            // found a golden scale
                            new OrTrigger<GlobalVariableTrigger, ItemObtainedTrigger>(game, TriggerType.Activation,
                                (g, t) => new GlobalVariableTrigger(g, t, 285),
                                (g, t) => new ItemObtainedTrigger(g, t, 430)),
                            // Complete: own seven golden scales or Morgat made the armour already
                            new CountTrigger(game, TriggerType.Completion, [], [430]),
                            new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_MorgatMadeGoldenArmour),
                        ],
                        MinAmount = 7,
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 46, // Morgat
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GoldenArmour_BringTheScalesToMorgat,
                        Final = true,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Morgat fused the scales with a chain mail
                            new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_MorgatMadeGoldenArmour),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 46, // Morgat
                    }
                ));
                #endregion
                #region The Mystic Creatures
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.MysticCreatures,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticCreatures_FindTheWineCellarOfCrystal,
                        Triggers =
                        [
                            // Activate: the book "Towns of Lyramion" (library of Newlake) tells about Crystal
                            new ItemObtainedTrigger(game, TriggerType.Activation, 466),
                            // Complete: dug up the entrance to the wine cellar of Crystal with a shovel
                            new GlobalVariableTrigger(game, TriggerType.Completion, 260),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticCreatures_FindTheBookOfMysticCreatures,
                        Triggers =
                        [
                            // Activate: found the wine cellar (it leads to the old paladin guild)
                            new GlobalVariableTrigger(game, TriggerType.Activation, 260),
                            // Complete: found the book "Mystic Creatures" in the paladin guild
                            new GlobalVariableTrigger(game, TriggerType.Completion, 268),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticCreatures_CollectTheEssences,
                        Final = true,
                        Triggers =
                        [
                            // Activate: the book names the six mystic creatures
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: the essences of Isdir, Pyrdacor, Marla, Lugthir, Fargon and Gunbar are owned or
                            // were transferred into Kasimir by Sandra
                            new CountTrigger(game, TriggerType.Completion, [262, 263, 264, 265, 266, 267], [438, 439, 440, 441, 442, 443]),
                        ],
                        MinAmount = 6,
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    }
                ));
                #endregion
                #region Sandra's Brooch
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.SandrasBrooch,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.SandrasBrooch_ReturnTheBroochToSandra,
                        Triggers =
                        [
                            // Activate: found Sandra's brooch in the wine cellar of Crystal
                            new ItemObtainedTrigger(game, TriggerType.Activation, 434),
                            // Complete: Sandra got her brooch back
                            new GlobalVariableTrigger(game, TriggerType.Completion, 54),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 5, // Sandra
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.SandrasBrooch_TransferTheEssencesIntoKasimir,
                        Final = true,
                        Triggers =
                        [
                            // Activate: Sandra can transfer the essences of the mystic creatures into Kasimir
                            new GlobalVariableTrigger(game, TriggerType.Activation, 54),
                            // Complete: all six essences were given to Sandra while Kasimir was in the party
                            new CountTrigger(game, TriggerType.Completion, [262, 263, 264, 265, 266, 267]),
                        ],
                        MinAmount = 6,
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 5, // Sandra
                    }
                ));
                #endregion
                #region The Eyrie
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.Eyrie,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.Eyrie_GetTheEagleKeyFromPelanis,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures names Lugthir in the eyrie and the curse of Sansri is lifted
                            new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 268) && IsGlobalVariableSet(g, e, 180)),
                            // Complete: Pelanis gave the eagle key
                            new GlobalVariableTrigger(game, TriggerType.Completion, 304),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 28, // Pelanis
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.Eyrie_OpenTheEyrie,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the door of the eyrie was opened with the eagle key
                            new DoorUnlockedTrigger(game, TriggerType.Completion, 48),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 28, // Pelanis
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.Eyrie_TakeLugthirsEssence,
                        Final = true,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: took Lugthir's essence from the nest
                            new EventDisabledTrigger(game, TriggerType.Completion, 468, 2),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 28, // Pelanis
                    }
                ));
                #endregion
                #region The Black Mountains
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.BlackMountains,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.BlackMountains_AskBaronKarstenAboutPyrdacor,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures names Pyrdacor
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: Baron Karsten told about Pyrdacor and the black mountains (or gave the crystal already)
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 301) || IsGlobalVariableSet(g, e, 302)),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 30, // Baron Karsten
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.BlackMountains_GetTheKeyCrystal,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Karsten gave the inactive crystal (after hearing about the paladin guild or seeing the book)
                            new GlobalVariableTrigger(game, TriggerType.Completion, 302),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 30, // Baron Karsten
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.BlackMountains_HaveLebabChargeTheCrystal,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Lebab turned it into a fire crystal (owned or used at the mountains)
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(426) != 0 || IsEventBitSet(g, e, 118, 3)),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 30, // Baron Karsten
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.BlackMountains_OpenTheBlackMountains,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the fire crystal opened the black mountains
                            new EventDisabledTrigger(game, TriggerType.Completion, 118, 3),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 30, // Baron Karsten
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.BlackMountains_DefeatPyrdacor,
                        Final = true,
                        Triggers =
                        [
                            // Activate: the mountains are open
                            new EventDisabledTrigger(game, TriggerType.Activation, 118, 3),
                            // Complete: Pyrdacor was defeated for the third time
                            new GlobalVariableTrigger(game, TriggerType.Completion, 270),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 30, // Baron Karsten
                    }
                ));
                #endregion
                #region The Desert Temple
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.DesertTemple,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DesertTemple_SolveTheFacePuzzle,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures names Marla in the sighing desert (which swallows the party)
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: the eyes of the face are closed and its mouth is open
                            new GlobalVariableTrigger(game, TriggerType.Completion, 286),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DesertTemple_AnswerTheRiddlemouths,
                        Triggers =
                        [
                            // Activate: the passages of peace are reachable
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: all three riddlemouths are answered (sand, magma or lava, swamp or marsh)
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                                IsEventBitSet(g, e, 474, 2) && IsEventBitSet(g, e, 474, 3) && IsEventBitSet(g, e, 474, 4)),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DesertTemple_DefeatMarla,
                        Final = true,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Marla was defeated
                            new EventDisabledTrigger(game, TriggerType.Completion, 474, 32),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    }
                ));
                #endregion
                #region The Paladin Guild of Crystal
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.PaladinGuild,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_BreakThroughTheCrackedWall,
                        Triggers =
                        [
                            // Activate: found the wine cellar of Crystal
                            new GlobalVariableTrigger(game, TriggerType.Activation, 260),
                            // Complete: the cracked wall in the wine cellar was smashed with the giant mallet
                            new EventDisabledTrigger(game, TriggerType.Completion, 470, 31),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_LightTheCandles,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the four candles in the first hall of the guild burn
                            new GlobalVariableTrigger(game, TriggerType.Completion, 259),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_SummonThePaladinMaster,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: ten paladin emblems summoned the paladin master (only with Gryban in the party)
                            new EventDisabledTrigger(game, TriggerType.Completion, 472, 4),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_DefeatThePaladinMaster,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the paladin master is hidden again after he was summoned
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                                g.CurrentSavegame!.GetEventBit(472, 3) && g.CurrentSavegame.GetCharacterBit(472, 19)),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_OpenTheExitOfTheGuild,
                        Triggers =
                        [
                            // Activate: reached the second part of the guild
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the switch opened the exit to the surface
                            new GlobalVariableTrigger(game, TriggerType.Completion, 261),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 466, // Towns of Lyramion
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_TakeGunbarsEssence,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures was found in the guild
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: Gunbar's statue released his essence
                            new GlobalVariableTrigger(game, TriggerType.Completion, 400),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.PaladinGuild_FindFargonsEssence,
                        Final = true,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures was found in the guild
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: Fargon's essence is owned, its chest (181) was unlocked or it was transferred into Kasimir
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                                g.GetTotalItemCount(442) != 0 || !g.CurrentSavegame!.IsChestLocked(180) || IsGlobalVariableSet(g, e, 266)),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 444, // Mystic Creatures
                    }
                ));
                #endregion
                #region Isdir's Trials
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.IsdirsTrials,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_AskJeffAboutIsdir,
                        Triggers =
                        [
                            // Activate: the book of mystic creatures names Isdir near Cavetown
                            new GlobalVariableTrigger(game, TriggerType.Activation, 268),
                            // Complete: Jeff (after his blind brandy) remembered the strange event at the northern beach
                            new GlobalVariableTrigger(game, TriggerType.Completion, 305),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 43, // Jeff
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_MeetIsdirAtNight,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the way to Isdir's cave appeared at the northern beach at night
                            new EventDisabledTrigger(game, TriggerType.Completion, 370, 14),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 43, // Jeff
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_BringGoldenWater,
                        Triggers =
                        [
                            // Activate: the crystal asked for it
                            new GlobalVariableTrigger(game, TriggerType.Activation, 291),
                            // Complete: the golden water from Lebab was poured onto the crystal
                            new GlobalVariableTrigger(game, TriggerType.Completion, 287),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(377, 7, 4), // Isdir
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_BringARestlessSoul,
                        Triggers =
                        [
                            // Activate: the crystal asked for it
                            new GlobalVariableTrigger(game, TriggerType.Activation, 292),
                            // Complete: the ghost soul from the graveyard was given to the crystal
                            new GlobalVariableTrigger(game, TriggerType.Completion, 288),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(377, 7, 4), // Isdir
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_BringAPoisonousSnake,
                        Triggers =
                        [
                            // Activate: the crystal asked for it
                            new GlobalVariableTrigger(game, TriggerType.Activation, 293),
                            // Complete: the poisonous snake from the island was frozen by the crystal
                            new GlobalVariableTrigger(game, TriggerType.Completion, 289),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(377, 7, 4), // Isdir
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_BringTheBlueCrystal,
                        Triggers =
                        [
                            // Activate: the crystal asked for it
                            new GlobalVariableTrigger(game, TriggerType.Activation, 294),
                            // Complete: the small blue crystal merged with the ice crystal
                            new GlobalVariableTrigger(game, TriggerType.Completion, 290),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(377, 7, 4), // Isdir
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.IsdirsTrials_ReceiveIsdirsEssence,
                        Final = true,
                        Triggers =
                        [
                            // Activate: all four ice crystals are satisfied
                            new PredicateTrigger(game, TriggerType.Activation, (g, e) =>
                                IsGlobalVariableSet(g, e, 287) && IsGlobalVariableSet(g, e, 288) && IsGlobalVariableSet(g, e, 289) && IsGlobalVariableSet(g, e, 290)),
                            // Complete: Isdir handed over his essence
                            new GlobalVariableTrigger(game, TriggerType.Completion, 298),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(377, 7, 4), // Isdir
                    }
                ));
                #endregion
            }

            if (episode >= 3)
            {
                #region The Gral Temple
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.GralTemple,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_AskMelvinAboutHisFamily,
                        Triggers =
                        [
                            // Activate: met Melvin in Snakesign (keyword "Family")
                            new KeywordLearnedTrigger(game, TriggerType.Activation, 146),
                            // Complete: he told the legend of his ancestor Melvin (keywords "Gral swamps" or "Melvin")
                            // or the gate in the mountains is open already
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 310) || IsEventBitSet(g, e, 183, 1)),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 47, // Melvin
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_SayGralAtNight,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: "Gral" was said at night in front of the mountains north of the swamps
                            new EventDisabledTrigger(game, TriggerType.Completion, 183, 1),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 47, // Melvin
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_OpenTheInnerDoor,
                        Triggers =
                        [
                            // Activate: the gate in the mountains is open
                            new EventDisabledTrigger(game, TriggerType.Activation, 183, 1),
                            // Complete: the inner door was opened with the gral key
                            new OrTrigger<DoorUnlockedTrigger, EventDisabledTrigger>(game, TriggerType.Completion,
                                (g, t) => new DoorUnlockedTrigger(g, t, 52),
                                (g, t) => new EventDisabledTrigger(g, t, 475, 25)),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(183, 36, 20), // Gate in the mountains
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_PlaceTheHeavyStoneOnTheAltar,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the heavy stone lies on the altar
                            new EventDisabledTrigger(game, TriggerType.Completion, 475, 29),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(183, 36, 20), // Gate in the mountains
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_DefeatDiokhar,
                        Triggers =
                        [
                            // Activate: the altar opened the way or Diokhar was met
                            new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsEventBitSet(g, e, 475, 29) || IsGlobalVariableSet(g, e, 315)),
                            // Complete: Diokhar was defeated
                            new EventDisabledTrigger(game, TriggerType.Completion, 475, 36),
                        ],
                        SourceType = QuestSourceType.MapEvent,
                        SourceIndex = CreateMapEventSourceIndex(183, 36, 20), // Gate in the mountains
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_FindMelvinsRemains,
                        Triggers =
                        [
                            // Activate: the gate in the mountains is open
                            new EventDisabledTrigger(game, TriggerType.Activation, 183, 1),
                            // Complete: found the emerald necklace in the old sewers or Melvin got it already
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(483) != 0 || IsGlobalVariableSet(g, e, 322)),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 47, // Melvin
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.GralTemple_BringTheNecklaceToMelvin,
                        Final = true,
                        Triggers =
                        [
                            // Activate: found the emerald necklace
                            new ItemObtainedTrigger(game, TriggerType.Activation, 483),
                            // Complete: Melvin saw the necklace of his ancestor
                            new GlobalVariableTrigger(game, TriggerType.Completion, 322),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 47, // Melvin
                    }
                ));
                #endregion
                #region The Mystic Tower
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.MysticTower,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticTower_VisitThePond,
                        Triggers =
                        [
                            // Activate: Targor's notes (Targor brings them when he joins, so no item is added)
                            new PredicateTrigger(game, TriggerType.Activation, (g, e) => g.GetTotalItemCount(507) != 0),
                            // Complete: Targor felt the energy of the small pond west of Illien
                            new EventDisabledTrigger(game, TriggerType.Completion, 85, 1),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 507, // Targor's notes
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticTower_VisitTheLonelyTree,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Targor felt the energy of the lonely tree
                            new EventDisabledTrigger(game, TriggerType.Completion, 102, 1),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 507, // Targor's notes
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticTower_VisitTheMountains,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the third symbol made the mystic tower rise
                            new EventDisabledTrigger(game, TriggerType.Completion, 134, 1),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 507, // Targor's notes
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticTower_EnterTheMysticTower,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the stone head let Targor in
                            new EventDisabledTrigger(game, TriggerType.Completion, 478, 2),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 507, // Targor's notes
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MysticTower_PassTheTestOfHighMysticism,
                        Final = true,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Targor defeated the demon with the mystic imitation
                            new OrTrigger<GlobalVariableTrigger, EventDisabledTrigger>(game, TriggerType.Completion,
                                (g, t) => new GlobalVariableTrigger(g, t, 371),
                                (g, t) => new EventDisabledTrigger(g, t, 478, 18)),
                        ],
                        SourceType = QuestSourceType.Item,
                        SourceIndex = 507, // Targor's notes
                    }
                ));
                #endregion
                #region The Master Thief
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.MasterThief,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_HelpAmanAgainstTheKnights,
                        Triggers =
                        [
                            // Activate: Aman asked for help against the knights in the weapons chamber of Gemstone
                            new GlobalVariableTrigger(game, TriggerType.Activation, 376),
                            // Complete: the knights are gone and Aman offers the highest thieves' test
                            new GlobalVariableTrigger(game, TriggerType.Completion, 375),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_MeetAmanInTheThievesGuild,
                        Triggers =
                        [
                            // Activate
                            new GlobalVariableTrigger(game, TriggerType.Activation, 375),
                            // Complete: Aman gave the silver lockpick in the cellar of the thieves' guild
                            new GlobalVariableTrigger(game, TriggerType.Completion, 377),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_SolveTheBarrelRiddle,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the old items were put into the right barrels (the stealth cloak is unlocked)
                            new GlobalVariableTrigger(game, TriggerType.Completion, 391),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_ShowTheCloakToAman,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Aman saw the stealth cloak and gave his key
                            new GlobalVariableTrigger(game, TriggerType.Completion, 380),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_PassTheTestOfStealth,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: the final switch behind the torches was used, Aman waits in the small room
                            new CharacterVisibilityTrigger(game, TriggerType.Completion, 265, 20, true),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.MasterThief_TalkToAman,
                        Final = true,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Aman revealed himself as the head of the guild
                            new GlobalVariableTrigger(game, TriggerType.Completion, 378),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 3, // Aman
                    }
                ));
                #endregion
                #region Drelb's Search
                quests.Add(QuestFactory.CreateMainQuest(this, MainQuestType.DrelbsSearch,
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DrelbsSearch_DefeatTheMonstersInTheMine,
                        Triggers =
                        [
                            // Activate: met Drelb, the search trainer of the gnomes
                            new GlobalVariableTrigger(game, TriggerType.Activation, 362),
                            // Complete: the four monsters in the labyrinth of the gnomes (map 409) are defeated
                            // or the search already started
                            new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 369) ||
                                Enumerable.Range(1, 4).All(index => g.CurrentSavegame!.GetCharacterBit(409, (uint)index))),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 73, // Drelb
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DrelbsSearch_AskDrelbAboutHisSearch,
                        Triggers =
                        [
                            // Activate
                            new PreviousSubQuestCompletedTrigger(),
                            // Complete: Drelb prepared the mine and hid six things
                            new GlobalVariableTrigger(game, TriggerType.Completion, 369),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 73, // Drelb
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DrelbsSearch_FindTheHidingPlaces,
                        Triggers =
                        [
                            // Activate: Drelb hid the things
                            new GlobalVariableTrigger(game, TriggerType.Activation, 369),
                            // Complete: the five hiding places (364-368) and Drelb's certificate in the last one
                            // (owned or given to Drelb)
                            new CountTrigger(game, TriggerType.Completion, [364, 365, 366, 367, 368, 363], [496]),
                        ],
                        MinAmount = 6,
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 73, // Drelb
                    },
                    mainQuest => new SubQuest(this, mainQuest)
                    {
                        Type = SubQuestType.DrelbsSearch_GiveTheCertificateToDrelb,
                        Final = true,
                        Triggers =
                        [
                            // Activate: found Drelb's certificate
                            new ItemObtainedTrigger(game, TriggerType.Activation, 496),
                            // Complete: Drelb got the certificate and gave his brooch
                            new GlobalVariableTrigger(game, TriggerType.Completion, 363),
                        ],
                        SourceType = QuestSourceType.NPC,
                        SourceIndex = 73, // Drelb
                    }
                ));
                #endregion
            }

            return quests;
        }

        // TODO ...
        return
        [
            #region Original quests
            #region Lyramion's Faith
            QuestFactory.CreateMainQuest(this, MainQuestType.LyramionsFaith,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_TalkToShandraInNewlake,
                    Triggers =
                    [
                        // Activate
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 1), // "Wine" (same dialogue as for grandfather)
                        // Completion
                        new GlobalVariableTrigger(game, TriggerType.Completion, 193), // Talked to Shandra in Newlake
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_EnterTheTempleOfBrotherhood,
                    // blocked until the demon sleep is brewed (but the guard demon can also be killed)
                    RequiredCompletedQuests = [SubQuestType.LyramionsFaith_BrewDemonSleep],
                    // The guard demon might be killed instead of using the demon sleep. The open
                    // library steps are hidden then (they show up again when done anyway).
                    PostCompletionAction = HideOtherOpenQuests(MainQuestType.LyramionsFaith, SubQuestType.LyramionsFaith_EnterSecretRoomInLibrary,
                        SubQuestType.LyramionsFaith_FindRecipe, SubQuestType.LyramionsFaith_BrewDemonSleep),
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new EventDisabledTrigger(game, TriggerType.Completion, 431, 6), // Uses demon sleep and removed the guard demon
                    ],
                    SourceType = QuestSourceType.Custom,
                    SourceIndex = (uint)QuestTexts.CustomSourceName.Shandra,
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_ExploreTheTempleOfBrotherhood,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion: picked up the hangar key (S'Orel's chest) or used it at the hangar door
                        new OrTrigger<ItemObtainedTrigger, ChestUnlockedTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 370), // Hangar key
                            (g, t) => new ChestUnlockedTrigger(g, t, 111)), // S'Orel's chest
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 30), // Hangar door
                    ],
                    SourceType = QuestSourceType.Custom,
                    SourceIndex = (uint)QuestTexts.CustomSourceName.Shandra,
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_ExploreTheHangar,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 381), // Kire's note
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 370, // Hangard key
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_FindTheNavStone,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion: own the green navstone or opened its chest (the navstone is used up when flying)
                        new OrTrigger<ItemObtainedTrigger, ChestUnlockedTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 378), // Green navstone
                            (g, t) => new ChestUnlockedTrigger(g, t, 116)),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 381, // Kire's note
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_FlyToTheForestMoon,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        // Complete: long text triggered in Dor Kiredon or met Kire already (51)
                        new OrTrigger<EventDisabledTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new EventDisabledTrigger(g, t, 343, 16),
                            (g, t) => new GlobalVariableTrigger(g, t, 51)),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 381, // Kire's note
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_MeetTheDwarfLeader,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new GlobalVariableTrigger(game, TriggerType.Completion, 51), // Talked to Kire once
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_FindAWayToLeaveForestMoon,
                    // the story continues on Morag (the steps are defined after the library steps)
                    PostCompletionAction = subQuest =>
                    {
                        var moragQuest = subQuest.Quest.SubQuests.First(q => q.Type == SubQuestType.LyramionsFaith_FlyToMorag);

                        if (moragQuest.State == QuestState.Inactive)
                            moragQuest.State = QuestState.Active;
                    },
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        // Complete: own the blue or yellow navstone or already flew away with it (it is used up)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 377, 379), // Blue or yellow navstone
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_LeftForestMoon)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_BringShandrasStoneToGrandfather,
                    Triggers =
                    [
                        // Activate
                        new ItemObtainedTrigger(game, TriggerType.Activation, 209), // Shandra's Amber
                        // Completion: showed the amber to grandfather, used it at Shandra's coffin (193)
                        // or grandfather died (11 = cave-in removed) before it was shown to him
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, GlobalVar_ShowedAmberToGrandfather) ||
                            IsGlobalVariableSet(g, e, 193) || (IsGlobalVariableSet(g, e, 11) && g.GetTotalItemCount(209) != 0)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_UseShandrasStone,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new EventDisabledTrigger(game, TriggerType.Completion, 428, 7), // Picked up the book key
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_EnterSecretRoomInLibrary,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 29), // Unlocked secret door in library
                    ],
                    SourceType = QuestSourceType.Custom,
                    SourceIndex = (uint)QuestTexts.CustomSourceName.Shandra,
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_FindRecipe,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 345), // Recipe
                    ],
                    SourceType = QuestSourceType.Custom,
                    SourceIndex = (uint)QuestTexts.CustomSourceName.Shandra,
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_BrewDemonSleep,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion: own the demon sleep or brewed it already (it is used up at the temple)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 343), // Demon Sleep
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_BrewedDemonSleep)),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 345, // Recipe
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_FlyToMorag,
                    Triggers =
                    [
                        // Activated when the way to leave the forest moon was found
                        // Complete: arrived on Morag (prison of S'angrila) or talked to S'riel (222)
                        new OrTrigger<EventDisabledTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new EventDisabledTrigger(g, t, 439, 2),
                            (g, t) => new GlobalVariableTrigger(g, t, 222)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_TalkToSriel,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the dying Moranian in the prison told about Morag and the crystal strings
                        new GlobalVariableTrigger(game, TriggerType.Completion, 222),
                    ],
                    SourceType = QuestSourceType.MapEvent,
                    SourceIndex = CreateMapEventSourceIndex(439, 8, 13), // Prison of S'angrila
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_SearchSrielsPalace,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: opened the chest with the Morag map, the key to S'orel's palace and the first crystal string
                        new OrTrigger<ChestUnlockedTrigger, ItemObtainedTrigger>(game, TriggerType.Completion,
                            (g, t) => new ChestUnlockedTrigger(g, t, 117),
                            (g, t) => new ItemObtainedTrigger(g, t, 387, 388)), // Morag map, S'orel's palace key
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 32, // S'riel
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_CollectTheCrystalStrings,
                    Triggers =
                    [
                        // Activate: S'riel told about the harp with crystal strings
                        new GlobalVariableTrigger(game, TriggerType.Activation, 222),
                        // Complete: own the seven crystal strings (S'riel's palace and the councillors of Morag)
                        new CountTrigger(game, TriggerType.Completion, [], [354]),
                        // or the harp was strung already (the strings are used then)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(159) != 0 || IsEventBitSet(g, e, 431, 3)),
                    ],
                    MinAmount = 7,
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 32, // S'riel
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_HaveTheHarpStrung,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Matthias in Illien fitted the strings to an elven harp (crystal harp)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(159) != 0 || IsEventBitSet(g, e, 431, 3)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 29, // Matthias
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_ShatterTheCrystalWall,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: played the crystal harp in front of the crystal wall in the temple
                        new EventDisabledTrigger(game, TriggerType.Completion, 431, 3),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 32, // S'riel
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.LyramionsFaith_DestroyTheMoranianMachine,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the Moranian machine behind the crystal wall was destroyed
                        new EventDisabledTrigger(game, TriggerType.Completion, 453, 4),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 32, // S'riel
                }
            ),
            #endregion
            #region Grandfather's Quest
            QuestFactory.CreateMainQuest(this, MainQuestType.Grandfather,
                mainQuest => new SubQuest(this, mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_TalkToGrandfather,
                    Triggers =
                    [
                        // Completion
                        new KeywordLearnedTrigger(game, TriggerType.Completion, 1), // "Wine"
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new TileChangeTrigger(game, TriggerType.Completion, 259, 10, 17, 0), // remove Riddlemouth in cellar
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_FindHisEquipment,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new ChestUnlockedTrigger(game, TriggerType.Completion, 23), // Grandfather's old chest
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_ReturnToGrandfather,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new NextSubQuestActivatedTrigger(),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_VisitGrave,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new CharacterVisibilityTrigger(game, TriggerType.Activation, 266, 1, true), // spawned Anthony in house of healers
                        // Complete
                        new EventDisabledTrigger(game, TriggerType.Completion, 263, 9), // visit grave
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_TellGrandfatherAboutCaveIn,
                    PostActivationAction = AddAsRequiredTo(mainQuest, SubQuestType.Grandfather_FindHisEquipment),
                    Triggers =
                    [
                        // Activate
                        new EventDisabledTrigger(game, TriggerType.Activation, 260, 13), // encounter cave-in (text popup)
                        // Complete
                        new KeywordLearnedTrigger(game, TriggerType.Completion, 3), // "Tools", not ideal, but there is nothing else to trigger on
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_FindTolimar,
                    PostActivationAction = subQuest =>
                    {
                        AddAsRequiredTo(mainQuest, SubQuestType.Grandfather_FindHisEquipment)(subQuest);
                        CompleteOtherQuest(mainQuest, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn)(subQuest);
                    },
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Tolimar told about the horseshoes or got them already (without asking for tools)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                            IsGlobalVariableSet(g, e, GlobalVar_TolimarQuestStarted) || IsGlobalVariableSet(g, e, GlobalVar_ReturnedHorseshoesToTolimar)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_RemoveCaveIn,
                    RequiredCompletedQuests = [SubQuestType.GoldenHorseshoes_ReturnHorseshoes],
                    PostActivationAction = AddAsRequiredTo(mainQuest, SubQuestType.Grandfather_FindHisEquipment),
                    PostCompletionAction = CompleteOtherQuests(MainQuestType.Grandfather, SubQuestType.Grandfather_FindTolimar, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn),
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.AlwaysComplete, 11), // Removed cave-in
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                }
            ),
            #endregion
            #region Swamp fever
            QuestFactory.CreateMainQuest(this, MainQuestType.SwampFever,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_TalkToFatherAnthony,
                    Triggers =
                    [
                        // Activate
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 12), // "ANTIDOTE", not ideal, but there is nothing else to trigger on
                        // Complete
                        new NextSubQuestActivatedTrigger(),
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 26) || AntidoteMade(g, e)), // talked to Antonius about Antidote
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 12, // Wat the fisherman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_ObtainSwampLilly,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 26), // talked to Antonius about Antidote
                        // Complete: own the swamp lilly or Anthony already made the antidote
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => (IsGlobalVariableSet(g, e, 26) && g.GetTotalItemCount(255) != 0) || AntidoteMade(g, e)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_ObtainEmptyBottle,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 26), // talked to Antonius about Antidote
                        // Complete: empty bottle, but also water of life will work, or Anthony already made the antidote
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => (IsGlobalVariableSet(g, e, 26) && (g.GetTotalItemCount(218) != 0 || g.GetTotalItemCount(254) != 0)) || AntidoteMade(g, e)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_ObtainWaterOfLife,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 26), // talked to Antonius about Antidote
                        // Complete: own the water of life or Anthony already made the antidote
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => (IsGlobalVariableSet(g, e, 26) && g.GetTotalItemCount(254) != 0) || AntidoteMade(g, e)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_ReturnToAnthony,
                    PostActivationAction = CompleteOtherQuests
                    (
                        MainQuestType.SwampFever,
                        SubQuestType.SwampFever_ObtainEmptyBottle,
                        SubQuestType.SwampFever_ObtainSwampLilly,
                        SubQuestType.SwampFever_ObtainWaterOfLife
                    ),
                    Triggers =
                    [
                        // Activate: own both the swamp lilly and the water of life
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => g.GetTotalItemCount(254) != 0 && g.GetTotalItemCount(255) != 0),
                        // Complete
                        new NextSubQuestActivatedTrigger(),
                        new PredicateTrigger(game, TriggerType.Completion, AntidoteMade),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_HealSally,
                    Triggers =
                    [
                        // Activate
                        new ItemObtainedTrigger(game, TriggerType.Activation, 253), // antidote
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 34), // spawn recovered Sally
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 12, // Wat the fisherman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_TalkToSally,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 36), // received reward from Sally
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 13, // Sally
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SwampFever_TalkToWat,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 34), // spawn recovered Sally
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 35), // received reward from Wat
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 12, // Wat the fisherman
                }
            ),
            #endregion
            #region Golden horseshoes
            QuestFactory.CreateMainQuest(this, MainQuestType.GoldenHorseshoes,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.GoldenHorseshoes_FindHorseshoes,
                    PostActivationAction = CompleteOtherActiveQuests(MainQuestType.Grandfather, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, SubQuestType.Grandfather_FindTolimar),
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_TolimarQuestStarted),
                        // Complete: own all four horseshoes or already returned them to Tolimar
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 270), // Golden horseshoe
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_ReturnedHorseshoesToTolimar)),
                    ],
                    MinAmount = 4,
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 10, // Tolimar
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.GoldenHorseshoes_ReturnHorseshoes,
                    Final = true,
                    PostActivationAction = CompleteOtherActiveQuests(MainQuestType.Grandfather, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, SubQuestType.Grandfather_FindTolimar),
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_ReturnedHorseshoesToTolimar), // Returned golden horseshoes
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 10, // Tolimar
                }
            ),
            #endregion
            #region Alkem's Ring
            QuestFactory.CreateMainQuest(this, MainQuestType.AlkemsRing,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.AlkemsRing_EnterTheCrypt,
                    Triggers =
                    [
                        // Activate
                        new ItemObtainedTrigger(game, TriggerType.Activation, 289), // Crypt key
                        // Complete
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 15), // Unlocked crypt door
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.AlkemsRing_FindTheRing,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: own Alkem's ring or already returned it (81)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 288), // Alkem's ring
                            (g, t) => new GlobalVariableTrigger(g, t, 81)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.AlkemsRing_ReturnTheRing,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 81), // Returned the ring
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                }
            ),
            #endregion
            #region Sylphs
            QuestFactory.CreateMainQuest(this, MainQuestType.Sylphs,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Sylphs_TalkToLadyHeidi,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_SylphQuestStarted), // Talked to cook
                        // Complete: Lady Heidi told about the fairies (keywords "Fairies" and "Fairy" are only typed by the player)
                        // or the quest went on without her (sylph cave door opened, Sariel told about her sister, Selena rescued)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, GlobalVar_HeidiToldAboutFairies) ||
                            g.CurrentSavegame!.IsDoorLocked(10) == false || IsGlobalVariableSet(g, e, 78) || IsGlobalVariableSet(g, e, 79)),
                    ],
                    SourceType = QuestSourceType.TextPopupNPC,
                    SourceIndex = CreateTextPopupNPCSourceIndex(269, 0), // Cook
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Sylphs_FindTheHiddenItemInTheTree,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: own the weird stone or already opened the door of the sylph cave with it
                        new OrTrigger<ItemObtainedTrigger, DoorUnlockedTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 283), // Weird stone
                            (g, t) => new DoorUnlockedTrigger(g, t, 10)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 9, // Lady Heidi
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Sylphs_FindTheSylphs,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 78), // Learned Sylphic and word "Oknard" from Sariel, TODO: maybe also use it to unlock Orc progression?
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 9, // Lady Heidi
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Sylphs_RescueSelena,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new GlobalVariableTrigger(game, TriggerType.Completion, 79), // Talked to Selena after rescue
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 17, // Sariel
                }
            ),
            #endregion
            #region Orcs
            QuestFactory.CreateMainQuest(this, MainQuestType.OrcPlague,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.OrcPlague_DefeatTheOrcLeader,
                    Triggers =
                    [
                        // Activate: Freiherr Georg (head of Spannenberg) tells about the two problems and teaches the keyword "Probleme"
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 23), // PROBLEME
                        // Complete: obtained the hill giant's head (item 268, proof the orc leader is dead),
                        // OR the head was already handed to Baron George (the custom global variable below).
                        // The latter is needed so this step stays completed after a save/reload, when the head
                        // is no longer in the inventory (ItemObtainedTrigger only checks items you currently own).
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 268), // KOPF HÜGELRIESEN
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_GaveOrcLeaderHeadToBaronGeorge)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 8, // Freiherr Georg von Spannenberg
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.OrcPlague_ReturnToGeorg,
                    Final = true,
                    Triggers =
                    [
                        // Activate: once the leader is defeated (head obtained)
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: hand the giant's head to Baron George. The original game changes nothing in the
                        // savegame here, so a custom global variable is set on that GiveItem event (see
                        // QuestCustomVariables) and checked here -- this persists the progress across a save/reload.
                        new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_GaveOrcLeaderHeadToBaronGeorge),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 8, // Freiherr Georg von Spannenberg
                }
            ),
            #endregion
            #region Thieves
            QuestFactory.CreateMainQuest(this, MainQuestType.ThiefPlague,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ThiefPlague_DealWithNagier,
                    Triggers =
                    [
                        // Activate: Freiherr Georg tells about the two problems and teaches the keyword "Probleme"
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 23), // PROBLEME
                        // Complete: got proof that Nagier was dealt with -- the silver hand (266, if killed) or his
                        // letter (267, if you made peace) -- OR you already handed it to Baron George (custom variable).
                        // The variable keeps the step completed after a save/reload, when the proof is no longer owned.
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 266, 267), // SILBERHAND or NAGIERS BRIEF
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_GaveThiefProofToBaronGeorge)),
                        new GlobalVariableTrigger(game, TriggerType.Completion, 60), // made peace with Nagier
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 8, // Freiherr Georg von Spannenberg
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ThiefPlague_ReturnToGeorg,
                    Final = true,
                    Triggers =
                    [
                        // Activate: once Nagier has been dealt with (proof obtained)
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: hand the proof (silver hand or letter) to Baron George. He changes nothing in the
                        // savegame, so a custom global variable is set on that GiveItem event (see QuestCustomVariables).
                        new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_GaveThiefProofToBaronGeorge),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 8, // Freiherr Georg von Spannenberg
                }
            ),
            #endregion
            #region Wine Trophies
            QuestFactory.CreateMainQuest(this, MainQuestType.WineTrophies,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WineTrophies_FindThem,
                    Triggers =
                    [
                        // Activate: Canth tells about the goblets in her greeting and adds the keyword "Wine goblets"
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 28),
                        // Complete: owned goblets plus the ones already returned to Canth (37) and Norlael (38)
                        new CountTrigger(game, TriggerType.Completion, [37, 38], [272]),
                    ],
                    MinAmount = 2,
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 14, // Canth
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WineTrophies_ReturnThem,
                    Final = true,
                    Triggers =
                    [
                        // Activate: found a goblet or returned one already
                        new CountTrigger(game, TriggerType.Activation, [37, 38], [272]),
                        // Complete: each of them accepts one goblet
                        new CountTrigger(game, TriggerType.Completion, [37, 38]),
                    ],
                    MinAmount = 2,
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 14, // Canth
                }
            ),
            #endregion
            #region Chain of Office
            QuestFactory.CreateMainQuest(this, MainQuestType.ChainOfOffice,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ChainOfOffice_FindIt,
                    Triggers =
                    [
                        // Activate: Lady Heidi or Baron George told about the fairies and the stolen chain
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_ChainOfOfficeQuestStarted),
                        // Complete: found the chain (Sylph cave) or already returned it (31)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 269), // Chain of office
                            (g, t) => new GlobalVariableTrigger(g, t, 31)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 9, // Lady Heidi
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ChainOfOffice_ReturnIt,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: gave the chain to Baron George
                        new GlobalVariableTrigger(game, TriggerType.Completion, 31),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 8, // Baron George
                }
            ),
            #endregion
            #region Monstereye
            QuestFactory.CreateMainQuest(this, MainQuestType.Monstereye,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Monstereye_BuyIt,
                    Final = true,
                    Triggers =
                    [
                        // Activate: Sandire offered the monster eye (or it was bought right away)
                        new OrTrigger<GlobalVariableTrigger, GlobalVariableTrigger>(game, TriggerType.Activation,
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_TalkedToSandire),
                            (g, t) => new GlobalVariableTrigger(g, t, 221)),
                        // Complete: gave 5000 gold to Sandire
                        new GlobalVariableTrigger(game, TriggerType.Completion, 221),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 35, // Sandire
                }
            ),
            #endregion
            #region Graveyard
            QuestFactory.CreateMainQuest(this, MainQuestType.Graveyard,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Graveyard_FindTheGardener,
                    Triggers =
                    [
                        // Activate: Aman asked for the gardener's brooch (keyword "Thing")
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_AmanAskedForBrooch),
                        // Complete: found the hole in the hedge where the gardener (zombie master) appears
                        new EventDisabledTrigger(game, TriggerType.Completion, 263, 24),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 3, // Aman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Graveyard_GetBroochFromGravedigger,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: got the necromancer brooch or already gave it to Aman (12)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 238), // Necromancer brooch
                            (g, t) => new GlobalVariableTrigger(g, t, 12)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 3, // Aman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Graveyard_ReturnBroochToAman,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: gave the brooch to Aman and got the password
                        new GlobalVariableTrigger(game, TriggerType.Completion, 12),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 3, // Aman
                }
            ),
            #endregion
            #region Thief Guild
            QuestFactory.CreateMainQuest(this, MainQuestType.ThiefGuild,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ThiefGuild_Enter,
                    Triggers =
                    [
                        // Activate: got the password from Aman
                        new GlobalVariableTrigger(game, TriggerType.Activation, 12),
                        // Complete: the riddlemouth in the wine cellar opened the way
                        new EventDisabledTrigger(game, TriggerType.Completion, 265, 4),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 3, // Aman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ThiefGuild_PassTheTest,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: opened the reward chest at the end of the test
                        new ChestUnlockedTrigger(game, TriggerType.Completion, 36),
                    ],
                    SourceType = QuestSourceType.MapEvent,
                    SourceIndex = CreateMapEventSourceIndex(265, 15, 12), // Sign of the test
                }
            ),
            #endregion
            #region Madman
            QuestFactory.CreateMainQuest(this, MainQuestType.Madman,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Madman_VisitTheMadman,
                    Triggers =
                    [
                        // Activate: Clementine told about the madman in the cellar
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_ClementineToldAboutMadman),
                        // Complete: watched the madman in his cell
                        new EventDisabledTrigger(game, TriggerType.Completion, 267, 4),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 6, // Clementine
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Madman_FindTheSecondPartOfTheAmulet,
                    Triggers =
                    [
                        // Activate: found a part of the amulet (B O S in the cellar, T A R from Thornahuun)
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => !MadmanKilled(g, e) &&
                            (g.GetTotalItemCount(263) + g.GetTotalItemCount(264) != 0 || g.GetTotalItemCount(265) != 0 || IsGlobalVariableSet(g, e, 29))),
                        // Complete: own both parts, the united amulet or the madman already got it (29).
                        // If the madman was killed, the amulet is useless and the step stays hidden.
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 29) || (!MadmanKilled(g, e) &&
                            ((g.GetTotalItemCount(263) != 0 && g.GetTotalItemCount(264) != 0) || g.GetTotalItemCount(265) != 0))),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 6, // Clementine
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Madman_UniteTheAmulet,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Theresa in Burnville united the parts (or the madman already got the amulet)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 29) ||
                            (!MadmanKilled(g, e) && g.GetTotalItemCount(265) != 0)), // T A R B O S amulet
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 22, // Theresa
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Madman_GiveTheAmuletToTheMadman,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the madman got the amulet and became Tar the dark
                        new GlobalVariableTrigger(game, TriggerType.Completion, 29),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 7, // Madman
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Madman_KillTheMadman,
                    Final = true,
                    // The amulet is useless then
                    PostCompletionAction = subQuest =>
                    {
                        foreach (var amuletQuest in subQuest.Quest.SubQuests.Where(q => q.Type != SubQuestType.Madman_VisitTheMadman && q != subQuest && q.State != QuestState.Completed))
                            amuletQuest.State = QuestState.Inactive;
                    },
                    Triggers =
                    [
                        // Only added when you kill him and directly completed
                        new PredicateTrigger(game, TriggerType.AlwaysComplete, MadmanKilled),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 6, // Clementine
                }
            ),
            #endregion
            #region Sandra's Daughter
            QuestFactory.CreateMainQuest(this, MainQuestType.SandrasDaughter,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SandrasDaughter_GoToBurnvilleAndFindSabine,
                    Triggers =
                    [
                        // Activate: Sandra asked to look for her daughter and gave her key
                        new GlobalVariableTrigger(game, TriggerType.Activation, 28),
                        // Complete: found Sabine's diary in Burnville or Sabine herself in the torture chamber
                        new OrTrigger<EventDisabledTrigger, EventDisabledTrigger>(game, TriggerType.Completion,
                            (g, t) => new EventDisabledTrigger(g, t, 286, 7),
                            (g, t) => new EventDisabledTrigger(g, t, 293, 2)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 5, // Sandra
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SandrasDaughter_RescueSabine,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: opened the cells with Luminor's key
                        new GlobalVariableTrigger(game, TriggerType.Completion, 27),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 300, // Sabine's diary
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SandrasDaughter_ReturnToSandra,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, 27),
                        // Complete: talked to Sandra after the rescue
                        new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_ToldSandraAboutRescue),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 5, // Sandra
                }
            ),
            #endregion
            #region Tower of Black Magic
            QuestFactory.CreateMainQuest(this, MainQuestType.TowerOfBlackMagic,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TowerOfBlackMagic_OpenTheTower,
                    Triggers =
                    [
                        // Activate: Nelvin told about the sphere of opening or joined with it
                        new OrTrigger<KeywordLearnedTrigger, GlobalVariableTrigger>(game, TriggerType.Activation,
                            (g, t) => new KeywordLearnedTrigger(g, t, 53), // "Sphere of opening"
                            (g, t) => new GlobalVariableTrigger(g, t, 80)), // Nelvin joined
                        // Complete: used the sphere of opening in the gate house
                        new EventDisabledTrigger(game, TriggerType.Completion, 292, 3),
                    ],
                    SourceType = QuestSourceType.PartyMember,
                    SourceIndex = 10, // Nelvin
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TowerOfBlackMagic_FreeThePeopleOfBurnville,
                    Final = true,
                    Triggers =
                    [
                        // Activate: found the prisoners in Luminor's torture chamber
                        new EventDisabledTrigger(game, TriggerType.Activation, 293, 2),
                        // Complete: opened the cells
                        new GlobalVariableTrigger(game, TriggerType.Completion, 32),
                    ],
                    SourceType = QuestSourceType.PartyMember,
                    SourceIndex = 11, // Sabine
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TowerOfBlackMagic_WeakenLuminor,
                    Triggers =
                    [
                        // Activate: learned about the flame of life or found icewater
                        // The button in Luminor's hall also disables the flame event (295:3) after his death.
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => g.GetTotalItemCount(316) != 0 || // Icewater
                            (IsEventBitSet(g, e, 295, 3) && g.GetTotalItemCount(315) == 0 && !IsGlobalVariableSet(g, e, 27))),
                        // Activate: icewater was poured already (persists when no icewater is left)
                        new CountTrigger(game, TriggerType.Activation, [99, 100, 101, 102]),
                        // Complete: poured icewater into the flame (99 to 102 = 1st to 4th icewater)
                        new CountTrigger(game, TriggerType.Completion, [99, 100, 101, 102]),
                    ],
                    MinAmount = 4,
                    SourceType = QuestSourceType.MapEvent,
                    SourceIndex = CreateMapEventSourceIndex(295, 7, 7), // Flame of life
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TowerOfBlackMagic_DefeatLuminor,
                    Final = true,
                    // Weakening Luminor is optional
                    PostCompletionAction = CompleteOtherActiveQuests(MainQuestType.TowerOfBlackMagic, SubQuestType.TowerOfBlackMagic_WeakenLuminor),
                    Triggers =
                    [
                        // Activate: Sabine told that Luminor holds the key to the cells
                        new EventDisabledTrigger(game, TriggerType.Activation, 293, 2),
                        // Complete: got Luminor's key from him or already opened the cells with it (27)
                        new OrTrigger<ItemObtainedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new ItemObtainedTrigger(g, t, 315), // Luminor's key
                            (g, t) => new GlobalVariableTrigger(g, t, 27)),
                    ],
                    SourceType = QuestSourceType.PartyMember,
                    SourceIndex = 11, // Sabine
                }
            ),
            #endregion
            #region Valdyn's Return
            QuestFactory.CreateMainQuest(this, MainQuestType.ValdynsReturn,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ValdynsReturn_FindAWayHome,
                    Triggers =
                    [
                        // Activate: Valdyn came through the light gate in Luminor's tower
                        new EventDisabledTrigger(game, TriggerType.Activation, 296, 9),
                        // Complete: got the yellow sphere from S'Krel (after Valdyn came through the light gate)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 218) && IsEventBitSet(g, e, 296, 9)),
                    ],
                    SourceType = QuestSourceType.PartyMember,
                    SourceIndex = 12, // Valdyn
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.ValdynsReturn_UseTheYellowSphere,
                    Final = true,
                    Triggers =
                    [
                        // Activate: got the yellow sphere and Valdyn came through the light gate
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 218) && IsEventBitSet(g, e, 296, 9)),
                        // Complete: entered the teleporter room with the sphere and Valdyn or used the sphere at the machine
                        new OrTrigger<EventDisabledTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new EventDisabledTrigger(g, t, 369, 11),
                            (g, t) => new GlobalVariableTrigger(g, t, GlobalVar_UsedYellowSphere)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 34, // S'Krel
                }
            ),
            #endregion
            #region Ambermoon Picture
            QuestFactory.CreateMainQuest(this, MainQuestType.AmbermoonPicture,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.AmbermoonPicture_DeliverThePicture,
                    Final = true,
                    Triggers =
                    [
                        // Activate: Dieter Rottermund gave the picture
                        new GlobalVariableTrigger(game, TriggerType.Activation, 97),
                        // Complete: Baron Karsten hung the picture in his hall. Global variable 184 is not
                        // used as a lever in the antique area toggles it.
                        new TileChangeTrigger(game, TriggerType.Completion, 427, 11, 14, 117),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 20, // Dieter Rottermund
                }
            ),
            #endregion
            #region Kire's Letter
            QuestFactory.CreateMainQuest(this, MainQuestType.KiresLetter,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.KiresLetter_FindDorina,
                    Triggers =
                    [
                        // Activate: Kire told about his wife Dorina and gave a letter for her
                        new GlobalVariableTrigger(game, TriggerType.Activation, 52),
                        // Complete: talked to Dorina in her cave (or she got the letter already)
                        new OrTrigger<GlobalVariableTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new GlobalVariableTrigger(g, t, 61),
                            (g, t) => new GlobalVariableTrigger(g, t, 45)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.KiresLetter_ShowTheLetterToKetnar,
                    Triggers =
                    [
                        // Activate: optional, Kire's son knows where Dorina lives
                        new GlobalVariableTrigger(game, TriggerType.Activation, 52),
                        // Complete: Ketnar saw the letter (66) or Dorina was found without him
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                            IsGlobalVariableSet(g, e, 66) || IsGlobalVariableSet(g, e, 61) || IsGlobalVariableSet(g, e, 45)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.KiresLetter_GiveTheLetterToDorina,
                    Triggers =
                    [
                        // Activate: found Dorina
                        new GlobalVariableTrigger(game, TriggerType.Activation, 61),
                        // Complete: Dorina got the letter
                        new GlobalVariableTrigger(game, TriggerType.Completion, 45),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 55, // Kire
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.KiresLetter_ReturnToKire,
                    Final = true,
                    Triggers =
                    [
                        // Activate: Dorina got the letter
                        new GlobalVariableTrigger(game, TriggerType.Activation, 45),
                        // Complete: Kire gave his reward (including the key of the test mine)
                        new GlobalVariableTrigger(game, TriggerType.Completion, 55),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 57, // Dorina
                }
            ),
            #endregion
            #region Ferrin's Daughter
            QuestFactory.CreateMainQuest(this, MainQuestType.FerrinsDaughter,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.FerrinsDaughter_AskFerrinAboutHisMourning,
                    Triggers =
                    [
                        // Activate: met Ferrin the smith
                        new GlobalVariableTrigger(game, TriggerType.Activation, 86),
                        // Complete: he told about his daughter and gave the key of his storage cellar
                        new GlobalVariableTrigger(game, TriggerType.Completion, 87),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 63, // Ferrin
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.FerrinsDaughter_SearchTheStorageCellar,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: opened the cellar door (it is locked again later) or found his daughter already
                        new OrTrigger<DoorUnlockedTrigger, GlobalVariableTrigger>(game, TriggerType.Completion,
                            (g, t) => new DoorUnlockedTrigger(g, t, 13),
                            (g, t) => new GlobalVariableTrigger(g, t, 94)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 63, // Ferrin
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.FerrinsDaughter_FindFerrinsDaughter,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: found her behind the tornak caves
                        new GlobalVariableTrigger(game, TriggerType.Completion, 94),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 63, // Ferrin
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.FerrinsDaughter_ReturnToFerrin,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the happy Ferrin gave his reward
                        new GlobalVariableTrigger(game, TriggerType.Completion, 95),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 63, // Ferrin
                }
            ),
            #endregion
            #region The Beast
            QuestFactory.CreateMainQuest(this, MainQuestType.TheBeast,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TheBeast_AskNorelAboutKay,
                    Triggers =
                    [
                        // Activate: Norel asked whether you met his apprentice Kay (keyword "Kay")
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 82),
                        // Complete: he told about the beast and gave the key of his weapons cupboard (169)
                        // or got Kay's amulet already
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 169) || IsGlobalVariableSet(g, e, 168)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 27, // Norel
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TheBeast_KillTheBeast,
                    Triggers =
                    [
                        // Activate: Norel told about the beast or it attacked in its cave
                        new OrTrigger<GlobalVariableTrigger, EventDisabledTrigger>(game, TriggerType.Activation,
                            (g, t) => new GlobalVariableTrigger(g, t, 169),
                            (g, t) => new EventDisabledTrigger(g, t, 415, 4)),
                        // Complete: the beast turned into Kay when it died (event 415:5) or its amulet is owned or delivered
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsEventBitSet(g, e, 415, 5) || g.GetTotalItemCount(348) != 0 || IsGlobalVariableSet(g, e, 168)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 27, // Norel
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TheBeast_BringTheAmuletToNorel,
                    Final = true,
                    Triggers =
                    [
                        // Activate: the beast is dead or Kay's amulet was found
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsEventBitSet(g, e, 415, 5) || g.GetTotalItemCount(348) != 0),
                        // Complete: Norel saw the amulet and gave the xenobil wood
                        new GlobalVariableTrigger(game, TriggerType.Completion, 168),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 27, // Norel
                }
            ),
            #endregion
            #region The Witch Master
            QuestFactory.CreateMainQuest(this, MainQuestType.WitchMaster,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WitchMaster_HearBralumsProblem,
                    Triggers =
                    [
                        // Activate: Bralum mentioned the problem of the gnomes (keyword "Problem")
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 80),
                        // Complete: he told about Nera (keyword "Nera") or got the ring already
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsKeywordLearned(g, e, 81) || IsGlobalVariableSet(g, e, 129)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 25, // Bralum
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WitchMaster_FindNerasLair,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: the riddlemouth in the cellar of the witch's house opened (answer "Nera")
                        // or Nera was defeated already
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsEventBitSet(g, e, 412, 2) || g.GetTotalItemCount(342) != 0 || IsGlobalVariableSet(g, e, 129)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 25, // Bralum
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WitchMaster_DefeatNera,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Nera's ring is owned or was shown to Bralum
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(342) != 0 || IsGlobalVariableSet(g, e, 129)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 25, // Bralum
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.WitchMaster_ShowTheRingToBralum,
                    Final = true,
                    Triggers =
                    [
                        // Activate: Nera's ring was found
                        new ItemObtainedTrigger(game, TriggerType.Activation, 342),
                        // Complete: Bralum saw the ring and gave his reward
                        new GlobalVariableTrigger(game, TriggerType.Completion, 129),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 25, // Bralum
                }
            ),
            #endregion
            #region Sansri's Curse
            QuestFactory.CreateMainQuest(this, MainQuestType.SansrisCurse,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SansrisCurse_AskPelanisAboutHisConcerns,
                    Triggers =
                    [
                        // Activate: Pelanis mentioned the curse when you left (keyword "Curse")
                        new KeywordLearnedTrigger(game, TriggerType.Activation, 69),
                        // Complete: he told about Sansri and the petrified eagles or Sansri was defeated already
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, GlobalVar_PelanisToldAboutSansri) ||
                            IsEventBitSet(g, e, 423, 5) || IsGlobalVariableSet(g, e, 180)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 28, // Pelanis
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SansrisCurse_DefeatSansri,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Sansri was defeated in her temple (event 423:5)
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsEventBitSet(g, e, 423, 5) || IsGlobalVariableSet(g, e, 180)),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 28, // Pelanis
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.SansrisCurse_GiveSansrisBloodToPelanis,
                    Final = true,
                    Triggers =
                    [
                        // Activate: Sansri was defeated or her blood was found
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsEventBitSet(g, e, 423, 5) || g.GetTotalItemCount(352) != 0),
                        // Complete: Pelanis lifted the curse and gave the key of the treasure chamber and the eagle flute
                        new GlobalVariableTrigger(game, TriggerType.Completion, 180),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 28, // Pelanis
                }
            ),
            #endregion
            #region Windgates
            QuestFactory.CreateMainQuest(this, MainQuestType.Windgates,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Windgates_EnterTheWindShrine,
                    Triggers =
                    [
                        // Activate: found the windgate book or the key to the shrine (library of Newlake)
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => g.GetTotalItemCount(339) != 0 || g.GetTotalItemCount(340) != 0),
                        // Complete: the door of the wind shrine was opened
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 23),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 339, // Windgate book
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Windgates_CreateTheConstructionStaff,
                    Triggers =
                    [
                        // Activate: entered the wind shrine
                        new DoorUnlockedTrigger(game, TriggerType.Activation, 23),
                        // Complete: the xenobil wood and a gem were melted on the altar. Global variable 127
                        // is reset when the staff is taken, so the staff itself or a repaired windgate is checked.
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => g.GetTotalItemCount(321) != 0 || WindgateRepaired(g, e)),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 339, // Windgate book
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Windgates_CreateTheWindChain,
                    Triggers =
                    [
                        // Activate: entered the wind shrine
                        new DoorUnlockedTrigger(game, TriggerType.Activation, 23),
                        // Complete: twelve windpearls and the pearl chain formed the windchain. Global variable 128
                        // is reset when the chain is taken. Advanced also sets global variable 229 but Theodorus
                        // sets it as well, so only the windchain itself is checked.
                        new ItemObtainedTrigger(game, TriggerType.Completion, 134),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 339, // Windgate book
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Windgates_RepairTheWindgates,
                    Final = true,
                    Triggers =
                    [
                        // Activate: own the construction staff or repaired a windgate
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => g.GetTotalItemCount(321) != 0 || WindgateRepaired(g, e)),
                        // Complete: all destroyed windgates were repaired with the construction staff
                        new CountTrigger(game, TriggerType.Completion, WindgateVariables),
                    ],
                    MinAmount = (uint)WindgateVariables.Length,
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 339, // Windgate book
                }
            ),
            #endregion
            #region Trip to Gadlon
            QuestFactory.CreateMainQuest(this, MainQuestType.TripToGadlon,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TripToGadlon_ActivateTheTeleporter,
                    Triggers =
                    [
                        // Activate: found the red carved sphere in the teleporter room (map 369)
                        new ItemObtainedTrigger(game, TriggerType.Activation, 365),
                        // Complete: the machine took the sphere (210). Advanced resets the variable when the
                        // teleporter is used, so meeting Aderin in Gadlon (170) also counts.
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) => IsGlobalVariableSet(g, e, 210) || IsGlobalVariableSet(g, e, 170)),
                    ],
                    SourceType = QuestSourceType.MapEvent,
                    SourceIndex = CreateMapEventSourceIndex(369, 13, 4), // Skeleton with the red sphere
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TripToGadlon_FindRinakles,
                    Triggers =
                    [
                        // Activate: the teleporter is active or the party is in Gadlon
                        new PredicateTrigger(game, TriggerType.Activation, (g, e) => IsGlobalVariableSet(g, e, 210) || IsGlobalVariableSet(g, e, 170)),
                        // Complete: talked to Rinakles, the head of the school of magic
                        new GlobalVariableTrigger(game, TriggerType.Completion, 176),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 68, // Aderin
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.TripToGadlon_AskRinaklesForTheWayHome,
                    Final = true,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete: Rinakles set his teleporter to Lyramion (177)
                        new GlobalVariableTrigger(game, TriggerType.Completion, 177),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 70, // Rinakles
                }
            ),
            #endregion
            #region Brotherhood of Tarbos
            QuestFactory.CreateMainQuest(this, MainQuestType.BrotherhoodOfTarbos,
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.BrotherhoodOfTarbos_BringTheAmberstarToTheTemple,
                    Triggers =
                    [
                        // Activate: found the Amberstar in castle Godsbane
                        new OrTrigger<ItemObtainedTrigger, DoorUnlockedTrigger>(game, TriggerType.Activation,
                            (g, t) => new ItemObtainedTrigger(g, t, 375), // Amberstar
                            (g, t) => new DoorUnlockedTrigger(g, t, 31)),
                        // Complete: the Amberstar opened the door to the coffin hall
                        new DoorUnlockedTrigger(game, TriggerType.Completion, 31),
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 375, // Amberstar
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.BrotherhoodOfTarbos_DefeatThornahuun,
                    Final = true,
                    Triggers =
                    [
                        // Activate: met Thornahuun in the coffin hall
                        new EventDisabledTrigger(game, TriggerType.Activation, 433, 3),
                        // Complete: Thornahuun was met and is hidden again. A defeated map monster is hidden without
                        // an event, so this is detected with the next event or item. He is hidden from the start,
                        // so the encounter must have happened.
                        new PredicateTrigger(game, TriggerType.Completion, (g, e) =>
                            g.CurrentSavegame!.GetEventBit(433, 2) && g.CurrentSavegame.GetCharacterBit(433, 0)),
                    ],
                    SourceType = QuestSourceType.MapEvent,
                    SourceIndex = CreateMapEventSourceIndex(433, 5, 6), // Coffin hall
                }
            ),
            #endregion
            #endregion
            .. (advanced ? CreateAdvancedQuests() : []),
        ];
    }
}
