using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class ReadOnlyFileSystem : IReadOnlyFileSystem
    {
        readonly IReadOnlyFolder rootFolder;
        readonly Dictionary<string, IReadOnlyFile> files = new Dictionary<string, IReadOnlyFile>();
        readonly List<IReadOnlyFile> filesInOrder = new List<IReadOnlyFile>();

        public ReadOnlyFileSystem(Folder rootFolder, Dictionary<string, File> files)
        {
            this.rootFolder = rootFolder.AsReadOnly(null);

            foreach (var file in files)
            {
                var readOnlyFile = GetFile(file.Key);

                if (readOnlyFile == null)
                    throw new KeyNotFoundException($"File '{file.Key}' was not found.");

                this.files.Add(readOnlyFile.Path, readOnlyFile);
                filesInOrder.Add(readOnlyFile);
            }
        }

        public bool MemoryFileSystem => true;

        string[] GetPathParts(string path)
        {
            return path.Split("/");
        }

        public IReadOnlyNode GetNode(string path)
        {
            var parts = GetPathParts(path);
            return GetNode(parts, 0, rootFolder, false);
        }

        IReadOnlyNode GetNode(string[] pathParts, int currentIndex, IReadOnlyFolder parent, bool getParent)
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

        public IReadOnlyFile GetFile(string path)
        {
            return GetNode(path) as IReadOnlyFile;
        }

        public IReadOnlyFolder GetFolder(string path)
        {
            return GetNode(path) as IReadOnlyFolder;
        }

        public IReadOnlyFileStream GetFileReader(string path)
        {
            return GetFile(path).Stream;
        }

        public IEnumerable<IReadOnlyFile> GetAllFiles() => filesInOrder;
    }
}
