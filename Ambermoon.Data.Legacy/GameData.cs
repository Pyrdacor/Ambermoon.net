using System.Collections.Generic;
using System.IO;

namespace Ambermoon.Data.Legacy
{
    public class GameData : IGameData
    {
        public Dictionary<string, IFileContainer> Files { get; } = new Dictionary<string, IFileContainer>();
        private readonly Dictionary<char, Dictionary<string, byte[]>> _loadedDisks = new Dictionary<char, Dictionary<string, byte[]>>();

        private static bool TryDiskFilename(string folderPath, string filename, out string fullPath)
        {
            fullPath = Path.Combine(folderPath, filename + ".adf");

            return File.Exists(fullPath);
        }

        // TODO: Maybe use a better approach later
        private static string FindDiskFile(string folderPath, char disk)
        {
            string diskFile;

            if (TryDiskFilename(folderPath, $"amber_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"ambermoon_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"ambermoong_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"ambermoone_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"amb_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"ambmoo_{disk}", out diskFile))
                return diskFile;
            if (TryDiskFilename(folderPath, $"ambmoon_{disk}", out diskFile))
                return diskFile;

            return null;
        }

        public void Load(string folderPath)
        {
            var ambermoonFiles = Legacy.Files.AmigaFiles;
            var fileReader = new FileReader();

            foreach (var ambermoonFile in ambermoonFiles)
            {
                var name = ambermoonFile.Key;
                var path = Path.Combine(folderPath, name);

                // prefer direct files but also allow loading ADF disks
                if (File.Exists(path))
                    Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                else
                {
                    // load from disk
                    var disk = ambermoonFile.Value;
                    if (!_loadedDisks.ContainsKey(disk))
                    {
                        string diskFile = FindDiskFile(folderPath, disk);

                        if (diskFile == null)
                            continue; // file not found

                        _loadedDisks.Add(disk, ADFReader.ReadADF(File.OpenRead(diskFile)));
                    }
                    Files.Add(name, fileReader.ReadFile(name, _loadedDisks[disk][name]));
                }

            }
        }

        public Character2DAnimationInfo PlayerAnimationInfo => new Character2DAnimationInfo
        {
            FrameWidth = 16,
            FrameHeight = 32,
            StandFrameIndex = 0,
            SitFrameIndex = 12,
            SleepFrameIndex = 16,
            NumStandFrames = 3,
            NumSitFrames = 1,
            NumSleepFrames = 1,
            TicksPerFrame = 0
        };
    }
}
