namespace Ambermoon.Data
{
    public interface IDataNameProvider
    {
        string DataVersionString { get; }
        string DataInfoString { get; }
        string GetClassName(Class @class);
        string GetRaceName(Race race);
        string GetGenderName(Gender gender);
        string GetGenderName(GenderFlag gender);
        string GetLanguageName(Language language);
        string GetAilmentName(Ailment ailment);
        string GetSpellname(Spell spell);
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
        string GetWorldName(World world);
        string InventoryTitleString { get; }
        string AttributesHeaderString { get; }
        string AbilitiesHeaderString { get; }
        string LanguagesHeaderString { get; }
        string AilmentsHeaderString { get; }
        string DataHeaderString { get; }
        string GetAttributeUIName(Attribute attribute);
        string GetAbilityUIName(Ability ability);
        string LoadWhichSavegameString { get; }
        string WrongRiddlemouthSolutionText { get; }
        string That { get; }
        string DropItemQuestion { get; }
        string DropGoldQuestion { get; }
        string DropFoodQuestion { get; }
        string NotEnoughSP { get; }
        string WrongArea { get; }
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
        string BattleMessageSpellFailed { get; }
        string BattleMessageDeflectedSpell { get; }
        string BattleMessageImmuneToSpellType { get; }
        string BattleMessageTheSpellFailed { get; }
        string BattleMessageCannotDamagePetrifiedMonsters { get; }
        string BattleMessageImmuneToSpell { get; }
        string BattleMessageWhereToBlinkTo { get; }
        string BattleMessageHasBlinked { get; }
        string BattleMessageCannotBlink { get; }
        string BattleMessageCannotCastCauseIrritation { get; }
        string BattleMessageYouDontKnowAnySpellsYet { get; }
        string BattleMessageCannotParry { get; }
        string BattleMessageUseItOnWhom { get; }

        #endregion

        // TODO
    }
}
