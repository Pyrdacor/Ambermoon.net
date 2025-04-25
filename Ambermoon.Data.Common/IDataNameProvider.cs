﻿using Ambermoon.Data.Enumerations;

namespace Ambermoon.Data
{
    public interface IDataNameProvider
    {
        string On { get; }
        string Off { get; }
        string DataVersionString { get; }
        string DataInfoString { get; }
        string GetClassName(Class @class);
        string GetRaceName(Race race);
        string GetGenderName(Gender gender);
        string GetGenderName(GenderFlag gender);
        string GetLanguageName(Language language);
        string GetConditionName(Condition condition);
        string GetSpellName(Spell spell);
        string GetWorldName(World world);
        string GetItemTypeName(ItemType itemType);
        string GetSongName(Song song);
        string GetElementName(CharacterElement element);
        string GetElementName(ItemElement element);
        string CharacterInfoHitPointsString { get; }
        string CharacterInfoSpellPointsString { get; }
        string CharacterInfoSpellLearningPointsString { get; }
        string CharacterInfoTrainingPointsString { get; }
        string CharacterInfoExperiencePointsString { get; }
        string CharacterInfoGoldAndFoodString { get; }
        string CharacterInfoAPRString { get; }
        string CharacterInfoAgeString { get; }
        string CharacterInfoWeightHeaderString { get; }
        string CharacterInfoWeightString { get; }
        string CharacterInfoDamageString { get; }
        string CharacterInfoDefenseString { get; }
        string InventoryTitleString { get; }
        string AttributesHeaderString { get; }
        string SkillsHeaderString { get; }
        string LanguagesHeaderString { get; }
        string ConditionsHeaderString { get; }
        string DataHeaderString { get; }
        string GetAttributeShortName(Attribute attribute);
        string GetSkillShortName(Skill skill);
        string GetAttributeName(Attribute attribute);
        string GetSkillName(Skill skill);
        string OptionsHeader { get; }
        string ClassesHeaderString { get; }
        string GenderHeaderString { get; }
        string ChooseCharacter { get; }
        string ConfirmCharacter { get; }
        string LoadWhichSavegame { get; }
        string SaveWhichSavegame { get; }
        string ReallyLoad { get; }
        string ReallyOverwriteSave { get; }
        string WrongRiddlemouthSolutionText { get; }
        string NotAllowingToLookIntoBackpack { get; }
        string That { get; }
        string DropItemQuestion { get; }
        string DropGoldQuestion { get; }
        string DropFoodQuestion { get; }
        string NotEnoughSP { get; }
        string WrongArea { get; }
        string WrongWorld { get; }
        string WrongClassToUseItem { get; }
        string WrongPlaceToUseItem { get; }
        string WrongWorldToUseItem { get; }
        string WrongClassToEquipItem { get; }
        string WrongSexToEquipItem { get; }
        string NotEnoughFreeFingers { get; }
        string NotEnoughFreeHands { get; }
        string CannotEquip { get; }
        string CannotEquipInFight { get; }
        string CannotUnequipInFight { get; }
        string ItemHasNoEffectHere { get; }
        string ItemCannotBeUsedHere { get; }
        string CannotUseBrokenItems { get; }
        string WhichItemToUseMessage { get; }
        string WhichItemToExamineMessage { get; }
        string WhichItemToDropMessage { get; }
        string WhichItemToStoreMessage { get; }
        string GoldName { get; }
        string FoodName { get; }
        string DropHowMuchItemsMessage { get; }
        string DropHowMuchGoldMessage { get; }
        string DropHowMuchFoodMessage { get; }
        string StoreHowMuchItemsMessage { get; }
        string StoreHowMuchGoldMessage { get; }
        string StoreHowMuchFoodMessage { get; }
        string GiveHowMuchGoldMessage { get; }
        string GiveHowMuchFoodMessage { get; }
        string GiveToWhom { get; }
        string WhereToMoveIt { get; }
        string TakeHowManyMessage { get; }
        string PersonAsleepMessage { get; }
        string WantToFightMessage { get; }
        string CompassDirections { get; }
        string AttackEscapeFailedMessage { get; }
        string SelectNewLeaderMessage { get; }
        string He { get; }
        string She { get; }
        string His { get; }
        string Her { get; }
        string DontForgetItems { get; }
        string LeaveConversationWithoutItems { get; }
        string LootAfterBattle { get; }
        string ReceiveExp { get; }
        string ChooseBattlePositions { get; }
        string WaitHowManyHours { get; }
        string CannotWaitBecauseOfNearbyMonsters { get; }
        string ItemWeightDisplay { get; }
        string ItemHandsDisplay { get; }
        string ItemFingersDisplay { get; }
        string ItemDamageDisplay { get; }
        string ItemDefenseDisplay { get; }
        string ReviveMessage { get; }
        string DustChangedToAshes { get; }
        string AshesChangedToBody { get; }
        string BodyBurnsUp { get; }
        string AshesFallToDust { get; }
        string IsNotDead { get; }
        string IsNotAsh { get; }
        string IsNotDust { get; }
        string CannotBeResurrected { get; }
        string YouDontKnowAnySpellsYet { get; }
        string CantLearnSpellsOfType { get; }
        string AlreadyKnowsSpell { get; }
        string FailedToLearnSpell { get; }
        string SpellFailed { get; }
        string ThatsNotASpellScroll { get; }
        string NotEnoughSpellLearningPoints { get; }
        string ManagedToLearnSpell { get; }
        string TheSpellFailed { get; }
        string UseSpellOnlyInCitiesOrDungeons { get; }
        string WhichScrollToRead { get; }
        string RestingTooDangerous { get; }
        string ItemIsNotBroken { get; }
        string ItemIsBroken { get; }
        string NoCursedItemFound { get;  }
        string ItemIsCursed { get; }
        string ThisIsNotAMagicalItem { get; }
        string ItemAlreadyFullyCharged { get; }
        string ItemAlreadyIdentified { get; }
        string CannotBeDuplicated { get; }
        string NoRoomForItem { get; }
        string MaxLPDisplay { get; }
        string MaxSPDisplay { get; }
        string MBWDisplay { get; }
        string MBRDisplay { get; }
        string AttributeHeader { get; }
        string SkillHeader { get; }
        string FunctionHeader { get; }
        string Cursed { get; }
        string TiredMessage { get; }
        string ExhaustedMessage { get; }
        string SleepUntilDawn { get; }
        string Sleep8Hours { get; }
        string RestingWouldHaveNoEffect { get; }
        string HasNoMoreFood { get; }
        string RecoveredLP { get; }
        string RecoveredLPAndSP { get; }
        string HasAged { get; }
        string HasDiedOfAge { get; }
        string LockpickBreaks { get; }
        string UnableToPickTheLock { get; }
        string UnlockedChestWithLockpick { get; }
        string UnlockedDoorWithLockpick { get; }
        string YouNoticeATrap { get; }
        string DisarmTrap { get; }
        string FindTrap { get; }
        string DoesNotFindTrap { get; }
        string UnableToDisarmTrap { get; }
        string EscapedTheTrap { get; }
        string WhichItemToOpenChest { get; }
        string WhichItemToOpenDoor { get; }
        string HasOpenedChest { get; }
        string HasOpenedDoor { get; }
        string ThisItemDoesNotOpenChest { get; }
        string ThisItemDoesNotOpenDoor { get; }
        string HasReachedLevel { get; }
        string MaxLevelReached { get; }
        string NextLevelAt { get; }
        string LPAreNow { get; }
        string SPAreNow { get; }
        string SLPAreNow { get; }
        string TPAreNow { get; }
        string APRAreNow { get; }
        string EP { get; }
        string SpecialItemActivated { get; }
        string SpecialItemAlreadyInUse { get; }
        string NoChargesLeft { get; }
        string CannotCallEagleIfNotOnFoot { get; }
        string BlowsTheFlute { get; }
        string CannotUseItHere { get; }
        string CannotUseMagicDiscHere { get; }
        string CannotJumpThroughWalls { get; }
        string MarksPosition { get; }
        string HasntMarkedAPosition { get; }
        string ReturnToMarkedPosition { get; }
        string SeeRoundDiskInFloor { get; }
        string CannotClimbHere { get; }
        string YouLevitate { get; }
        string WhichNumber { get; }
        string AutomapperNotWorkingHere { get; }
        string GameOverLoadOrQuit { get; }
        string GameOverMessage { get; }
        string ReallyQuit { get; }
        string ItemIsImportant { get; }
        string ChestFull { get; }
        string ChestNowFull { get; }
        string NoOneCanCarryThatMuch { get; }
        string CannotCarryAllGold { get; }
        string MapViewNotWorkingHere { get; }
        string TurnOnTuneInAndDropOut { get; }
        string TextBlockMissing { get; }
        string ReviveCatMessage { get; }
        string CannotExchangeExpWithAnimals { get; }
        string CannotExchangeExpWithDead { get; }
        string ThisCantBeMoved { get; }
        string ElementLabel { get; }
        string MountTheWasp { get; }


