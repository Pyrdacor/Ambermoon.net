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
    // TODO ...
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
    ThiefPlague_SilverHand, // get it after talking to baron
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
    Graveyard_GetBroochFromGravedigger, // after talking to Aman
    Graveyard_ReturnBroochToAman, // after getting the brooch
    // Monstereye
    Monstereye_BuyIt, // after talking to Sandire
    // Thief Guild
    ThiefGuild_Enter, // after returning the brooch to Aman
    // Golden Horseshoes
    GoldenHorseshoes_FindHorseshoes,
    GoldenHorseshoes_ReturnHorseshoes,
    // Orcs
    // Sandra's Daughter
    SandrasDaughter_GoToBurnvilleAndFindSabine, // after talking to sandra
    SandrasDaughter_RescueSabine, // after finding sabine's note
    // Brotherhood of Tarbos
    BrotherhoodOfTarbos_X, // after talking to otram
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

    public OrTrigger(Game game, TriggerType triggerType,
        Func<Game, TriggerType, TFirst> firstTriggerFactory,
        Func<Game, TriggerType, TSecond> secondTriggerFactory)
    {
        this.triggerType = triggerType;
        firstTrigger = firstTriggerFactory(game, triggerType);
        secondTrigger = secondTriggerFactory(game, triggerType);
    }

    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
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

    bool IQuestTrigger.CheckItem(Item item, uint itemCount, SubQuest subQuest)
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

file class GlobalVariableTrigger(Game game, TriggerType triggerType, uint index, bool expectedValue = true) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.ObjectIndex == index && e.Value == (expectedValue ? 1 : 0))
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame.GetGlobalVariable(index) == expectedValue)
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class TileChangeTrigger(Game game, TriggerType triggerType, uint mapIndex, uint x, uint y, uint expectedTile) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ChangeTileEvent e && e.MapIndex == mapIndex && e.X == x && e.Y == y && e.FrontTileIndex == expectedTile)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame.TileChangeEvents.TryGetValue(mapIndex, out var changes) &&
            changes.Any(c => c.X == x && c.Y == y && c.FrontTileIndex == expectedTile))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class EventDisabledTrigger(Game game, TriggerType triggerType, uint mapIndex, uint eventIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        uint index = (mapIndex - 1) * 64 + eventIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetEventBit && e.ObjectIndex == index && e.Value == 1)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame.GetEventBit(mapIndex, eventIndex - 1))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class CharacterVisibilityTrigger(Game game, TriggerType triggerType, uint mapIndex, uint characterIndex, bool visible) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        uint index = (mapIndex - 1) * 32 + characterIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetCharacterBit && e.ObjectIndex == index && e.Value == (visible ? 0 : 1))
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame.GetCharacterBit(mapIndex, characterIndex - 1) != visible) // if the bit is true, the character is invisible and vice versa
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class ItemObtainedTrigger(Game game, TriggerType triggerType, params uint[] itemIndices) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckItem(Item item, uint itemCount, SubQuest subQuest)
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

file class ExplorationTrigger(Game game, TriggerType triggerType, uint mapIndex, uint x, uint y) : IQuestTrigger
{
    private readonly Game game = game;
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
        else if (game.CurrentSavegame.Automaps.TryGetValue(this.mapIndex, out var exploration) && exploration.IsBlockExplored(game.MapManager.GetMap(this.mapIndex), this.x, this.y))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

// TODO: Careful with this, as you can also add keywords by typing them in conversation which might activate the quest.
// Always add a required quest when using this.
file class KeywordLearnedTrigger(Game game, TriggerType triggerType, uint keywordIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType.GetOldState();
    public QuestState NewState => triggerType.GetNewState();

    bool IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (OldState != QuestState.Any && NewState != QuestState.Completed && subQuest.State != OldState)
            return false;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.AddKeyword && e.ObjectIndex == keywordIndex && e.Value == 1)
        {
            subQuest.State = NewState;
            return true;
        }
        else if (game.CurrentSavegame.IsDictionaryWordKnown(keywordIndex))
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

                if (requiredQuests.All(q => q.State == QuestState.Completed))
                {
                    subQuest.State = NewState;
                    return true;
                }
            }
        }

        return false;
    }
}

file class ChestUnlockedTrigger(Game game, TriggerType triggerType, uint chestIndex) : IQuestTrigger
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
        else if (!game.CurrentSavegame.IsChestLocked(this.chestIndex - 1))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

