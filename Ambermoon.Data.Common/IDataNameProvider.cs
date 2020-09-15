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
        string CharacterInfoHitPointsString { get; }
        string CharacterInfoSpellPointsString { get; }
        string CharacterInfoSpellLearningPointsString { get; }
        string CharacterInfoTrainingPointsString { get; }
        string CharacterInfoExperiencePointsString { get; }
        string CharacterInfoGoldAndFoodString { get; }
        string CharacterInfoAgeString { get; }
        string GetWorldName(World world);
        string InventoryTitleString { get; }
        string LoadWhichSavegameString { get; }
        string WrongRiddlemouthSolutionText { get; }

        // TODO
    }
}
