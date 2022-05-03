using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class Folder : IFolder
    {
        readonly Dictionary<string, IFolder> folders = new Dictionary<string, IFolder>();
        readonly Dictionary<string, IFile> files = new Dictionary<string, IFile>();

        public Folder(string name, IFolder parent)
        {
            Name = name;
            Parent = parent;
        }

        public IReadOnlyDictionary<string, IFolder> Folders => folders;

        public IReadOnlyDictionary<string, IFile> Files => files;

        public string Name { get; }

        public string Path => Parent == null ? Name : Parent.Path + "/" + Name;

        public IFolder Parent { get; }

        public IFolder AddFolder(string name)
        {
            if (files.ContainsKey(name))
                throw new InvalidOperationException("A file with the same path already exists.");
            if (folders.ContainsKey(name))
                throw new InvalidOperationException("A folder with the same path already exists.");

            var subFolder = new Folder(name, this);
            folders.Add(name, subFolder);
            return subFolder;
        }

        public void AddFile(string name, IFile file)
        {
            if (files.ContainsKey(name))
                throw new InvalidOperationException("A file with the same path already exists.");
            if (folders.ContainsKey(name))
                throw new InvalidOperationException("A folder with the same path already exists.");

            files.Add(name, file);
        }

        public IReadOnlyFolder AsReadOnly(IReadOnlyFolder parent)
        {
            var folder = new ReadOnlyFolder(Name, parent);

            foreach (var subFolder in folders)
                (folder.Folders as Dictionary<string, IReadOnlyFolder>).Add(subFolder.Key, (subFolder.Value as Folder).AsReadOnly(folder));

            foreach (var file in files)
                (folder.Files as Dictionary<string, IReadOnlyFile>).Add(file.Key, (file.Value as File).AsReadOnly(folder));

            return folder;
        }
    }
}
