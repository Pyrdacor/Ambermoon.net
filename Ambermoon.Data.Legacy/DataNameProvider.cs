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

        public string LoadWhichSavegameString => executableData.Messages.GetEntry(Messages.Index.LoadWhichSavegame);

        public string WrongRiddlemouthSolutionText => executableData.Messages.GetEntry(Messages.Index.IsNotTheRightAnswer);

        /// <summary>
        /// This is used if the entered word is not part of the dictionary.
        /// </summary>
        public string That => executableData.Messages.GetEntry(Messages.Index.That);
    }
}
