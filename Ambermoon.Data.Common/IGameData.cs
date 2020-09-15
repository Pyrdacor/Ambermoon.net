using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IGameData
    {
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<string, IDataReader> Dictionaries { get; }
        Dictionary<StationaryImage, GraphicInfo> StationaryImageInfos { get; }
        TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction);

        void Load(string folderPath);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
    }
}
