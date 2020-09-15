using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IGameData
    {
        Dictionary<string, IFileContainer> Files { get; }
        Dictionary<string, IDataReader> Dictionaries { get; }

        void Load(string folderPath);
        Character2DAnimationInfo PlayerAnimationInfo { get; }
        Character2DAnimationInfo WorldPlayerAnimationInfo { get; }
    }
}
