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

        public string On => executableData.UITexts.Entries[UITextIndex.On];
        public string Off => executableData.UITexts.Entries[UITextIndex.Off];
        public string DataVersionString => executableData.DataVersionString;
        public string DataInfoString => executableData.DataInfoString;
        public string CharacterInfoAgeString => executableData.UITexts.Entries[UITextIndex.AgeDisplay];
        public string CharacterInfoAPRString => executableData.UITexts.Entries[UITextIndex.APR];
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
        public string GetItemTypeName(ItemType itemType) => executableData.ItemTypeNames.Entries[itemType];
        public string InventoryTitleString => executableData.UITexts.Entries[UITextIndex.Inventory];
        public string AttributesHeaderString => executableData.UITexts.Entries[UITextIndex.Attributes];
        public string AbilitiesHeaderString => executableData.UITexts.Entries[UITextIndex.Abilities];
        public string LanguagesHeaderString => executableData.UITexts.Entries[UITextIndex.Languages];
        public string AilmentsHeaderString => executableData.UITexts.Entries[UITextIndex.Ailments];
        public string DataHeaderString => executableData.UITexts.Entries[UITextIndex.DataHeader];
        public string GetAttributeUIName(Attribute attribute) => executableData.AttributeNames.ShortNames[attribute];
        public string GetAbilityUIName(Ability ability) => executableData.AbilityNames.ShortNames[ability];
        public string OptionsHeader => executableData.Messages.GetEntry(Messages.Index.Options);
        public string ClassesHeaderString => executableData.UITexts.Entries[UITextIndex.ClassHeader];
        public string GenderHeaderString => executableData.UITexts.Entries[UITextIndex.Sex];
        public string ChooseCharacter => executableData.UITexts.Entries[UITextIndex.ChooseCharacter];
        public string ConfirmCharacter => executableData.Messages.GetEntry(Messages.Index.HappyWithCharacter);
        public string LoadWhichSavegame => executableData.Messages.GetEntry(Messages.Index.LoadWhichSavegame);
        public string SaveWhichSavegame => executableData.Messages.GetEntry(Messages.Index.SaveWhichSavegame);
        public string ReallyLoad => executableData.Messages.GetEntry(Messages.Index.ReallyLoad);
        public string ReallyOverwriteSave => executableData.Messages.GetEntry(Messages.Index.ReallyOverwriteSave);
        public string WrongRiddlemouthSolutionText => executableData.Messages.GetEntry(Messages.Index.IsNotTheRightAnswer);
        /// <summary>
        /// This is used if the entered word is not part of the dictionary.
        /// </summary>
        public string That => executableData.Messages.GetEntry(Messages.Index.That);
        public string DropItemQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropIt);
        public string DropGoldQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropGold);
        public string DropFoodQuestion => executableData.Messages.GetEntry(Messages.Index.ReallyDropFood);
        public string NotEnoughSP => executableData.Messages.GetEntry(Messages.Index.NotEnoughSP);
        public string WrongArea => executableData.Messages.GetEntry(Messages.Index.WrongArea);
        public string WrongWorld => executableData.Messages.GetEntry(Messages.Index.WrongWorld);
        public string WrongClassToUseItem => executableData.Messages.GetEntry(Messages.Index.WrongClassToUseItem);
        public string WrongPlaceToUseItem => executableData.Messages.GetEntry(Messages.Index.WrongPlaceToUseItem);
        public string WrongWorldToUseItem => executableData.Messages.GetEntry(Messages.Index.WrongWorldToUseItem);
        public string WrongClassToEquipItem => executableData.Messages.GetEntry(Messages.Index.WrongClassToEquip);
        public string WrongSexToEquipItem => executableData.Messages.GetEntry(Messages.Index.WrongSexToEquip);
        public string NotEnoughFreeFingers => executableData.Messages.GetEntry(Messages.Index.NotEnoughFreeFingers);
        public string NotEnoughFreeHands => executableData.Messages.GetEntry(Messages.Index.NotEnoughFreeHands);
        public string CannotEquip => executableData.Messages.GetEntry(Messages.Index.ThisCannotBeEquipped);
        public string CannotEquipInFight => executableData.Messages.GetEntry(Messages.Index.CannotEquipInCombat);
        public string CannotUnequipInFight => executableData.Messages.GetEntry(Messages.Index.CannotUnequipInCombat);
        public string ItemHasNoEffectHere => executableData.Messages.GetEntry(Messages.Index.CannotUseItHere);
        public string ItemCannotBeUsedHere => executableData.Messages.GetEntry(Messages.Index.ItemCannotBeUsedHere);
        public string CannotUseBrokenItems => executableData.Messages.GetEntry(Messages.Index.CannotUseBrokenItems);
        public string WhichItemToUseMessage => executableData.Messages.GetEntry(Messages.Index.WhichItemToUse);
        public string WhichItemToExamineMessage => executableData.Messages.GetEntry(Messages.Index.WhichItemToExamine);
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
        public string GiveHowMuchGoldMessage => executableData.Messages.GetEntry(Messages.Index.GiveHowMuchGold);
        public string GiveHowMuchFoodMessage => executableData.Messages.GetEntry(Messages.Index.GiveHowMuchFood);
        public string GiveToWhom => executableData.Messages.GetEntry(Messages.Index.WhomGiveItTo);
        public string WhereToMoveIt => executableData.Messages.GetEntry(Messages.Index.WhereToMoveItTo);
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
        public string DontForgetItems => executableData.Messages.GetEntry(Messages.Index.DontForgetItems);
        public string LootAfterBattle => executableData.Messages.GetEntry(Messages.Index.LootAfterBattle);
        public string ReceiveExp => executableData.Messages.GetEntry(Messages.Index.ReceiveExp);
        public string ChooseBattlePositions => executableData.Messages.GetEntry(Messages.Index.EnterBattlePositions);
        public string WaitHowManyHours => executableData.Messages.GetEntry(Messages.Index.WaitHowManyHours);
        public string CannotWaitBecauseOfNearbyMonsters => executableData.Messages.GetEntry(Messages.Index.WaitingIsTooDangerous);
        public string ItemWeightDisplay => executableData.UITexts.Entries[UITextIndex.WeightGramDisplay];
        public string ItemHandsDisplay => executableData.UITexts.Entries[UITextIndex.HandsDisplay];
        public string ItemFingersDisplay => executableData.UITexts.Entries[UITextIndex.FingersDisplay];
        public string ItemDamageDisplay => executableData.UITexts.Entries[UITextIndex.DamageDisplay];
        public string ItemDefenseDisplay => executableData.UITexts.Entries[UITextIndex.DefenseDisplay];
        public string ReviveMessage => executableData.Messages.GetEntry(Messages.Index.ReviveMessage);
        public string DustChangedToAshes => executableData.Messages.GetEntry(Messages.Index.DustChangedToAshes);
        public string AshesChangedToBody => executableData.Messages.GetEntry(Messages.Index.AshesChangedToBody);
        public string YouDontKnowAnySpellsYet => executableData.Messages.GetEntry(Messages.Index.YouDontKnowAnySpellsYet);
        public string CantLearnSpellsOfType => executableData.Messages.GetEntry(Messages.Index.CantLearnSpellsOfType);
        public string AlreadyKnowsSpell => executableData.Messages.GetEntry(Messages.Index.AlreadyKnowsSpell);
        public string FailedToLearnSpell => executableData.Messages.GetEntry(Messages.Index.FailedToLearnSpell);
        public string SpellFailed => executableData.Messages.GetEntry(Messages.Index.SpellFailed);
        public string ThatsNotASpellScroll => executableData.Messages.GetEntry(Messages.Index.ThatsNotASpellScroll);
        public string NotEnoughSpellLearningPoints => executableData.Messages.GetEntry(Messages.Index.NotEnoughSpellLearningPoints);
        public string ManagedToLearnSpell => executableData.Messages.GetEntry(Messages.Index.ManagedToLearnSpell);
        public string TheSpellFailed => executableData.Messages.GetEntry(Messages.Index.TheSpellFailed);
        public string UseSpellOnlyInCitiesOrDungeons => executableData.Messages.GetEntry(Messages.Index.UseSpellOnlyInCitiesOrDungeons);
        public string SleepUntilDawn => executableData.Messages.GetEntry(Messages.Index.SleepUntilDawn);
        public string WhichScrollToRead => executableData.Messages.GetEntry(Messages.Index.WhichScrollToRead);
        public string RestingTooDangerous => executableData.Messages.GetEntry(Messages.Index.RestingTooDangerous);
        public string ItemIsNotBroken => executableData.Messages.GetEntry(Messages.Index.ItemIsNotBroken);
        public string ItemIsBroken => executableData.Messages.GetEntry(Messages.Index.ItemIsBroken);
        public string NoCursedItemFound => executableData.Messages.GetEntry(Messages.Index.NoCursedItemFound);
        public string ItemIsCursed => executableData.Messages.GetEntry(Messages.Index.ThisItemIsCursed);
        public string ThisIsNotAMagicalItem => executableData.Messages.GetEntry(Messages.Index.ThisIsNotAMagicalItem);
        public string ItemAlreadyFullyCharged => executableData.Messages.GetEntry(Messages.Index.ItemAlreadyFullyCharged);
        public string ItemAlreadyIdentified => executableData.Messages.GetEntry(Messages.Index.ItemAlreadyIdentified);



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
        public string BattleMessageDeflectedSpell => executableData.Messages.GetEntry(Messages.Index.DeflectedSpell);
        public string BattleMessageImmuneToSpellType => executableData.Messages.GetEntry(Messages.Index.ImmuneToSpellType);
        public string BattleMessageCannotDamagePetrifiedMonsters => executableData.Messages.GetEntry(Messages.Index.CannotDamagePetrifiedMonsters);
        public string BattleMessageImmuneToSpell => executableData.Messages.GetEntry(Messages.Index.ImmuneToSpell);
        public string BattleMessageWhereToBlinkTo => executableData.Messages.GetEntry(Messages.Index.WhereToBlinkTo);
        public string BattleMessageHasBlinked => executableData.Messages.GetEntry(Messages.Index.HasBlinked);
        public string BattleMessageCannotBlink => executableData.Messages.GetEntry(Messages.Index.CannotBlink);
        public string BattleMessageCannotCastCauseIrritation => executableData.Messages.GetEntry(Messages.Index.CannotCastCauseIrritation);
        public string BattleMessageCannotParry => executableData.Messages.GetEntry(Messages.Index.CannotParry);
        public string BattleMessageUseItOnWhom => executableData.Messages.GetEntry(Messages.Index.UseItOnWhom);

        #endregion


        #region Places

        public string WelcomeAttackTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeAttackTrainer);
        public string WelcomeBlacksmith => executableData.Messages.GetEntry(Messages.Index.WelcomeBlacksmith);
        public string WelcomeCriticalHitTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeCriticalHitTrainer);
        public string WelcomeDisarmTrapTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeDisarmTrapTrainer);
        public string WelcomeFindTrapTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeFindTrapTrainer);
        public string WelcomeFoodDealer => executableData.Messages.GetEntry(Messages.Index.WelcomeFoodDealer);
        public string WelcomeHealer => executableData.Messages.GetEntry(Messages.Index.WelcomeHealer);
        public string WelcomeHorseSeller => executableData.Messages.GetEntry(Messages.Index.WelcomeHorseSeller);
        public string WelcomeInnkeeper => executableData.Messages.GetEntry(Messages.Index.WelcomeInnkeeper);
        public string WelcomeLockPickingTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeLockPickingTrainer);
        public string WelcomeMagician => executableData.Messages.GetEntry(Messages.Index.WelcomeMagician);
        public string WelcomeMerchant => executableData.Messages.GetEntry(Messages.Index.WelcomeMerchant);
        public string WelcomeParryTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeParryTrainer);
        public string WelcomeRaftSeller => executableData.Messages.GetEntry(Messages.Index.WelcomeRaftSeller);
        public string WelcomeReadMagicTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeReadMagicTrainer);
        public string WelcomeRecharger => executableData.Messages.GetEntry(Messages.Index.WelcomeRecharger);
        public string WelcomeSage => executableData.Messages.GetEntry(Messages.Index.WelcomeSage);
        public string WelcomeSearchTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeSearchTrainer);
        public string WelcomeShipSeller => executableData.Messages.GetEntry(Messages.Index.WelcomeShipSeller);
        public string WelcomeSwimTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeSwimTrainer);
        public string WelcomeUseMagicTrainer => executableData.Messages.GetEntry(Messages.Index.WelcomeUseMagicTrainer);
        public string BuyWhichItem => executableData.Messages.GetEntry(Messages.Index.WhichItemToBuy);
        public string SellWhichItem => executableData.Messages.GetEntry(Messages.Index.WhichItemToSell);
        public string ExamineWhichItem => executableData.Messages.GetEntry(Messages.Index.WhichItemToExamine);
        public string ExamineWhichItemMerchant => executableData.Messages.GetEntry(Messages.Index.WhichMerchantItemToExamine);
        public string ExamineWhichItemSage => executableData.Messages.GetEntry(Messages.Index.WhichItemToExamineSage);
        public string ThisWillCost => executableData.Messages.GetEntry(Messages.Index.ThisWillCost);
        public string ForThisIllGiveYou => executableData.Messages.GetEntry(Messages.Index.ForThisIllGiveYou);
        public string AgreeOnPrice => executableData.Messages.GetEntry(Messages.Index.AgreeOnPrice);
        public string OneFoodCosts => executableData.Messages.GetEntry(Messages.Index.AgreeOnFoodPrice);
        public string MerchantFull => executableData.Messages.GetEntry(Messages.Index.CannotBuyAnymore);
        public string BuyHowMuchItems => executableData.Messages.GetEntry(Messages.Index.BuyHowMany);
        public string SellHowMuchItems => executableData.Messages.GetEntry(Messages.Index.SellHowMany);
        public string NotEnoughMoneyToBuy => executableData.Messages.GetEntry(Messages.Index.NotEnoughMoney);
        public string NotInterestedInItemMerchant => executableData.Messages.GetEntry(Messages.Index.NotInterestedInTrinket);
        public string WantToGoWithoutItemsMerchant => executableData.Messages.GetEntry(Messages.Index.LeaveBoughtGoods);
        public string TrainHowOften => executableData.Messages.GetEntry(Messages.Index.TrainHowOften);
        public string PriceForTraining => executableData.Messages.GetEntry(Messages.Index.PriceForTraining);
        public string IncreasedAfterTraining => executableData.Messages.GetEntry(Messages.Index.IncreasedAfterTraining);
        public string NotEnoughTrainingPoints => executableData.Messages.GetEntry(Messages.Index.NotEnoughTrainingPoints);
        public string NotEnoughMoney => executableData.Messages.GetEntry(Messages.Index.NotEnoughMoney);
        public string BuyHowMuchFood => executableData.Messages.GetEntry(Messages.Index.BuyHowMuchFood);
        public string FoodDividedEqually => executableData.Messages.GetEntry(Messages.Index.FoodDividedEqually);
        public string FoodLeftAfterDividing => executableData.Messages.GetEntry(Messages.Index.FoodLeftAfterDividing);
        public string WantToLeaveRestOfFood => executableData.Messages.GetEntry(Messages.Index.WantToLeaveRestOfFood);
        public string PriceOfFood => executableData.Messages.GetEntry(Messages.Index.PriceOfFood);
        public string PriceForHealing => executableData.Messages.GetEntry(Messages.Index.PriceForHealing);
        public string PriceForHealingCondition => executableData.Messages.GetEntry(Messages.Index.PriceForHealingCondition);
        public string PriceForRemovingCurses => executableData.Messages.GetEntry(Messages.Index.PriceForRemovingCurses);
        public string HowManyLP => executableData.Messages.GetEntry(Messages.Index.HowManyLP);
        public string WhichConditionToHeal => executableData.Messages.GetEntry(Messages.Index.WhichConditionToHeal);
        public string WhichInventoryAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichInventoryAsTarget);
        public string WhichItemAsTarget => executableData.Messages.GetEntry(Messages.Index.WhichItemAsTarget);
        public string InnkeeperGoodSleepWish => executableData.Messages.GetEntry(Messages.Index.InnkeeperGoodSleepWish);

        #endregion
    }
}
