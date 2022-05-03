using Ambermoon.Data.Serialization.FileSystem;
using System.Collections.Generic;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class ReadOnlyFolder : IReadOnlyFolder
    {
        readonly Dictionary<string, IReadOnlyFolder> folders = new Dictionary<string, IReadOnlyFolder>();
        readonly Dictionary<string, IReadOnlyFile> files = new Dictionary<string, IReadOnlyFile>();

        public ReadOnlyFolder(string name, IReadOnlyFolder parent)
        {
            Name = name;
            Parent = parent;
        }

        public IReadOnlyDictionary<string, IReadOnlyFolder> Folders => folders;

        public IReadOnlyDictionary<string, IReadOnlyFile> Files => files;

        public string Name { get; }

        public string Path => Parent == null ? Name : Parent.Path + "/" + Name;

        public IReadOnlyFolder Parent { get; }
    }
}
