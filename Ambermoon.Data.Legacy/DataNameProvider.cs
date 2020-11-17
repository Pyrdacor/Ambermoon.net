namespace Ambermoon.Data.Legacy
{
    using ExecutableData;

    public class DataNameProvider : IDataNameProvider
    {
        readonly ExecutableData.ExecutableData executableData;

        public DataNameProvider(ExecutableData.ExecutableData executableData)
        {
            this.executableData = executableData;
        }

        public string DataVersionString => executableData.DataVersionString;
        public string DataInfoString => executableData.DataInfoString;
        public string CharacterInfoAgeString => executableData.UITexts.Entries[UITextIndex.AgeDisplay];
        public string CharacterInfoExperiencePointsString => executableData.UITexts.Entries[UITextIndex.EPDisplay];
        public string CharacterInfoGoldAndFoodString => executableData.UITexts.Entries[UITextIndex.GoldAndFoodDisplay];
        public string CharacterInfoHitPointsString => executableData.UITexts.Entries[UITextIndex.LPDisplay];
        public string CharacterInfoSpellPointsString => executableData.UITexts.Entries[UITextIndex.SPDisplay];
        public string CharacterInfoSpellLearningPointsString => executableData.UITexts.Entries[UITextIndex.SLPDisplay];
        public string CharacterInfoTrainingPointsString => executableData.UITexts.Entries[UITextIndex.TPDisplay];
        public string CharacterInfoWeightHeaderString => executableData.UITexts.Entries[UITextIndex.Weight];
        public string CharacterInfoWeightString => executableData.UITexts.Entries[UITextIndex.WeightKilogramDisplay];
        public string CharacterInfoDamageString => executableData.UITexts.Entries[UITextIndex.LabeledValueDisplay];
        public string CharacterInfoDefenseString => executableData.UITexts.Entries[UITextIndex.LabeledValueDisplay];
        public string GetAilmentName(Ailment ailment) => executableData.AilmentNames.Entries[ailment];
        public string GetClassName(Class @class) => executableData.ClassNames.Entries[@class];
        public string GetGenderName(Gender gender) => gender switch
        {
            Gender.Male => executableData.UITexts.Entries[UITextIndex.Male],
            Gender.Female => executableData.UITexts.Entries[UITextIndex.Female],
            _ => null
        };
        public string GetGenderName(GenderFlag gender) => gender switch
        {
            GenderFlag.Male => executableData.UITexts.Entries[UITextIndex.Male],
            GenderFlag.Female => executableData.UITexts.Entries[UITextIndex.Female],
            GenderFlag.Both => executableData.UITexts.Entries[UITextIndex.BothSexes],
            _ => null
        };
        public string GetLanguageName(Language language) => executableData.LanguageNames.Entries[language];
        public string GetRaceName(Race race) => executableData.RaceNames.Entries[race];
        public string GetSpellname(Spell spell) => executableData.SpellNames.Entries[spell];
        public string GetWorldName(World world) => executableData.WorldNames.Entries[world];
        public string InventoryTitleString => executableData.UITexts.Entries[UITextIndex.Inventory];
        public string AttributesHeaderString => executableData.UITexts.Entries[UITextIndex.Attributes];
        public string AbilitiesHeaderString => executableData.UITexts.Entries[UITextIndex.Abilities];
        public string LanguagesHeaderString => executableData.UITexts.Entries[UITextIndex.Languages];
        public string AilmentsHeaderString => executableData.UITexts.Entries[UITextIndex.Ailments];
        public string GetAttributeUIName(Attribute attribute) => executableData.AttributeNames.ShortNames[attribute];
        public string GetAbilityUIName(Ability ability) => executableData.AbilityNames.ShortNames[ability];
        public string LoadWhichSavegameString => executableData.Messages.GetEntry(Messages.Index.LoadWhichSavegame);
        public string WrongRiddlemouthSolutionText => executableData.Messages.GetEntry(Messages.Index.IsNotTheRightAnswer);
        /// <summary>
        /// This is used if the entered word is not part of the dictionary.
        /// </summary>
        public string That => executableData.Messages.GetEntry(Messages.Index.That);
        public string DropItemQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropIt);
        public string DropGoldQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropGold);
        public string DropFoodQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropFood);
        public string WhichItemToDropMessage => executableData.Messages.GetEntry(Messages.Index.WhichItemToDrop);
        public string WhichItemToStoreMessage => executableData.Messages.GetEntry(Messages.Index.WhichItemToPutInChest);
        public string GoldName => executableData.UITexts.Entries[UITextIndex.Gold];
        public string FoodName => executableData.UITexts.Entries[UITextIndex.Food];
        public string DropHowMuchItemsMessage => executableData.Messages.GetEntry(Messages.Index.DropHowMany);
        public string DropHowMuchGoldMessage => executableData.Messages.GetEntry(Messages.Index.DropHowMuchGold);
        public string DropHowMuchFoodMessage => executableData.Messages.GetEntry(Messages.Index.DropHowMuchFood);
        public string StoreHowMuchItemsMessage => executableData.Messages.GetEntry(Messages.Index.StoreHowMany);
        public string StoreHowMuchGoldMessage => executableData.Messages.GetEntry(Messages.Index.StoreHowMuchGold);
        public string StoreHowMuchFoodMessage => executableData.Messages.GetEntry(Messages.Index.StoreHowMuchFood);
        public string TakeHowManyMessage => executableData.Messages.GetEntry(Messages.Index.TakeHowMany);
        public string PersonAsleepMessage => executableData.Messages.GetEntry(Messages.Index.ThisPersonIsAsleep);
        public string WantToFightMessage => executableData.Messages.GetEntry(Messages.Index.AttackWantToFight);
        public string CompassDirections => executableData.UITexts.Entries[UITextIndex.CardinalDirections];
        public string AttackEscapeFailedMessage => executableData.Messages.GetEntry(Messages.Index.CouldNotEscape);
        public string SelectNewLeaderMessage => executableData.Messages.GetEntry(Messages.Index.SelectNewLeader);
        public string He => executableData.UITexts.Entries[UITextIndex.He];
        public string She => executableData.UITexts.Entries[UITextIndex.She];
        public string His => executableData.UITexts.Entries[UITextIndex.His];
        public string Her => executableData.UITexts.Entries[UITextIndex.Her];


        #region Battle messages

        public string BattleMessageAttacksWith => executableData.Messages.GetEntry(Messages.Index.AttacksWith);
        public string BattleMessageAttacks => executableData.Messages.GetEntry(Messages.Index.Attacks);
        public string BattleMessageWasBroken => executableData.Messages.GetEntry(Messages.Index.WasBroken);
        public string BattleMessageDidPointsOfDamage => executableData.Messages.GetEntry(Messages.Index.DidPointsOfDamage);
        public string BattleMessageCastsSpell => executableData.Messages.GetEntry(Messages.Index.CastsSpell);
        public string BattleMessageCastsSpellFrom => executableData.Messages.GetEntry(Messages.Index.CastsSpellFrom);
        public string BattleMessageWhoToBlink => executableData.Messages.GetEntry(Messages.Index.WhichMemberShouldBeBlinked);
        public string BattleMessageFlees => executableData.Messages.GetEntry(Messages.Index.Flees);
        public string BattleMessageWhereToMoveTo => executableData.Messages.GetEntry(Messages.Index.WhereToMoveTo);
        public string BattleMessageNowhereToMoveTo => executableData.Messages.GetEntry(Messages.Index.NowhereToMoveTo);
        public string BattleMessageNoAmmunition => executableData.Messages.GetEntry(Messages.Index.NoAmmunition);
        public string BattleMessageWhatToAttack => executableData.Messages.GetEntry(Messages.Index.WhatToAttack);
        public string BattleMessageCannotReachAnyone => executableData.Messages.GetEntry(Messages.Index.CannotReachAnyone);
        public string BattleMessageMissedTheTarget => executableData.Messages.GetEntry(Messages.Index.MissedTheTarget);
        public string BattleMessageCannotPenetrateMagicalAura => executableData.Messages.GetEntry(Messages.Index.CannotPenetrateMagicalAura);
        public string BattleMessageAttackFailed => executableData.Messages.GetEntry(Messages.Index.AttackFailed);
        public string BattleMessageAttackWasParried => executableData.Messages.GetEntry(Messages.Index.AttackWasDeflected);
        public string BattleMessageAttackDidNoDamage => executableData.Messages.GetEntry(Messages.Index.AttackDidNoDamage);
        public string BattleMessageMadeCriticalHit => executableData.Messages.GetEntry(Messages.Index.MadeCriticalHit);
        public string BattleMessageUsedLastAmmunition => executableData.Messages.GetEntry(Messages.Index.UsedLastAmmunition);
        public string BattleMessageCannotMove => executableData.Messages.GetEntry(Messages.Index.CannotMove);
        public string BattleMessageTooFarAway => executableData.Messages.GetEntry(Messages.Index.TooFarAway);
        public string BattleMessageUnableToAttack => executableData.Messages.GetEntry(Messages.Index.UnableToAttack);
        public string BattleMessageSomeoneAlreadyGoingThere => executableData.Messages.GetEntry(Messages.Index.SomeoneAlreadyGoingThere);
        public string BattleMessageMonstersAdvance => executableData.Messages.GetEntry(Messages.Index.MonstersAdvance);
        public string BattleMessageMoves => executableData.Messages.GetEntry(Messages.Index.Moves);
        public string BattleMessageWayWasBlocked => executableData.Messages.GetEntry(Messages.Index.WayWasBlocked);
        public string BattleMessageHasDroppedWeapon => executableData.Messages.GetEntry(Messages.Index.HasDroppedWeapon);
        public string BattleMessageRetreats => executableData.Messages.GetEntry(Messages.Index.Retreats);
        public string BattleMessagePartyAdvances => executableData.Messages.GetEntry(Messages.Index.PartyAdvances);
        public string BattleMessageWhichPartyMemberAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichPartyMemberAsTarget);
        public string BattleMessageWhichMonsterAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichMonsterAsTarget);
        public string BattleMessageWhichPartyMemberRowAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichPartyMemberRowAsTarget);
        public string BattleMessageWhichMonsterRowAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichMonsterRowAsTarget);
        public string BattleMessageSpellFailed => executableData.Messages.GetEntry(Messages.Index.SpellFailed);
        public string BattleMessageDeflectedSpell => executableData.Messages.GetEntry(Messages.Index.DeflectedSpell);
        public string BattleMessageImmuneToSpellType => executableData.Messages.GetEntry(Messages.Index.ImmuneToSpellType);
        public string BattleMessageTheSpellFailed => executableData.Messages.GetEntry(Messages.Index.TheSpellFailed);
        public string BattleMessageCannotDamagePetrifiedMonsters => executableData.Messages.GetEntry(Messages.Index.CannotDamagePetrifiedMonsters);
        public string BattleMessageImmuneToSpell => executableData.Messages.GetEntry(Messages.Index.ImmuneToSpell);
        public string BattleMessageWhereToBlinkTo => executableData.Messages.GetEntry(Messages.Index.WhereToBlinkTo);
        public string BattleMessageHasBlinked => executableData.Messages.GetEntry(Messages.Index.HasBlinked);
        public string BattleMessageCannotBlink => executableData.Messages.GetEntry(Messages.Index.CannotBlink);
        public string BattleMessageCannotCastCauseIrritation => executableData.Messages.GetEntry(Messages.Index.CannotCastCauseIrritation);
        public string BattleMessageYouDontKnowAnySpellsYet => executableData.Messages.GetEntry(Messages.Index.YouDontKnowAnySpellsYet);
        public string BattleMessageCannotParry => executableData.Messages.GetEntry(Messages.Index.CannotParry);
        public string BattleMessageUseItOnWhom => executableData.Messages.GetEntry(Messages.Index.UseItOnWhom);

        #endregion
    }
}
