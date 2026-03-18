using System.Numerics;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.ExecutableData;

namespace Ambermoon.Data.Pyrdacor;

internal class DataNameProvider(string versionString, string infoString, Dictionary<MessageTextType, IReadOnlyDictionary<int, string>> texts) : IDataNameProvider
{
    private string GetUIText(UITextIndex index) => texts[MessageTextType.UI][(int)index];
    private string GetMessage(Messages.Index index) => texts[MessageTextType.Message][(int)index];

    public string On => GetUIText(UITextIndex.On);
    public string Off => GetUIText(UITextIndex.Off);
    public string DataVersionString => versionString;
    public string DataInfoString => infoString;
    public string CharacterInfoAgeString => GetUIText(UITextIndex.AgeDisplay);
    public string CharacterInfoAPRString => GetUIText(UITextIndex.APR);
    public string CharacterInfoExperiencePointsString => GetUIText(UITextIndex.EPDisplay);
    public string CharacterInfoGoldAndFoodString => GetUIText(UITextIndex.GoldAndFoodDisplay);
    public string CharacterInfoHitPointsString => GetUIText(UITextIndex.LPDisplay);
    public string CharacterInfoSpellPointsString => GetUIText(UITextIndex.SPDisplay);
    public string CharacterInfoSpellLearningPointsString => GetUIText(UITextIndex.SLPDisplay);
    public string CharacterInfoTrainingPointsString => GetUIText(UITextIndex.TPDisplay);
    public string CharacterInfoWeightHeaderString => GetUIText(UITextIndex.Weight);
    public string CharacterInfoWeightString => GetUIText(UITextIndex.WeightKilogramDisplay);
    public string CharacterInfoDamageString => GetUIText(UITextIndex.LabeledValueDisplay);
    public string CharacterInfoDefenseString => GetUIText(UITextIndex.LabeledValueDisplay);
    public string GetConditionName(Condition condition)
    {
        if (condition == Condition.None)
            return "";

        // As our values are always of form 2^n this will exactly return n.
        int index = BitOperations.TrailingZeroCount((ushort)condition);

        return texts[MessageTextType.Condition][index];
    }
    public string GetClassName(Class @class) => texts[MessageTextType.Class][(int)@class];
    public string? GetGenderName(Gender gender) => gender switch
    {
        Gender.Male => GetUIText(UITextIndex.Male),
        Gender.Female => GetUIText(UITextIndex.Female),
        _ => null
    };
    public string? GetGenderName(GenderFlag gender) => gender switch
    {
        GenderFlag.Male => GetUIText(UITextIndex.Male),
        GenderFlag.Female => GetUIText(UITextIndex.Female),
        GenderFlag.Both => GetUIText(UITextIndex.BothSexes),
        _ => null
    };
    public string GetLanguageName(Language language)
    {
        if (language == Language.None)
            return "";

        // As our values are always of form 2^n this will exactly return n.
        int index = BitOperations.TrailingZeroCount((byte)language);

        return texts[MessageTextType.Language][index];
    }
    public string GetRaceName(Race race) => texts[MessageTextType.Race][(int)race];
    public string GetSpellName(Spell spell) => spell == Spell.None ? "" : texts[MessageTextType.Spell][(int)spell - 1];
    public string GetWorldName(World world) => texts[MessageTextType.World][(int)world];
    public string GetItemTypeName(ItemType itemType) => texts[MessageTextType.ItemType][(int)itemType];
    public string GetSongName(Song song)
        => song switch
        {
            Song.Default => "",
            Song.Intro => "Intro",
            Song.Outro => "Extro",
            Song.Menu => "MainMenu",
            _ => texts[MessageTextType.Song][(int)song - 1]
        };
    public string GetElementName(CharacterElement element)
    {
        Messages.Index index = Messages.Index.ElementNone;

        if (element != CharacterElement.None)
        {
            if (((int)element & ((int)element - 1)) == 0) // single bit set
            {
                int elementIndex = Enum.GetNames<CharacterElement>().ToList().IndexOf(element.ToString());
                index = Messages.Index.ElementNone + elementIndex;
            }
            else
            {
                index = Messages.Index.ElementMultiple;
            }
        }

        return GetMessage(index);
    }
    public string GetElementName(ItemElement element)
    {
        return GetMessage(Messages.Index.ElementNone + (int)element);
    }
    public string GetExtendedLanguageName(ExtendedLanguage language)
    {
        if (language == ExtendedLanguage.None)
            return "";

        // As our values are always of form 2^n this will exactly return n.
        int index = BitOperations.TrailingZeroCount((byte)language);

        return GetMessage(Messages.Index.ExtendedLanguage1 + index);
    }
    public string InventoryTitleString => GetUIText(UITextIndex.Inventory);
    public string AttributesHeaderString => GetUIText(UITextIndex.Attributes);
    public string SkillsHeaderString => GetUIText(UITextIndex.Skills);
    public string LanguagesHeaderString => GetUIText(UITextIndex.Languages);
    public string ConditionsHeaderString => GetUIText(UITextIndex.Conditions);
    public string DataHeaderString => GetUIText(UITextIndex.DataHeader);
    public string GetAttributeShortName(Attribute attribute) => texts[MessageTextType.AttributeShortcut][(int)attribute];
    public string GetSkillShortName(Skill skill) => texts[MessageTextType.SkillShortcut][(int)skill];
    public string GetAttributeName(Attribute attribute) => texts[MessageTextType.Attribute][(int)attribute];
    public string GetSkillName(Skill skill) => texts[MessageTextType.Skill][(int)skill];
    public string OptionsHeader => GetMessage(Messages.Index.Options);
    public string ClassesHeaderString => GetUIText(UITextIndex.ClassHeader);
    public string GenderHeaderString => GetUIText(UITextIndex.Sex);
    public string ChooseCharacter => GetUIText(UITextIndex.ChooseCharacter);
    public string ConfirmCharacter => GetMessage(Messages.Index.HappyWithCharacter);
    public string LoadWhichSavegame => GetMessage(Messages.Index.LoadWhichSavegame);
    public string SaveWhichSavegame => GetMessage(Messages.Index.SaveWhichSavegame);
    public string ReallyLoad => GetMessage(Messages.Index.ReallyLoad);
    public string ReallyOverwriteSave => GetMessage(Messages.Index.ReallyOverwriteSave);
    public string WrongRiddlemouthSolutionText => GetMessage(Messages.Index.IsNotTheRightAnswer);
    public string NotAllowingToLookIntoBackpack => GetMessage(Messages.Index.NotAllowingToLookIntoBackpack);
    /// <summary>
    /// This is used if the entered word is not part of the dictionary.
    /// </summary>
    public string That => GetMessage(Messages.Index.That);
    public string DropItemQuestion => GetMessage(Messages.Index.ReallyDropIt);
    public string DropGoldQuestion => GetMessage(Messages.Index.ReallyDropGold);
    public string DropFoodQuestion => GetMessage(Messages.Index.ReallyDropFood);
    public string NotEnoughSP => GetMessage(Messages.Index.NotEnoughSP);
    public string WrongArea => GetMessage(Messages.Index.WrongArea);
    public string WrongWorld => GetMessage(Messages.Index.WrongWorld);
    public string WrongClassToUseItem => GetMessage(Messages.Index.WrongClassToUseItem);
    public string WrongPlaceToUseItem => GetMessage(Messages.Index.WrongPlaceToUseItem);
    public string WrongWorldToUseItem => GetMessage(Messages.Index.WrongWorldToUseItem);
    public string WrongClassToEquipItem => GetMessage(Messages.Index.WrongClassToEquip);
    public string WrongSexToEquipItem => GetMessage(Messages.Index.WrongSexToEquip);
    public string NotEnoughFreeFingers => GetMessage(Messages.Index.NotEnoughFreeFingers);
    public string NotEnoughFreeHands => GetMessage(Messages.Index.NotEnoughFreeHands);
    public string CannotEquip => GetMessage(Messages.Index.ThisCannotBeEquipped);
    public string CannotEquipInFight => GetMessage(Messages.Index.CannotEquipInCombat);
    public string CannotUnequipInFight => GetMessage(Messages.Index.CannotUnequipInCombat);
    public string ItemHasNoEffectHere => GetMessage(Messages.Index.ItemCannotBeUsedHere);
    public string ItemCannotBeUsedHere => GetMessage(Messages.Index.CannotUseItHere);
    public string CannotUseBrokenItems => GetMessage(Messages.Index.CannotUseBrokenItems);
    public string WhichItemToUseMessage => GetMessage(Messages.Index.WhichItemToUse);
    public string WhichItemToExamineMessage => GetMessage(Messages.Index.WhichItemToExamine);
    public string WhichItemToDropMessage => GetMessage(Messages.Index.WhichItemToDrop);
    public string WhichItemToStoreMessage => GetMessage(Messages.Index.WhichItemToPutInChest);
    public string GoldName => GetUIText(UITextIndex.Gold);
    public string FoodName => GetUIText(UITextIndex.Food);
    public string DropHowMuchItemsMessage => GetMessage(Messages.Index.DropHowMany);
    public string DropHowMuchGoldMessage => GetMessage(Messages.Index.DropHowMuchGold);
    public string DropHowMuchFoodMessage => GetMessage(Messages.Index.DropHowMuchFood);
    public string StoreHowMuchItemsMessage => GetMessage(Messages.Index.StoreHowMany);
    public string StoreHowMuchGoldMessage => GetMessage(Messages.Index.StoreHowMuchGold);
    public string StoreHowMuchFoodMessage => GetMessage(Messages.Index.StoreHowMuchFood);
    public string GiveHowMuchGoldMessage => GetMessage(Messages.Index.GiveHowMuchGold);
    public string GiveHowMuchFoodMessage => GetMessage(Messages.Index.GiveHowMuchFood);
    public string GiveToWhom => GetMessage(Messages.Index.WhomGiveItTo);
    public string WhereToMoveIt => GetMessage(Messages.Index.WhereToMoveItTo);
    public string TakeHowManyMessage => GetMessage(Messages.Index.TakeHowMany);
    public string PersonAsleepMessage => GetMessage(Messages.Index.ThisPersonIsAsleep);
    public string WantToFightMessage => GetMessage(Messages.Index.AttackWantToFight);
    public string CompassDirections => GetUIText(UITextIndex.CardinalDirections);
    public string AttackEscapeFailedMessage => GetMessage(Messages.Index.CouldNotEscape);
    public string SelectNewLeaderMessage => GetMessage(Messages.Index.SelectNewLeader);
    public string He => GetUIText(UITextIndex.He);
    public string She => GetUIText(UITextIndex.She);
    public string His => GetUIText(UITextIndex.His);
    public string Her => GetUIText(UITextIndex.Her);
    public string DontForgetItems => GetMessage(Messages.Index.DontForgetItems);
    public string LeaveConversationWithoutItems => GetMessage(Messages.Index.DontWantToTakeItemsWithYou);
    public string LootAfterBattle => GetMessage(Messages.Index.LootAfterBattle);
    public string ReceiveExp => GetMessage(Messages.Index.ReceiveExp);
    public string ChooseBattlePositions => GetMessage(Messages.Index.EnterBattlePositions);
    public string WaitHowManyHours => GetMessage(Messages.Index.WaitHowManyHours);
    public string CannotWaitBecauseOfNearbyMonsters => GetMessage(Messages.Index.WaitingIsTooDangerous);
    public string ItemWeightDisplay => GetUIText(UITextIndex.WeightGramDisplay);
    public string ItemHandsDisplay => GetUIText(UITextIndex.HandsDisplay);
    public string ItemFingersDisplay => GetUIText(UITextIndex.FingersDisplay);
    public string ItemDamageDisplay => GetUIText(UITextIndex.DamageDisplay);
    public string ItemDefenseDisplay => GetUIText(UITextIndex.DefenseDisplay);
    public string ReviveMessage => GetMessage(Messages.Index.ReviveMessage);
    public string DustChangedToAshes => GetMessage(Messages.Index.DustChangedToAshes);
    public string AshesChangedToBody => GetMessage(Messages.Index.AshesChangedToBody);
    public string BodyBurnsUp => GetMessage(Messages.Index.BodyBurnsUp);
    public string AshesFallToDust => GetMessage(Messages.Index.AshesFallToDust);
    public string IsNotDead => GetMessage(Messages.Index.IsNotDead);
    public string IsNotAsh => GetMessage(Messages.Index.IsNotAsh);
    public string IsNotDust => GetMessage(Messages.Index.IsNotDust);
    public string CannotBeResurrected => GetMessage(Messages.Index.CannotBeResurrected);
    public string YouDontKnowAnySpellsYet => GetMessage(Messages.Index.YouDontKnowAnySpellsYet);
    public string CantLearnSpellsOfType => GetMessage(Messages.Index.CantLearnSpellsOfType);
    public string AlreadyKnowsSpell => GetMessage(Messages.Index.AlreadyKnowsSpell);
    public string FailedToLearnSpell => GetMessage(Messages.Index.FailedToLearnSpell);
    public string SpellFailed => GetMessage(Messages.Index.SpellFailed);
    public string ThatsNotASpellScroll => GetMessage(Messages.Index.ThatsNotASpellScroll);
    public string NotEnoughSpellLearningPoints => GetMessage(Messages.Index.NotEnoughSpellLearningPoints);
    public string ManagedToLearnSpell => GetMessage(Messages.Index.ManagedToLearnSpell);
    public string TheSpellFailed => GetMessage(Messages.Index.TheSpellFailed);
    public string UseSpellOnlyInCitiesOrDungeons => GetMessage(Messages.Index.UseSpellOnlyInCitiesOrDungeons);
    public string WhichScrollToRead => GetMessage(Messages.Index.WhichScrollToRead);
    public string RestingTooDangerous => GetMessage(Messages.Index.RestingTooDangerous);
    public string ItemIsNotBroken => GetMessage(Messages.Index.ItemIsNotBroken);
    public string ItemIsBroken => GetMessage(Messages.Index.ItemIsBroken);
    public string NoCursedItemFound => GetMessage(Messages.Index.NoCursedItemFound);
    public string ItemIsCursed => GetMessage(Messages.Index.ThisItemIsCursed);
    public string ThisIsNotAMagicalItem => GetMessage(Messages.Index.ThisIsNotAMagicalItem);
    public string ItemAlreadyFullyCharged => GetMessage(Messages.Index.ItemAlreadyFullyCharged);
    public string ItemAlreadyIdentified => GetMessage(Messages.Index.ItemAlreadyIdentified);
    public string CannotBeDuplicated => GetMessage(Messages.Index.CannotBeDuplicated);
    public string NoRoomForItem => GetMessage(Messages.Index.NoRoomForItem);
    public string MaxLPDisplay => GetUIText(UITextIndex.MaxLPDisplay);
    public string MaxSPDisplay => GetUIText(UITextIndex.MaxSPDisplay);
    public string MBWDisplay => GetUIText(UITextIndex.MBWDisplay);
    public string MBRDisplay => GetUIText(UITextIndex.MBRDisplay);
    public string AttributeHeader => GetUIText(UITextIndex.Attribute);
    public string SkillHeader => GetUIText(UITextIndex.Skill);
    public string FunctionHeader => texts[MessageTextType.SpellType][(int)SpellSchool.Function];
    public string Cursed => GetUIText(UITextIndex.Cursed);
    public string TiredMessage => GetMessage(Messages.Index.GettingTired);
    public string ExhaustedMessage => GetMessage(Messages.Index.CompletelyExhausted);
    public string SleepUntilDawn => GetMessage(Messages.Index.SleepUntilDawn);
    public string Sleep8Hours => GetMessage(Messages.Index.PartyRestsFor8Hours);
    public string RestingWouldHaveNoEffect => GetMessage(Messages.Index.RestingWouldHaveNoEffect);
    public string HasNoMoreFood => GetMessage(Messages.Index.HasNoMoreFood);
    public string RecoveredLP => GetMessage(Messages.Index.RegainsLP);
    public string RecoveredLPAndSP => GetMessage(Messages.Index.RegainsLPAndSP);
    public string HasAged => GetMessage(Messages.Index.HasAged);
    public string HasDiedOfAge => GetMessage(Messages.Index.HasDiedOfAge);
    public string LockpickBreaks => GetMessage(Messages.Index.LockpickBreaks);
    public string UnableToPickTheLock => GetMessage(Messages.Index.UnableToPickTheLock);
    public string UnlockedChestWithLockpick => GetMessage(Messages.Index.UnlockedChestWithLockpick);
    public string UnlockedDoorWithLockpick => GetMessage(Messages.Index.UnlockedDoorWithLockpick);
    public string YouNoticeATrap => GetMessage(Messages.Index.YouNoticeATrap);
    public string DisarmTrap => GetMessage(Messages.Index.DisarmTrap);
    public string FindTrap => GetMessage(Messages.Index.DiscoverTrap);
    public string DoesNotFindTrap => GetMessage(Messages.Index.DoesNotDiscoverTraps);
    public string UnableToDisarmTrap => GetMessage(Messages.Index.HearStrangeSound);
    public string EscapedTheTrap => GetMessage(Messages.Index.EscapedTheTrap);
    public string WhichItemToOpenChest => GetMessage(Messages.Index.WhichItemToOpenChest);
    public string WhichItemToOpenDoor => GetMessage(Messages.Index.WhichItemToOpenDoor);
    public string HasOpenedChest => GetMessage(Messages.Index.HasOpenedChest);
    public string HasOpenedDoor => GetMessage(Messages.Index.HasOpenedDoor);
    public string ThisItemDoesNotOpenChest => GetMessage(Messages.Index.ThisItemDoesNotOpenChest);
    public string ThisItemDoesNotOpenDoor => GetMessage(Messages.Index.ThisItemDoesNotOpenDoor);
    public string HasReachedLevel => GetMessage(Messages.Index.HasReachedLevel);
    public string MaxLevelReached => GetMessage(Messages.Index.MaxLevelReached);
    public string NextLevelAt => GetMessage(Messages.Index.NextLevelAt);
    public string LPAreNow => GetMessage(Messages.Index.LPAreNow);
    public string SPAreNow => GetMessage(Messages.Index.SPAreNow);
    public string SLPAreNow => GetMessage(Messages.Index.SLPAreNow);
    public string TPAreNow => GetMessage(Messages.Index.TPAreNow);
    public string APRAreNow => GetMessage(Messages.Index.APRAreNow);
    public string EP => GetUIText(UITextIndex.EP);
    public string SpecialItemActivated => GetMessage(Messages.Index.ThisItemFulfillsSpecialPurpose);
    public string SpecialItemAlreadyInUse => GetMessage(Messages.Index.SameItemAlreadyInUse);
    public string NoChargesLeft => GetMessage(Messages.Index.NoChargesLeft);
    public string CannotCallEagleIfNotOnFoot => GetMessage(Messages.Index.CannotCallEagleIfNotOnFoot);
    public string BlowsTheFlute => GetMessage(Messages.Index.BlowsTheFlute);
    public string MountTheWasp => GetMessage(Messages.Index.MountTheWasp);
    public string CannotUseItHere => GetMessage(Messages.Index.CannotUseItHere);
    public string CannotUseMagicDiscHere => GetMessage(Messages.Index.CannotUseMagicDiscHere);
    public string CannotJumpThroughWalls => GetMessage(Messages.Index.CannotJumpThroughWalls);
    public string MarksPosition => GetMessage(Messages.Index.MarksPosition);
    public string HasntMarkedAPosition => GetMessage(Messages.Index.HasntMarkedAPosition);
    public string ReturnToMarkedPosition => GetMessage(Messages.Index.ReturnToMarkedPosition);
    public string SeeRoundDiskInFloor => GetMessage(Messages.Index.SeeRoundDiskInFloor);
    public string CannotClimbHere => GetMessage(Messages.Index.CannotClimbHere);
    public string YouLevitate => GetMessage(Messages.Index.YouLevitate);
    public string WhichNumber => GetMessage(Messages.Index.WhichNumber);
    public string AutomapperNotWorkingHere => GetMessage(Messages.Index.AutomapperNotWorkingHere);
    public string GameOverLoadOrQuit => GetMessage(Messages.Index.GameOverLoadOrQuit);
    public string GameOverMessage => GetMessage(Messages.Index.GameOver);
    public string ReallyQuit => GetMessage(Messages.Index.ReallyQuit);
    public string ItemIsImportant => GetMessage(Messages.Index.YouNeedThisItem);
    public string ChestFull => GetMessage(Messages.Index.ChestFull);
    public string ChestNowFull => GetMessage(Messages.Index.ChestNowFull);
    public string NoOneCanCarryThatMuch => GetMessage(Messages.Index.NoOneCanCarryThatMuch);
    public string CannotCarryAllGold => GetMessage(Messages.Index.CannotCarryAllGold);
    public string MapViewNotWorkingHere => GetMessage(Messages.Index.MapViewNotWorkingHere);
    public string TurnOnTuneInAndDropOut => GetMessage(Messages.Index.TurnOnTuneInAndDropOut);
    public string TextBlockMissing => GetMessage(Messages.Index.TextBlockMissing);
    public string ReviveCatMessage => GetMessage(Messages.Index.ReviveCat);
    public string CannotExchangeExpWithAnimals => GetMessage(Messages.Index.CannotExchangeExpWithAnimals);
    public string CannotExchangeExpWithDead => GetMessage(Messages.Index.CannotExchangeExpWithDead);
    public string ThisCantBeMoved => GetMessage(Messages.Index.ThisCantBeMoved);
    public string ElementLabel => GetMessage(Messages.Index.ElementLabel);


