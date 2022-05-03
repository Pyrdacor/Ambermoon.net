using System.Collections.Generic;

namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IFolder : INode
    {
        public IReadOnlyDictionary<string, IFolder> Folders { get; }
        public IReadOnlyDictionary<string, IFile> Files { get; }
    }
}
