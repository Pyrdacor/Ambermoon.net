using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IGameData
    {
        Dictionary<string, IFileContainer> Files { get; }

        void Load(string folderPath);
    }
}
