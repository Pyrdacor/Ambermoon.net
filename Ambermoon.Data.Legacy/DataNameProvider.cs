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
    }
}