    #region Conversations

    public string DontKnowAnythingSpecialAboutIt => GetMessage(Messages.Index.DontKnowAnythingSpecialAboutIt);
    public string DenyJoiningParty => GetMessage(Messages.Index.DenyJoiningParty);
    public string PartyFull => GetMessage(Messages.Index.PartyFull);
    public string DenyLeavingPartyOnMoon => GetMessage(Messages.Index.DenyLeavingPartyOnMoon);
    public string CannotSendDeadPeopleAway => GetMessage(Messages.Index.CannotSendDeadPeopleAway);
    public string CrazyPeopleDontFollowCommands => GetMessage(Messages.Index.CrazyPeopleDontFollowCommands);
    public string PetrifiedPeopleCantGoHome => GetMessage(Messages.Index.PetrifiedPeopleCantGoHome);
    public string YouDontSpeakSameLanguage => GetMessage(Messages.Index.YouDontSpeakSameLanguage);
    public string WhoToTalkTo => GetMessage(Messages.Index.WhoToTalkTo);
    public string SelfTalkingIsMad => GetMessage(Messages.Index.SelfTalkingIsMad);
    public string UnableToTalk => GetMessage(Messages.Index.YouCantConversate);
    public string WhichItemToGive => GetMessage(Messages.Index.WhichItemToGive);
    public string WhichItemToShow => GetMessage(Messages.Index.WhichItemToShow);
    public string GiveHowMuchGoldToNPC => GetMessage(Messages.Index.GiveHowMuchGoldToNPC);
    public string GiveHowMuchFoodToNPC => GetMessage(Messages.Index.GiveHowMuchFoodToNPC);
    public string NotInterestedInItem => GetMessage(Messages.Index.NotInterestedInItem);
    public string NotInterestedInGold => GetMessage(Messages.Index.NotInterestedInGold);
    public string NotInterestedInFood => GetMessage(Messages.Index.NotInterestedInFood);
    public string MoreGoldNeeded => GetMessage(Messages.Index.HowAboutSomeMore);
    public string MoreFoodNeeded => GetMessage(Messages.Index.MoreFoodWouldBeGood);
    public string Hello => GetMessage(Messages.Index.Hello);
    public string GoodBye => GetMessage(Messages.Index.GoodBye);
    public string WellIShouldLeave => GetMessage(Messages.Index.WellShouldLeave);

