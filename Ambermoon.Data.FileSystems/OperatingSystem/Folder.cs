using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon.Data.FileSystems.OperatingSystem
{
    internal class Folder : IFolder
    {
        readonly DirectoryInfo directory;
        Dictionary<string, IFolder> folders;
        Dictionary<string, IFile> files;

        public Folder(DirectoryInfo directoryInfo, IFolder parent)
        {
            directory = directoryInfo;
            Parent = parent;
        }

        public IReadOnlyDictionary<string, IFolder> Folders
        {
            get
            {
                if (folders != null)
                    return folders;

                return folders = directory.GetDirectories().ToDictionary(d => d.Name, d => (IFolder)new Folder(d, this));
            }
        }

        public IReadOnlyDictionary<string, IFile> Files
        {
            get
            {
                if (files != null)
                    return files;

                return files = directory.GetFiles().ToDictionary(f => f.Name, f => (IFile)new File(f, this));
            }
        }

        public string Name => directory.Name;

        public string Path => directory.FullName;

        public IFolder Parent { get; }

        public IFolder AddFolder(string name)
        {
            if (files.ContainsKey(name))
                throw new InvalidOperationException("A file with the same path already exists.");
            if (folders.ContainsKey(name))
                throw new InvalidOperationException("A folder with the same path already exists.");

            string newPath = System.IO.Path.Combine(Path, name);

            try
            {
                var subDir = directory.CreateSubdirectory(newPath);
                var subFolder = new Folder(subDir, this);
                folders.Add(name, subFolder);
                return subFolder;
            }
            catch
            {
                return null;
            }
        }

        public IFile AddFile(string name, byte[] data)
        {
            if (files.ContainsKey(name))
                throw new InvalidOperationException("A file with the same path already exists.");
            if (folders.ContainsKey(name))
                throw new InvalidOperationException("A folder with the same path already exists.");

            string newPath = System.IO.Path.Combine(Path, name);

            try
            {
                var fileInfo = new FileInfo(newPath);
                using var stream = fileInfo.Create();

                if (data != null && data.Length != 0)
                    stream.Write(data, 0, data.Length);

                var file = new File(fileInfo, this);
                files.Add(name, file);
                return file;
            }
            catch
            {
                return null;
            }
        }
    }
}
