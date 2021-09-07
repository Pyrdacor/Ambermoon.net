using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public enum GameDataSource
    {
        Memory,
        ADF,
        LegacyFiles
    }

    public interface IGameData
    {
        bool Loaded { get; }
        GameDataSource GameDataSource { get; }
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<string, IDataReader> Dictionaries { get; }
        Dictionary<TravelType, GraphicInfo> StationaryImageInfos { get; }
        TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
    }
}
