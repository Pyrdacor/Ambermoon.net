using Ambermoon.Data.Serialization.FileSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon.Data.FileSystems.OperatingSystem
{
    internal class ReadOnlyFolder : IReadOnlyFolder
    {
        readonly DirectoryInfo directory;
        Dictionary<string, IReadOnlyFolder> folders;
        Dictionary<string, IReadOnlyFile> files;

        public ReadOnlyFolder(DirectoryInfo directoryInfo, IReadOnlyFolder parent)
        {
            directory = directoryInfo;
            Parent = parent;
        }

        public IReadOnlyDictionary<string, IReadOnlyFolder> Folders
        {
            get
            {
                if (folders != null)
                    return folders;

                return folders = directory.GetDirectories().ToDictionary(d => d.Name, d => (IReadOnlyFolder)new ReadOnlyFolder(d, this));
            }
        }

        public IReadOnlyDictionary<string, IReadOnlyFile> Files
        {
            get
            {
                if (files != null)
                    return files;

                return files = directory.GetFiles().ToDictionary(f => f.Name, f => (IReadOnlyFile)new ReadOnlyFile(f, this));
            }
        }

        public string Name => directory.Name;

        public string Path => directory.FullName;

        public IReadOnlyFolder Parent { get; }
    }
}
