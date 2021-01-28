using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IGameData
    {
        bool Loaded { get; }
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<string, IDataReader> Dictionaries { get; }
        Dictionary<StationaryImage, GraphicInfo> StationaryImageInfos { get; }
        TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
    }
}