    #endregion


    #region Automap

    public string LegendHeader => GetUIText(UITextIndex.Legend);
    public string GetAutomapName(AutomapType automapType) => texts[MessageTextType.Automap][(int)automapType];
    public string Location => GetUIText(UITextIndex.Location);
    public string AlreadyAtGotoPoint => GetMessage(Messages.Index.AlreadyAtGotoPoint);
    public string GotoPointSaved => GetMessage(Messages.Index.GotoPointSaved);
    public string WayBackTooDangerous => GetMessage(Messages.Index.WayBackTooDangerous);
    public string ReallyWantToGoThere => GetMessage(Messages.Index.ReallyWantToGoThere);
    public string DarkDontFindWayBack => GetMessage(Messages.Index.DarkDontFindWayBack);

    #endregion


    #region Battle messages

    public string BattleMessageAttacksWith => GetMessage(Messages.Index.AttacksWith);
    public string BattleMessageAttacks => GetMessage(Messages.Index.Attacks);
    public string BattleMessageWasBroken => GetMessage(Messages.Index.WasBroken);
    public string BattleMessageDidPointsOfDamage => GetMessage(Messages.Index.DidPointsOfDamage);
    public string BattleMessageCastsSpell => GetMessage(Messages.Index.CastsSpell);
    public string BattleMessageCastsSpellFrom => GetMessage(Messages.Index.CastsSpellFrom);
    public string BattleMessageWhoToBlink => GetMessage(Messages.Index.WhichMemberShouldBeBlinked);
    public string BattleMessageFlees => GetMessage(Messages.Index.Flees);
    public string BattleMessageWhereToMoveTo => GetMessage(Messages.Index.WhereToMoveTo);
    public string BattleMessageNowhereToMoveTo => GetMessage(Messages.Index.NowhereToMoveTo);
    public string BattleMessageNoAmmunition => GetMessage(Messages.Index.NoAmmunition);
    public string BattleMessageWhatToAttack => GetMessage(Messages.Index.WhatToAttack);
    public string BattleMessageCannotReachAnyone => GetMessage(Messages.Index.CannotReachAnyone);
    public string BattleMessageMissedTheTarget => GetMessage(Messages.Index.MissedTheTarget);
    public string BattleMessageCannotPenetrateMagicalAura => GetMessage(Messages.Index.CannotPenetrateMagicalAura);
    public string BattleMessageAttackFailed => GetMessage(Messages.Index.AttackFailed);
    public string BattleMessageImmuneToAttack => GetMessage(Messages.Index.ImmuneToAttack);
    public string BattleMessageAttackWasParried => GetMessage(Messages.Index.AttackWasDeflected);
    public string BattleMessageAttackDidNoDamage => GetMessage(Messages.Index.AttackDidNoDamage);
    public string BattleMessageMadeCriticalHit => GetMessage(Messages.Index.MadeCriticalHit);
    public string BattleMessageUsedLastAmmunition => GetMessage(Messages.Index.UsedLastAmmunition);
    public string BattleMessageCannotMove => GetMessage(Messages.Index.CannotMove);
    public string BattleMessageTooFarAway => GetMessage(Messages.Index.TooFarAway);
    public string BattleMessageUnableToAttack => GetMessage(Messages.Index.UnableToAttack);
    public string BattleMessageSomeoneAlreadyGoingThere => GetMessage(Messages.Index.SomeoneAlreadyGoingThere);
    public string BattleMessageMonstersAdvance => GetMessage(Messages.Index.MonstersAdvance);
    public string BattleMessageMoves => GetMessage(Messages.Index.Moves);
    public string BattleMessageWayWasBlocked => GetMessage(Messages.Index.WayWasBlocked);
    public string BattleMessageHasDroppedWeapon => GetMessage(Messages.Index.HasDroppedWeapon);
    public string BattleMessageRetreats => GetMessage(Messages.Index.Retreats);
    public string BattleMessagePartyAdvances => GetMessage(Messages.Index.PartyAdvances);
    public string BattleMessageWhichPartyMemberAsTarget => GetMessage(Messages.Index.WhichPartyMemberAsTarget);
    public string BattleMessageWhichMonsterAsTarget => GetMessage(Messages.Index.WhichMonsterAsTarget);
    public string BattleMessageWhichPartyMemberRowAsTarget => GetMessage(Messages.Index.WhichPartyMemberRowAsTarget);
    public string BattleMessageWhichMonsterRowAsTarget => GetMessage(Messages.Index.WhichMonsterRowAsTarget);
    public string BattleMessageDeflectedSpell => GetMessage(Messages.Index.DeflectedSpell);
    public string BattleMessageImmuneToSpellType => GetMessage(Messages.Index.ImmuneToSpellType);
    public string BattleMessageCannotDamagePetrifiedMonsters => GetMessage(Messages.Index.CannotDamagePetrifiedMonsters);
    public string BattleMessageImmuneToSpell => GetMessage(Messages.Index.ImmuneToSpell);
    public string BattleMessageWhereToBlinkTo => GetMessage(Messages.Index.WhereToBlinkTo);
    public string BattleMessageHasBlinked => GetMessage(Messages.Index.HasBlinked);
    public string BattleMessageCannotBlink => GetMessage(Messages.Index.CannotBlink);
    public string BattleMessageCannotParry => GetMessage(Messages.Index.CannotParry);
    public string BattleMessageUseItOnWhom => GetMessage(Messages.Index.UseItOnWhom);

