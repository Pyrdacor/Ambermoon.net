using Ambermoon.Render;
using System.Collections.Generic;
using System.IO;

namespace Ambermoon.Data.Legacy
{
    public class GameData : IGameData
    {
        public enum LoadPreference
        {
            PreferAdf,
            PreferExtracted,
            ForceAdf,
            ForceExtracted
        }

        public interface ILogger
        {
            void Append(string text);
            void AppendLine(string text);
        }

        public Dictionary<string, IFileContainer> Files { get; } = new Dictionary<string, IFileContainer>();
        private readonly Dictionary<char, Dictionary<string, byte[]>> _loadedDisks = new Dictionary<char, Dictionary<string, byte[]>>();
        private readonly LoadPreference _loadPreference;
        private readonly ILogger _log;
        private readonly bool _stopAtFirstError;

        public GameData(LoadPreference loadPreference = LoadPreference.PreferExtracted, ILogger logger = null, bool stopAtFirstError = true)
        {
            _loadPreference = loadPreference;
            _log = logger;
            _stopAtFirstError = stopAtFirstError;
        }

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

        static bool IsDictionary(string file) => file.ToLower().StartsWith("dictionary.");
        static bool IsSavegame(string file) => file.ToLower().StartsWith("initial/") || file.ToLower().StartsWith("save");

        public void Load(string folderPath)
        {
            var ambermoonFiles = Legacy.Files.AmigaFiles;
            var fileReader = new FileReader();
            bool foundNoDictionary = true;

            void HandleFileLoaded(string file)
            {
                if (_log != null)
                    _log.AppendLine("succeeded");

                if (IsDictionary(file))
                    foundNoDictionary = false;
            }

            void HandleFileNotFound(string file)
            {
                if (_log != null)
                {
                    _log.AppendLine("failed");
                    _log.AppendLine($" -> Unable to find file '{file}'.");
                }

                // We only need 1 dictionary, no savegames and only AM2_CPU but not AM2_BLIT.
                if (IsDictionary(file) || IsSavegame(file) || file == "AM2_BLIT")
                    return;

                if (_stopAtFirstError)
                    throw new FileNotFoundException($"Unable to find file '{file}'.");
            }

            foreach (var ambermoonFile in ambermoonFiles)
            {
                var name = ambermoonFile.Key;
                var path = Path.Combine(folderPath, name.Replace('/', Path.DirectorySeparatorChar));

                if (_log != null)
                    _log.Append($"Trying to load file '{name}' ... ");

                // prefer direct files but also allow loading ADF disks
                if (_loadPreference == LoadPreference.PreferExtracted && File.Exists(path))
                {
                    Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                    HandleFileLoaded(name);
                }
                else if (_loadPreference == LoadPreference.ForceExtracted)
                {
                    if (File.Exists(path))
                    {
                        Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                        HandleFileLoaded(name);
                    }
                    else
                    {
                        HandleFileNotFound(name);
                    }                        
                }
                else
                {
                    // load from disk
                    var disk = ambermoonFile.Value;

                    if (!_loadedDisks.ContainsKey(disk))
                    {
                        string diskFile = FindDiskFile(folderPath, disk);

                        if (diskFile == null)
                        {
                            // file not found
                            if (_loadPreference == LoadPreference.ForceAdf)
                            {
                                if (_log != null)
                                {
                                    _log.AppendLine("failed");
                                    _log.AppendLine($" -> Unabled to find ADF disk file with letter '{disk}'. Try to rename your ADF file to 'ambermoon_{disk}.adf'.");
                                }

                                if (_stopAtFirstError)
                                    throw new FileNotFoundException($"Unabled to find ADF disk file with letter '{disk}'. Try to rename your ADF file to 'ambermoon_{disk}.adf'.");
                            }

                            if (_loadPreference == LoadPreference.PreferAdf)
                            {
                                if (!File.Exists(path))
                                {
                                    HandleFileNotFound(name);
                                }
                                else
                                {
                                    Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                                }
                            }

                            HandleFileLoaded(name);

                            continue;
                        }

                        _loadedDisks.Add(disk, ADFReader.ReadADF(File.OpenRead(diskFile)));
                    }

                    if (!_loadedDisks[disk].ContainsKey(name))
                        HandleFileNotFound(name);
                    else
                    {
                        Files.Add(name, fileReader.ReadFile(name, _loadedDisks[disk][name]));
                        HandleFileLoaded(name);
                    }
                }

            }

            if (foundNoDictionary)
            {
                throw new FileNotFoundException($"Unable to find any dictionary file.");
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

        public Character2DAnimationInfo WorldPlayerAnimationInfo => new Character2DAnimationInfo
        {
            FrameWidth = 12,
            FrameHeight = 24,
            StandFrameIndex = 3 * 17,
            SitFrameIndex = 0,
            SleepFrameIndex = 0,
            NumStandFrames = 1,
            NumSitFrames = 0,
            NumSleepFrames = 0,
            TicksPerFrame = 0
        };
    }
}
