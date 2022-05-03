using System.Collections.Generic;

namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IReadOnlyFolder : IReadOnlyNode
    {
        public IReadOnlyDictionary<string, IReadOnlyFolder> Folders { get; }
        public IReadOnlyDictionary<string, IReadOnlyFile> Files { get; }
    }
}
