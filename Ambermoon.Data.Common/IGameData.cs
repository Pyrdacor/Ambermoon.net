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
    }

    public interface ILegacyGameData : IGameData
    {
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<string, IDataReader> Dictionaries { get; }
    }
}