        #region Conversations

        string DontKnowAnythingSpecialAboutIt { get; }
        string DenyJoiningParty { get; }
        string PartyFull { get; }
        string DenyLeavingPartyOnMoon { get; }
        string CannotSendDeadPeopleAway { get; }
        string CrazyPeopleDontFollowCommands { get; }
        string PetrifiedPeopleCantGoHome { get; }
        string YouDontSpeakSameLanguage { get; }
        string WhoToTalkTo { get; }
        string SelfTalkingIsMad { get; }
        string UnableToTalk { get; }
        string WhichItemToGive { get; }
        string WhichItemToShow { get; }
        string GiveHowMuchGoldToNPC { get; }
        string GiveHowMuchFoodToNPC { get; }
        string NotInterestedInItem { get; }
        string NotInterestedInGold { get; }
        string NotInterestedInFood { get; }
        string MoreGoldNeeded { get; }
        string MoreFoodNeeded { get; }
        string Hello { get; }
        string GoodBye { get; }
        string WellIShouldLeave { get; }

        #endregion


        #region Automap

        string LegendHeader { get; }
        string GetAutomapName(AutomapType automapType);
        string Location { get; }
        string AlreadyAtGotoPoint { get; }
        string GotoPointSaved { get; }
        string WayBackTooDangerous { get; }
        string ReallyWantToGoThere { get; }
        string DarkDontFindWayBack { get; }

