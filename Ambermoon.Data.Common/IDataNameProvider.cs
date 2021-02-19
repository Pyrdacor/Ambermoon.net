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
        string GetAilmentName(Ailment ailment);
        string GetSpellname(Spell spell);
        string GetWorldName(World world);
        string GetItemTypeName(ItemType itemType);
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
        string AbilitiesHeaderString { get; }
        string LanguagesHeaderString { get; }
        string AilmentsHeaderString { get; }
        string DataHeaderString { get; }
        string GetAttributeUIName(Attribute attribute);
        string GetAbilityUIName(Ability ability);
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
        string SleepUntilDawn { get; }
        string WhichScrollToRead { get; }

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
        string BattleMessageCannotCastCauseIrritation { get; }
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
        string WelcomeRecharger { get; }
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
        string InnkeeperGoodSleepWish { get; }

        #endregion

        // TODO
    }
}
