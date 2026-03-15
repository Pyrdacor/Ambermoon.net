using System;
using System.Collections.Generic;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;

namespace Ambermoon.Data
{
    public enum GameDataSource
    {
        Unknown,
        Memory,
        ADF,
        LegacyFiles,
        ADFAndLegacyFiles
    }

    public enum GameLanguage
    {
        English,
        German,
        French,
        Polish,
        Czech
    }

    public static class GameLanguageExtensions
    {
        public static GameLanguage ToGameLanguage(this string languageString)
        {
            if (Enum.TryParse(languageString, out GameLanguage gameLanguage))
                return gameLanguage;

            languageString = languageString.ToLower().Trim();

            if (languageString == "german" || languageString == "deutsch" || languageString == "ger" || languageString == "de")
                return GameLanguage.German;
            if (languageString == "french" || languageString == "français" || languageString == "fre" || languageString == "fr")
                return GameLanguage.French;
            if (languageString == "polish" || languageString == "polski" || languageString == "pol" || languageString == "pl")
                return GameLanguage.Polish;
            if (languageString == "czech" || languageString == "český" || languageString == "česky" || languageString == "ces" || languageString == "cze" || languageString == "cs")
                return GameLanguage.Czech;

            return GameLanguage.English;
        }
    }

    public interface IGameData
    {
        bool Loaded { get; }
        GameDataSource GameDataSource { get; }
        GameLanguage Language { get; }
        bool Advanced { get; }
        IReadOnlyDictionary<TravelType, GraphicInfo> StationaryImageInfos { get; }
        TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
        IReadOnlyList<Position> CursorHotspots { get; }
        Places Places { get; }
        IItemManager ItemManager { get; }
        IGraphicInfoProvider GraphicInfoProvider { get; }
        IFontProvider FontProvider { get; }
        IDataNameProvider DataNameProvider { get; }
        ILightEffectProvider LightEffectProvider { get; }
        IMapManager MapManager { get; }
        ISongManager SongManager { get; }
        ICharacterManager CharacterManager { get; }
        IIntroData IntroData { get; }
        IFantasyIntroData FantasyIntroData { get; }
        IOutroData OutroData { get; }
        TextDictionary Dictionary { get; }
    }

    public interface ILegacyGameData : IGameData
    {
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<GameLanguage?, IDataReader> Dictionaries { get; }
    }
}