        #endregion


        #region Battle messages

        string BattleMessageAttacksWith { get; }
        string BattleMessageAttacks { get; }
        string BattleMessageWasBroken { get; }
        string BattleMessageDidPointsOfDamage { get; }
        string BattleMessageCastsSpell { get; }
        string BattleMessageCastsSpellFrom { get; }
        string BattleMessageWhoToBlink { get; }
        string BattleMessageFlees { get; }
        string BattleMessageWhereToMoveTo { get; }
        string BattleMessageNowhereToMoveTo { get; }
        string BattleMessageNoAmmunition { get; }
        string BattleMessageWhatToAttack { get; }
        string BattleMessageCannotReachAnyone { get; }
        string BattleMessageMissedTheTarget { get; }
        string BattleMessageCannotPenetrateMagicalAura { get; }
        string BattleMessageAttackFailed { get; }
        string BattleMessageImmuneToAttack { get; }
        string BattleMessageAttackWasParried { get; }
        string BattleMessageAttackDidNoDamage { get; }
        string BattleMessageMadeCriticalHit { get; }
        string BattleMessageUsedLastAmmunition { get; }
        string BattleMessageCannotMove { get; }
        string BattleMessageTooFarAway { get; }
        string BattleMessageUnableToAttack { get; }
        string BattleMessageSomeoneAlreadyGoingThere { get; }
        string BattleMessageMonstersAdvance { get; }
        string BattleMessageMoves { get; }
        string BattleMessageWayWasBlocked { get; }
        string BattleMessageHasDroppedWeapon { get; }
        string BattleMessageRetreats { get; }
        string BattleMessagePartyAdvances { get; }
        string BattleMessageWhichPartyMemberAsTarget { get; }
        string BattleMessageWhichMonsterAsTarget { get; }
        string BattleMessageWhichPartyMemberRowAsTarget { get; }
        string BattleMessageWhichMonsterRowAsTarget { get; }
        string BattleMessageDeflectedSpell { get; }
        string BattleMessageImmuneToSpellType { get; }
        string BattleMessageCannotDamagePetrifiedMonsters { get; }
        string BattleMessageImmuneToSpell { get; }
        string BattleMessageWhereToBlinkTo { get; }
        string BattleMessageHasBlinked { get; }
        string BattleMessageCannotBlink { get; }
        string BattleMessageCannotParry { get; }
        string BattleMessageUseItOnWhom { get; }

