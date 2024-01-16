using Ambermoon.Data.Serialization.FileSystem;
using System.Collections.Generic;
using System.Linq;
using static System.IO.Path;

namespace Ambermoon.Data.FileSystems.OperatingSystem
{
    internal class FileSystem : IFileSystem
    {
        readonly string rootPath;
        readonly IFolder rootFolder;

        public FileSystem(string rootPath)
        {
            this.rootPath = rootPath;

            if (!this.rootPath.EndsWith(DirectorySeparatorChar) && !this.rootPath.EndsWith(AltDirectorySeparatorChar))
                this.rootPath += DirectorySeparatorChar;

            if (!System.IO.Directory.Exists(rootPath))
                System.IO.Directory.CreateDirectory(rootPath);

            rootFolder = new Folder(new System.IO.DirectoryInfo(this.rootPath), null);
        }

        public bool MemoryFileSystem => false;

        string ToRelativePath(string path)
        {
            if (path.ToLower().StartsWith(rootPath.ToLower()))
                return path[rootPath.Length..];

            return path;
        }

        string[] GetPathParts(string path)
        {
            return path.Split(DirectorySeparatorChar, AltDirectorySeparatorChar);
        }

        public INode GetNode(string path)
        {
            path = ToRelativePath(path);
            var parts = GetPathParts(path);
            return GetNode(parts, 0, rootFolder, false);
        }

        INode GetNode(string[] pathParts, int currentIndex, IFolder parent, bool getParent)
        {
            string name = pathParts[currentIndex];

            int endIndex = getParent ? pathParts.Length - 2 : pathParts.Length - 1;

            if (currentIndex == endIndex)
            {
                if (parent.Files.TryGetValue(name, out var file))
                    return file;

                if (parent.Folders.TryGetValue(name, out var folder))
                    return folder;                

                return null;
            }
            else
            {
                if (!parent.Folders.TryGetValue(name, out var folder))
                    return null;

                return GetNode(pathParts, currentIndex + 1, folder, getParent);
            }
        }

        public IFile GetFile(string path)
        {
            return GetNode(path) as IFile;
        }

        public IFolder GetFolder(string path)
        {
            return GetNode(path) as IFolder;
        }

        public IFolder CreateFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return rootFolder;

            path = ToRelativePath(path);
            var parts = GetPathParts(path);
            var parent = GetNode(parts, 0, rootFolder, true) as Folder;

            if (parent == null)
                parent = CreateFolder(string.Join("/", parts.Take(parts.Length - 1))) as Folder;

            if (parent == null)
                return null;

            return parent.AddFolder(parts[^1]);
        }

        public IFile CreateEmptyFile(string path)
        {
            return CreateFile(path, null);
        }

        public IFile CreateFile(string path, byte[] data)
        {
            path = ToRelativePath(path);
            var parts = GetPathParts(path);
            var parent = GetNode(parts, 0, rootFolder, true) as Folder;

            if (parent == null)
                parent = CreateFolder(string.Join("/", parts.Take(parts.Length - 1))) as Folder;

            if (parent == null)
                return null;

            return parent.AddFile(parts[^1], data);
        }

        public IReadOnlyFileSystem AsReadOnly() => new ReadOnlyFileSystem(rootPath);

        public IReadOnlyFileStream GetFileReader(string path)
        {
            return GetFile(path).Stream;
        }

        public IEnumerable<IFile> GetAllFiles()
        {
            var foundFiles = new List<IFile>();
            GetAllFilesInFolder(foundFiles, rootFolder);
            return foundFiles;
        }

        void GetAllFilesInFolder(List<IFile> foundFiles, IFolder folder)
        {
            foreach (var subFolder in folder.Folders)
                GetAllFilesInFolder(foundFiles, subFolder.Value);

            foreach (var file in folder.Files)
                foundFiles.Add(file.Value);
        }
    }
}