file class DoorUnlockedTrigger(Game game, TriggerType triggerType, uint doorIndex) : IQuestTrigger
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
        else if (!game.CurrentSavegame.IsDoorLocked(this.doorIndex))
        {
            subQuest.State = NewState;
            return true;
        }

        return false;
    }
}

#endregion


partial class QuestLog
{
    private MainQuest[] CreateQuests(bool advanced, int episode)
    {
        static uint CreateMapEventSourceIndex(uint mapIndex, uint x, uint y)
        {
            uint sourceIndex = x & 0xff;
            sourceIndex <<= 8;
            sourceIndex |= y & 0xff;
            sourceIndex <<= 8;
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

        Action<SubQuest> AddAsRequiredTo(MainQuestType mainQuest, SubQuestType targetQuest)
        {
            return (subQuest) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.FirstOrDefault(q => q.Type == targetQuest).AddRequiredQuest(subQuest.Type);
        }

        Action<SubQuest> ActivateOtherQuest(MainQuestType mainQuest, SubQuestType otherQuest)
        {
            return (_) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.FirstOrDefault(q => q.Type == otherQuest).State = QuestState.Active;
        }

        Action<SubQuest> CompleteOtherQuest(MainQuestType mainQuest, SubQuestType otherQuest)
        {
            return (_) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.FirstOrDefault(q => q.Type == otherQuest).State = QuestState.Completed;
        }

        Action<SubQuest> CompleteOtherQuests(MainQuestType mainQuest, params SubQuestType[] otherQuests)
        {
            var lookup = new HashSet<SubQuestType>(otherQuests);
            return (_) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.Where(q => lookup.Contains(q.Type)).ToList().ForEach(q => q.State = QuestState.Completed);
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
                    PostActivationAction = subQuest => subQuest.State = QuestState.Blocked,
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
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 370), // Picked up the hangar key (needs defeat of S'Orel beforehand)
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
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 378), // Green navstone
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
                        new EventDisabledTrigger(game, TriggerType.Completion, 343, 16), // Long text triggered in Dor Kiredon
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
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 377, 379), // Blue or yellow navstone
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
                        // Completion
                        new GlobalVariableTrigger(game, TriggerType.Completion, GlobalVar_ShowedAmberToGrandfather), // Showed Amber to Grandfather
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
                    PostCompletionAction = subQuest =>
                    {
                        ActivateOtherQuest(MainQuestType.LyramionsFaith, SubQuestType.LyramionsFaith_EnterTheTempleOfBrotherhood)(subQuest);
                    },
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Completion
                        new ItemObtainedTrigger(game, TriggerType.Completion, 343), // Demon Sleep
                    ],
                    SourceType = QuestSourceType.Item,
                    SourceIndex = 345, // Recipe
                }
                // TODO ...
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
                        // No completion, SubQuestType.GoldenHorseshoes_FindHorseshoes will manually complete this
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
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 255), // swamp lilly
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
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 218, 254), // empty bottle, but also water of life will work
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
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 254), // water of life
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
                        // Activate
                        new ItemObtainedTrigger(game, TriggerType.Activation, 254, 255), // swamp lilly and water of life
                        // Complete
                        new NextSubQuestActivatedTrigger(),
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
                    PostActivationAction = CompleteOtherQuests(MainQuestType.Grandfather, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, SubQuestType.Grandfather_FindTolimar),
                    Triggers =
                    [
                        // Activate
                        new GlobalVariableTrigger(game, TriggerType.Activation, GlobalVar_TolimarQuestStarted),
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 270), // Golden horseshoe
                    ],
                    MinAmount = 4,
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 10, // Tolimar
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.GoldenHorseshoes_ReturnHorseshoes,
                    PostActivationAction = CompleteOtherQuests(MainQuestType.Grandfather, SubQuestType.Grandfather_TellGrandfatherAboutCaveIn, SubQuestType.Grandfather_FindTolimar),
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 255), // Returned golden horseshoes
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
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 288), // Alkem's ring
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.AlkemsRing_ReturnTheRing,
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
                        // Complete
                        new KeywordLearnedTrigger(game, TriggerType.Completion, 24), // Learned word "Fairies"
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
                        // Complete
                        new ItemObtainedTrigger(game, TriggerType.Completion, 283), // Weird stone
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
            #endregion
            #region Advanced quests
            #endregion
        ];
    }
}
