using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using System.Collections.Generic;

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

    public interface IGameData
    {
        bool Loaded { get; }
        GameDataSource GameDataSource { get; }
        bool Advanced { get; }
        Dictionary<TravelType, GraphicInfo> StationaryImageInfos { get; }
        TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
        IReadOnlyList<Position> CursorHotspots { get; }
        Places Places { get; }
        IItemManager ItemManager { get; }
        IGraphicProvider GraphicProvider { get; }
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
        Dictionary<string, IDataReader> Dictionaries { get; }
    }
}