        #endregion


        #region Places

        string WelcomeAttackTrainer { get; }
        string WelcomeBlacksmith { get; }
        string WelcomeCriticalHitTrainer { get; }
        string WelcomeDisarmTrapTrainer { get; }
        string WelcomeFindTrapTrainer { get; }
        string WelcomeFoodDealer { get; }
        string WelcomeHealer { get; }
        string WelcomeHorseSeller { get; }
        string WelcomeInnkeeper { get; }
        string WelcomeLockPickingTrainer { get; }
        string WelcomeMagician { get; }
        string WelcomeMerchant { get; }
        string WelcomeParryTrainer { get; }
        string WelcomeRaftSeller { get; }
        string WelcomeReadMagicTrainer { get; }
        string WelcomeEnchanter { get; }
        string WelcomeSage { get; }
        string WelcomeSearchTrainer { get; }
        string WelcomeShipSeller { get; }
        string WelcomeSwimTrainer { get; }
        string WelcomeUseMagicTrainer { get; }
        string BuyWhichItem { get; }
        string SellWhichItem { get; }
        string ExamineWhichItem { get; }
        string ExamineWhichItemMerchant { get; }
        string ExamineWhichItemSage { get; }
        string ThisWillCost { get; }
        string ForThisIllGiveYou { get; }
        string AgreeOnPrice { get; }
        string OneFoodCosts { get; }
        string MerchantFull { get; }
        string BuyHowMuchItems { get; }
        string SellHowMuchItems { get; }
        string NotEnoughMoneyToBuy { get; }
        string NotInterestedInItemMerchant { get; }
        string WontBuyBrokenStuff { get; }
        string WantToGoWithoutItemsMerchant { get; }
        string TrainHowOften { get; }
        string PriceForTraining { get; }
        string IncreasedAfterTraining { get; }
        string NotEnoughTrainingPoints { get; }
        string NotEnoughMoney { get; }
        string BuyHowMuchFood { get; }
        string FoodDividedEqually { get; }
        string FoodLeftAfterDividing { get; }
        string WantToLeaveRestOfFood { get; }
        string PriceOfFood { get; }
        string PriceForHealing { get; }
        string PriceForHealingCondition { get; }
        string PriceForRemovingCurses { get; }
        string HowManyLP { get; }
        string WhichConditionToHeal { get; }
        string WhichItemAsTarget { get; }
        string WhichInventoryAsTarget { get; }
        string InnkeeperGoodSleepWish { get; }
        string CannotRepairUnbreakableItem { get; }
        string CannotEnchantOrdinaryItem { get; }
        string StayWillCost { get; }
        string PriceForHorse { get; }
        string PriceForRaft { get; }
        string PriceForShip { get; }
        string PriceForExamining { get; }
        string PriceForRepair { get; }
        string WhichItemToRepair { get; }
        string WhichItemToEnchant { get; }
        string HowManyCharges { get; }
        string AlreadyFullyCharged { get; }
        string PriceForEnchanting { get; }
        string LastTimeEnchanting { get; }
        string CannotRechargeAnymore { get; }
        string SageIdentifyScroll { get; }
        string SageSLP { get; }

        #endregion
    }
}