    #endregion


    #region Places

    public string WelcomeAttackTrainer => GetMessage(Messages.Index.WelcomeAttackTrainer);
    public string WelcomeBlacksmith => GetMessage(Messages.Index.WelcomeBlacksmith);
    public string WelcomeCriticalHitTrainer => GetMessage(Messages.Index.WelcomeCriticalHitTrainer);
    public string WelcomeDisarmTrapTrainer => GetMessage(Messages.Index.WelcomeDisarmTrapTrainer);
    public string WelcomeFindTrapTrainer => GetMessage(Messages.Index.WelcomeFindTrapTrainer);
    public string WelcomeFoodDealer => GetMessage(Messages.Index.WelcomeFoodDealer);
    public string WelcomeHealer => GetMessage(Messages.Index.WelcomeHealer);
    public string WelcomeHorseSeller => GetMessage(Messages.Index.WelcomeHorseSeller);
    public string WelcomeInnkeeper => GetMessage(Messages.Index.WelcomeInnkeeper);
    public string WelcomeLockPickingTrainer => GetMessage(Messages.Index.WelcomeLockPickingTrainer);
    public string WelcomeMagician => GetMessage(Messages.Index.WelcomeMagician);
    public string WelcomeMerchant => GetMessage(Messages.Index.WelcomeMerchant);
    public string WelcomeParryTrainer => GetMessage(Messages.Index.WelcomeParryTrainer);
    public string WelcomeRaftSeller => GetMessage(Messages.Index.WelcomeRaftSeller);
    public string WelcomeReadMagicTrainer => GetMessage(Messages.Index.WelcomeReadMagicTrainer);
    public string WelcomeEnchanter => GetMessage(Messages.Index.WelcomeRecharger);
    public string WelcomeSage => GetMessage(Messages.Index.WelcomeSage);
    public string WelcomeSearchTrainer => GetMessage(Messages.Index.WelcomeSearchTrainer);
    public string WelcomeShipSeller => GetMessage(Messages.Index.WelcomeShipSeller);
    public string WelcomeSwimTrainer => GetMessage(Messages.Index.WelcomeSwimTrainer);
    public string WelcomeUseMagicTrainer => GetMessage(Messages.Index.WelcomeUseMagicTrainer);
    public string BuyWhichItem => GetMessage(Messages.Index.WhichItemToBuy);
    public string SellWhichItem => GetMessage(Messages.Index.WhichItemToSell);
    public string ExamineWhichItem => GetMessage(Messages.Index.WhichItemToExamine);
    public string ExamineWhichItemMerchant => GetMessage(Messages.Index.WhichMerchantItemToExamine);
    public string ExamineWhichItemSage => GetMessage(Messages.Index.WhichItemToExamineSage);
    public string ThisWillCost => GetMessage(Messages.Index.ThisWillCost);
    public string ForThisIllGiveYou => GetMessage(Messages.Index.ForThisIllGiveYou);
    public string AgreeOnPrice => GetMessage(Messages.Index.AgreeOnPrice);
    public string OneFoodCosts => GetMessage(Messages.Index.AgreeOnFoodPrice);
    public string MerchantFull => GetMessage(Messages.Index.CannotBuyAnymore);
    public string BuyHowMuchItems => GetMessage(Messages.Index.BuyHowMany);
    public string SellHowMuchItems => GetMessage(Messages.Index.SellHowMany);
    public string NotEnoughMoneyToBuy => GetMessage(Messages.Index.NotEnoughMoney);
    public string NotInterestedInItemMerchant => GetMessage(Messages.Index.NotInterestedInTrinket);
    public string WontBuyBrokenStuff => GetMessage(Messages.Index.WontBuyBrokenStuff);
    public string WantToGoWithoutItemsMerchant => GetMessage(Messages.Index.LeaveBoughtGoods);
    public string TrainHowOften => GetMessage(Messages.Index.TrainHowOften);
    public string PriceForTraining => GetMessage(Messages.Index.PriceForTraining);
    public string IncreasedAfterTraining => GetMessage(Messages.Index.IncreasedAfterTraining);
    public string NotEnoughTrainingPoints => GetMessage(Messages.Index.NotEnoughTrainingPoints);
    public string NotEnoughMoney => GetMessage(Messages.Index.NotEnoughMoney);
    public string BuyHowMuchFood => GetMessage(Messages.Index.BuyHowMuchFood);
    public string FoodDividedEqually => GetMessage(Messages.Index.FoodDividedEqually);
    public string FoodLeftAfterDividing => GetMessage(Messages.Index.FoodLeftAfterDividing);
    public string WantToLeaveRestOfFood => GetMessage(Messages.Index.WantToLeaveRestOfFood);
    public string PriceOfFood => GetMessage(Messages.Index.PriceOfFood);
    public string PriceForHealing => GetMessage(Messages.Index.PriceForHealing);
    public string PriceForHealingCondition => GetMessage(Messages.Index.PriceForHealingCondition);
    public string PriceForRemovingCurses => GetMessage(Messages.Index.PriceForRemovingCurses);
    public string HowManyLP => GetMessage(Messages.Index.HowManyLP);
    public string WhichConditionToHeal => GetMessage(Messages.Index.WhichConditionToHeal);
    public string WhichInventoryAsTarget => GetMessage(Messages.Index.WhichInventoryAsTarget);
    public string WhichItemAsTarget => GetMessage(Messages.Index.WhichItemAsTarget);
    public string InnkeeperGoodSleepWish => GetMessage(Messages.Index.InnkeeperGoodSleepWish);
    public string CannotRepairUnbreakableItem => GetMessage(Messages.Index.CannotRepairUnbreakableItem);
    public string CannotEnchantOrdinaryItem => GetMessage(Messages.Index.CannotEnchantOrdinaryItem);
    public string StayWillCost => GetMessage(Messages.Index.StayWillCost);
    public string PriceForHorse => GetMessage(Messages.Index.PriceForHorse);
    public string PriceForRaft => GetMessage(Messages.Index.PriceForRaft);
    public string PriceForShip => GetMessage(Messages.Index.PriceForShip);
    public string PriceForExamining => GetMessage(Messages.Index.PriceForExamining);
    public string PriceForRepair => GetMessage(Messages.Index.PriceForRepair);
    public string WhichItemToRepair => GetMessage(Messages.Index.WhichItemToRepair);
    public string WhichItemToEnchant => GetMessage(Messages.Index.WhichItemToEnchant);
    public string HowManyCharges => GetMessage(Messages.Index.HowManyCharges);
    public string AlreadyFullyCharged => GetMessage(Messages.Index.AlreadyFullyCharged);
    public string PriceForEnchanting => GetMessage(Messages.Index.PriceForEnchanting);
    public string LastTimeEnchanting => GetMessage(Messages.Index.LastTimeEnchanting);
    public string CannotRechargeAnymore => GetMessage(Messages.Index.CannotRechargeAnymore);
    public string SageIdentifyScroll => GetMessage(Messages.Index.SageIdentifyScroll);
    public string SageSLP => GetMessage(Messages.Index.SageSLP);

    #endregion
}
