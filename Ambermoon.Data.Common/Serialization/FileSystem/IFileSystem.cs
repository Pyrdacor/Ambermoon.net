using System.Collections.Generic;

namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IFileSystem : IFileReaderProvider
    {
        bool MemoryFileSystem { get; }
        INode GetNode(string path);
        IFolder GetFolder(string path);
        IFile GetFile(string path);
        IFolder CreateFolder(string path);
        IFile CreateFile(string path, byte[] data);
        IFile CreateEmptyFile(string path);
        IReadOnlyFileSystem AsReadOnly();
        IEnumerable<IFile> GetAllFiles();
    }
}
