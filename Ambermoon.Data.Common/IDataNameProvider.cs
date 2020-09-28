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
        string CharacterInfoHitPointsString { get; }
        string CharacterInfoSpellPointsString { get; }
        string CharacterInfoSpellLearningPointsString { get; }
        string CharacterInfoTrainingPointsString { get; }
        string CharacterInfoExperiencePointsString { get; }
        string CharacterInfoGoldAndFoodString { get; }
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
        string GetAttributeUIName(Attribute attribute);
        string GetAbilityUIName(Ability ability);
        string LoadWhichSavegameString { get; }
        string WrongRiddlemouthSolutionText { get; }
        string That { get; }
        string DropItemQuestion { get; }
        string DropGoldQuestion { get; }
        string DropFoodQuestion { get; }
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
        string TakeHowManyMessage { get; }

        // TODO
    }
}
