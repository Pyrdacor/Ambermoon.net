using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("Ambermoon.Data.Pyrdacor")]

namespace Ambermoon.Data.Legacy
{
    public class GameData : ILegacyGameData
    {
        public enum LoadPreference
        {
            PreferAdf,
            PreferExtracted,
            ForceAdf,
            ForceExtracted
        }

        public enum VersionPreference
        {
            Any,
            Pre114,
            Post114
        }

        public interface ILogger
        {
            void Append(string text);
            void AppendLine(string text);
        }

        public Dictionary<string, IFileContainer> Files { get; } = new Dictionary<string, IFileContainer>();
        public Dictionary<string, IDataReader> Dictionaries { get; } = new Dictionary<string, IDataReader>();
        public Dictionary<TravelType, GraphicInfo> StationaryImageInfos { get; } = new Dictionary<TravelType, GraphicInfo>
        {
            { TravelType.Horse, new GraphicInfo { Width = 32, Height = 22, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.Raft, new GraphicInfo { Width = 32, Height = 11, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.Ship, new GraphicInfo { Width = 48, Height = 34, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.SandLizard, new GraphicInfo { Width = 48, Height = 21, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.SandShip, new GraphicInfo { Width = 48, Height = 39, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } }
        };
        private readonly Dictionary<char, Dictionary<string, byte[]>> loadedDisks = [];
        private readonly LoadPreference loadPreference;
        private readonly VersionPreference versionPreference;
        private readonly ILogger log;
        private readonly bool stopAtFirstError;
        private readonly List<TravelGraphicInfo> travelGraphicInfos = new(44);
        private ExecutableData.ExecutableData executableData;
        public IReadOnlyList<Position> CursorHotspots => executableData?.Cursors.Entries.Select(c => new Position(c.HotspotX - 1, c.HotspotY - 1)).ToList().AsReadOnly();
        public Places Places { get; private set; }
        public IGraphicProvider GraphicProvider { get; private set; }
        public ICharacterManager CharacterManager { get; private set; }
        public IItemManager ItemManager => executableData?.ItemManager;
        public IFontProvider FontProvider { get; private set; }
        public IDataNameProvider DataNameProvider { get; private set; }
        public ILightEffectProvider LightEffectProvider { get; private set; }
        public IMapManager MapManager { get; private set; }
        public ISongManager SongManager { get; private set; }
        public IIntroData IntroData { get; private set; }
        public IFantasyIntroData FantasyIntroData { get; private set; }
        public IOutroData OutroData { get; private set; }
        internal List<Graphic> TravelGraphics { get; } = new List<Graphic>(44);
        public bool Loaded { get; private set; } = false;
        public GameDataSource GameDataSource { get; private set; } = GameDataSource.Memory;
        public string Version { get; private set; } = "Unknown";
        public string Language { get; private set; } = "Unknown";
        public bool Advanced { get; private set; } = false;
        public TextDictionary Dictionary { get; private set; }

        public KeyValuePair<string, IDataReader> GetDictionary()
        {
            if (versionPreference != VersionPreference.Pre114 && Dictionaries.TryGetValue("", out var dictionary))
                return KeyValuePair.Create(Language, dictionary);

            if (Dictionaries.TryGetValue(Language.ToLower(), out dictionary))
                return KeyValuePair.Create(Language, dictionary);

            return Dictionaries.First();
        }

        public GameData(LoadPreference loadPreference = LoadPreference.PreferExtracted, ILogger logger = null, bool stopAtFirstError = true,
            VersionPreference versionPreference = VersionPreference.Any)
        {
            this.loadPreference = loadPreference;
            this.versionPreference = versionPreference;
            log = logger;
            this.stopAtFirstError = stopAtFirstError;
        }

        private static string FindDiskFile(string folderPath, char disk)
        {
            if (!Directory.Exists(folderPath))
                return null;

            disk = char.ToLower(disk);
            var adfFiles = Directory.GetFiles(folderPath, "*.adf");

            foreach (var adfFile in adfFiles)
            {
                string filename = Path.GetFileNameWithoutExtension(adfFile).ToLower();

                if (filename.Contains("amb") && (filename.EndsWith(disk) || filename.EndsWith($"({disk})") || filename.EndsWith($"[{disk}]")))
                    return adfFile;
            }

            return null;
        }

        static bool IsDictionary(string file) => file.ToLower().StartsWith("dictionary.") || file.ToLower() == "dict.amb";

        public void LoadFromFileSystem(IReadOnlyFileSystem fileSystem)
        {
            GameDataSource = fileSystem.MemoryFileSystem ? GameDataSource.Memory : GameDataSource.Unknown;
            var fileReader = new FileReader();

            IFileContainer LoadFile(string name)
            {
                var file = fileSystem.GetFile(name);

                if (file == null || file.Stream == null)
                    return null;

                using var reader = file.Stream.GetReader();
                return fileReader.ReadFile(name, reader);
            }
            bool CheckFileExists(string name)
            {
                return fileSystem.GetFile(name) != null;
            }
            Load(LoadFile, null, CheckFileExists);
        }

        public void LoadFromMemoryZip(Stream stream, Func<ILegacyGameData> fallbackGameDataProvider = null,
            Dictionary<string, char> optionalAdditionalFiles = null, Action<float> progressTracker = null,
            bool ignoreMusic = false)
        {
            Loaded = false;
            GameDataSource = GameDataSource.Memory;
            using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, true);
            var fileReader = new FileReader();
            ILegacyGameData fallbackGameData = null;
            ILegacyGameData EnsureFallbackData()
            {
                if (fallbackGameDataProvider == null)
                    return null;
                if (fallbackGameData == null)
                    fallbackGameData = fallbackGameDataProvider?.Invoke();
                return fallbackGameData;
            }
            IFileContainer LoadFile(string name)
            {
                if (archive.GetEntry(name) == null)
                    return EnsureFallbackData().Files[name];

                using var uncompressedStream = new MemoryStream();
                archive.GetEntry(name).Open().CopyTo(uncompressedStream);
                uncompressedStream.Position = 0;
                return fileReader.ReadRawFile(name, uncompressedStream);
            }
            bool CheckFileExists(string name)
            {
                return archive.GetEntry(name) != null ||
                    EnsureFallbackData()?.Files?.ContainsKey(name) == true;
            }
            Load(LoadFile, null, CheckFileExists, false, optionalAdditionalFiles, progressTracker, ignoreMusic);
        }

        public class GameDataWriter
        {
            internal class FileContainerWriter
            {
                public string Name { get; set; }
                public uint Header { get; set; }
                public Dictionary<int, IDataWriter> Files { get; set; }
                public bool Changed { get; set; } = false;

                static FileType GetFileType(FileContainerWriter writer, bool noCompression)
                {
                    var fileType = (FileType)writer.Header;

                    if (noCompression)
                    {
                        if (fileType == FileType.AMNP ||
                            fileType == FileType.AMPC)
                            fileType = FileType.AMBR;
                        else if (fileType == FileType.LOB ||
                            fileType == FileType.VOL1)
                            fileType = FileType.None;
                    }

                    return fileType;
                }

                public static IFileContainer AsContainer(FileContainerWriter writer, bool noCompression) =>
                    FileReader.Create(writer.Name, GetFileType(writer, noCompression), writer.Files.ToDictionary(f => f.Key, f => WriterToReader(f.Value)));
                public static FileContainerWriter FromContainer(IFileContainer container) =>
                    new FileContainerWriter
                    {
                        Name = container.Name,
                        Header = container.Header,
                        Files = container.Files.ToDictionary(f => f.Key, f => ReaderToWriter(f.Value))
                    };
            }

            static IDataWriter ReaderToWriter(IDataReader reader)
            {
                var oldPosition = reader.Position;
                reader.Position = 0;

                var writer = new DataWriter(reader.ReadToEnd());

                reader.Position = oldPosition;

                return writer;
            }

            static IDataReader WriterToReader(IDataWriter writer)
            {
                return new DataReader(writer.ToArray());
            }

            readonly Dictionary<string, FileContainerWriter> files = new Dictionary<string, FileContainerWriter>();

            internal IReadOnlyDictionary<string, FileContainerWriter> Files => files;

            internal GameDataWriter(GameData gameData)
            {
                foreach (var file in gameData.Files)
                    files.Add(file.Key, FileContainerWriter.FromContainer(file.Value));
            }

            public void AddFile(string container, int index, IDataWriter writer)
            {
                files[container].Files.Add(index, writer);
                files[container].Changed = true;
            }

            public void RemoveFile(string container, int index)
            {
                files[container].Files.Remove(index);
                files[container].Changed = true;
            }

            public bool FileExists(string container, int index)
            {
                return files[container].Files.ContainsKey(index);
            }

            public void ReplaceFile(string container, int index, IDataWriter writer)
            {
                files[container].Files[index] = writer;
                files[container].Changed = true;
            }
        }

        public void Save(string targetFolder, Action<GameDataWriter> preSaveAction = null,
            bool saveOnlyChangedContainers = false, Action finishAction = null,
            bool noCompression = false)
        {
            var gameDataWriter = new GameDataWriter(this);

            preSaveAction?.Invoke(gameDataWriter);

            foreach (var file in gameDataWriter.Files)
            {
                if (saveOnlyChangedContainers && !file.Value.Changed)
                    continue;

                var dataWriter = new DataWriter();
                var container = GameDataWriter.FileContainerWriter.AsContainer(file.Value, noCompression);
                FileWriter.Write(dataWriter, container, Compression.LobCompression.LobType.Ambermoon, FileDictionaryCompression.None);

                using var fileStream = File.Create(Path.Combine(targetFolder, file.Key));
                dataWriter.CopyTo(fileStream);
            }

            finishAction?.Invoke();
        }

        public struct GameDataInfo
        {
            public string Version;
            public string Language;
            public bool Advanced;
        }

        static readonly Regex VersionRegex = new Regex(@"[vV]?([0-9]+[.][0-9]+)", RegexOptions.Compiled);

        static GameDataInfo GetInfo(Func<IDataReader> exeProvider, Func<IDataReader> textAmbProvider)
        {
            var info = new GameDataInfo();
            var textAmb = textAmbProvider?.Invoke();

            if (textAmb != null)
            {
                int oldPosition = textAmb.Position;

                try
                {
                    textAmb.Position = textAmb.Size - 1;

                    while (textAmb.Position != 0)
                    {
                        var b = textAmb.PeekByte();

                        if (b == 0 || b >= 0x20)
                        {
                            --textAmb.Position;
                        }
                        else
                        {
                            --textAmb.Position;
                            int versionStringLength = textAmb.ReadByte() * 4;
                            int languageStringLength = textAmb.ReadByte() * 4;
                            string versionString = textAmb.ReadString(versionStringLength).TrimEnd('\0');
                            var versionMatch = VersionRegex.Matches(versionString).LastOrDefault();
                            if (versionMatch == null)
                                info.Version = "1.0";
                            else
                                info.Version = versionMatch.Groups[1].Value;
                            info.Advanced = versionString.ToLower().Contains("adv");
                            string languageString = textAmb.ReadString(languageStringLength).TrimEnd('\0');
                            info.Language = languageString.Trim().Split(' ').Last();
                            break;
                        }
                    }
                }
                finally
                {
                    textAmb.Position = oldPosition;
                }                
            }
            else
            {
                var exe = exeProvider();
                int oldPosition = exe.Position;

                try
                {
                    var hunks = AmigaExecutable.Read(exe);
                    var hunk = (AmigaExecutable.Hunk)hunks.First(h => h.Type == AmigaExecutable.HunkType.Code);
                    var reader = new DataReader(hunk.Data);
                    reader.Position = 6;
                    string versionString = reader.ReadNullTerminatedString();
                    var versionMatch = VersionRegex.Matches(versionString).LastOrDefault();
                    if (versionString == null)
                        info.Version = "1.0";
                    else
                        info.Version = versionMatch.Groups[1].Value;
                    info.Advanced = versionString.ToLower().Contains("adv");
                    string languageString = reader.ReadNullTerminatedString();
                    info.Language = languageString.Trim().Split(' ').Last();
                }
                finally
                {
                    exe.Position = oldPosition;
                }
            }

            return info;
        }

        public static GameDataInfo GetInfo(string folderPath, LoadPreference loadPreference = LoadPreference.PreferExtracted,
            VersionPreference versionPreference = VersionPreference.Any)
        {
            var possibleSources = new string[3] { "Text.amb", "AM2_CPU", "AM2_BLIT" };

            KeyValuePair<int, IDataReader>? GetFirstReader(Func<string, IDataReader> readerProvider)
            {
                for (int i = 0; i < possibleSources.Length; ++i)
                {
                    var reader = readerProvider(possibleSources[i]);

                    if (reader != null)
                        return KeyValuePair.Create(i, reader);
                }

                return null;
            }

            KeyValuePair<int, IDataReader>? GetFirstFileReader()
            {
                return GetFirstReader(file =>
                {
                    var path = Path.Combine(folderPath, file);
                    if (!File.Exists(path))
                        return null;
                    if (file == "Text.amb")
                        return new FileReader().ReadRawFile(file, File.ReadAllBytes(path)).Files[1];
                    return new DataReader(File.ReadAllBytes(path));
                });
            }

            KeyValuePair<int, IDataReader>? GetFirstDiskFileReader()
            {
                var diskFile = FindDiskFile(folderPath, 'A');

                if (diskFile == null)
                    return null;

                using var stream = File.OpenRead(diskFile);
                var adf = ADFReader.ReadADF(stream, versionPreference);

                return GetFirstReader(file =>
                {
                    if (adf.ContainsKey(file))
                    {
                        var data = adf[file];
                        if (file == "Text.amb")
                            return new FileReader().ReadRawFile(file, data).Files[1];
                        return new DataReader(data);
                    }

                    return null;
                });
            }

            GameDataInfo GetInfo(params Func<KeyValuePair<int, IDataReader>?>[] providers)
            {
                foreach (var provider in providers)
                {
                    var reader = provider?.Invoke();

                    if (reader != null)
                    {
                        if (reader.Value.Key == 0)
                            return GameData.GetInfo(null, () => reader.Value.Value);
                        else
                            return GameData.GetInfo(() => reader.Value.Value, null);
                    }
                }

                throw new AmbermoonException(ExceptionScope.Data, "Incomplete game data.");
            }

            if (loadPreference == LoadPreference.ForceExtracted)
            {
                return GetInfo(GetFirstFileReader);
            }
            else if (loadPreference == LoadPreference.ForceAdf)
            {
                return GetInfo(GetFirstDiskFileReader);
            }
            else if (loadPreference == LoadPreference.PreferAdf)
            {
                return GetInfo(GetFirstDiskFileReader, GetFirstFileReader);
            }
            else
            {
                return GetInfo(GetFirstFileReader, GetFirstDiskFileReader);
            }
        }

        public void Load(string folderPath, bool savesOnly = false)
        {
            Loaded = false;
            GameDataSource = GameDataSource.Unknown;

            if (!Directory.Exists(folderPath))
            {
                if (stopAtFirstError)
                    throw new FileNotFoundException("Given data folder does not exist.");

                return;
            }

            var fileReader = new FileReader();
            string GetPath(string name) => Path.Combine(folderPath, name.Replace('/', Path.DirectorySeparatorChar));
            Func<string, IFileContainer> fileLoader = name =>
            {
                using var stream = File.OpenRead(GetPath(name));
                return fileReader.ReadRawFile(name, stream);
            };
            Func<char, Dictionary<string, byte[]>> diskLoader = disk =>
            {
                string diskFile = FindDiskFile(folderPath, disk);

                if (diskFile == null)
                    return null;

                using var stream = File.OpenRead(diskFile);

                return ADFReader.ReadADF(stream, versionPreference);
            };
            Func<string, bool> fileExistChecker = name => File.Exists(GetPath(name));
            Load(fileLoader, diskLoader, fileExistChecker, savesOnly);
        }

        void Load(Func<string, IFileContainer> fileLoader, Func<char, Dictionary<string, byte[]>> diskLoader,
            Func<string, bool> fileExistChecker, bool savesOnly = false, Dictionary<string, char> optionalAdditionalFiles = null,
            Action<float> progressTracker = null, bool ignoreMusic = false)
        {
            var ambermoonFiles = new Dictionary<string, char>(savesOnly ? Legacy.Files.AmigaSaveFiles : Legacy.Files.AmigaFiles);

            if (optionalAdditionalFiles != null)
            {
                foreach (var additionalFile in optionalAdditionalFiles)
                    ambermoonFiles.Add(additionalFile.Key, additionalFile.Value);
            }

            var fileReader = new FileReader();
            bool foundNoDictionary = true;

            if (versionPreference == VersionPreference.Post114)
            {
                foreach (var file in Legacy.Files.Removed114Files)
                    ambermoonFiles.Remove(file);
            }

            if (versionPreference != VersionPreference.Pre114)
            {
                foreach (var file in Legacy.Files.New114Files)
                    ambermoonFiles.Add(file.Key, file.Value);
            }

            float progress = 0.0f;
            float progressPerFile = (savesOnly ? 1.0f : 0.45f) / ambermoonFiles.Count;
            // If not only saves are loaded a lot of the time is needed after the files are loaded for image loading etc.

            progressTracker?.Invoke(progress);

            void HandleFileLoaded(string file, bool fromFiles)
            {
                if (fromFiles)
                {
                    GameDataSource = GameDataSource == GameDataSource.ADF || GameDataSource == GameDataSource.ADFAndLegacyFiles
                        ? GameDataSource.ADFAndLegacyFiles
                        : GameDataSource.LegacyFiles;
                }
                else
                {
                    GameDataSource = GameDataSource == GameDataSource.LegacyFiles || GameDataSource == GameDataSource.ADFAndLegacyFiles
                        ? GameDataSource.ADFAndLegacyFiles
                        : GameDataSource.ADF;
                }

                if (log != null)
                    log.AppendLine("succeeded");

                if (IsDictionary(file))
                {
                    if (file.ToLower() == "dict.amb")
                        Dictionaries.Add("", Files[file].Files[1]);
                    else
                        Dictionaries.Add(file.ToLower().Split('.').Last(), Files[file].Files[1]);
                    foundNoDictionary = false;
                }

                progress += progressPerFile;
                progressTracker?.Invoke(progress);
            }

            void HandleFileNotFound(string file, char disk)
            {
                progress += progressPerFile;
                progressTracker?.Invoke(progress);

                if (optionalAdditionalFiles?.ContainsKey(file) == true)
                    return; // Don't error on missing optional files

                if (log != null)
                {
                    log.AppendLine("failed");
                    log.AppendLine($" -> Unable to find file '{file}'.");
                }

                // We only need 1 dictionary, no savegames and only AM2_CPU but not AM2_BLIT.
                if (IsDictionary(file) || disk == 'J' || file == "AM2_BLIT" || file == "Keymap" || file.StartsWith("Initial/"))
                    return;

                if (stopAtFirstError)
                {
                    if (versionPreference != VersionPreference.Post114 &&
                        Legacy.Files.New114Files.ContainsKey(file))
                        return;

                    throw new FileNotFoundException($"Unable to find file '{file}'.");
                }
            }

            foreach (var ambermoonFile in ambermoonFiles)
            {
                var name = ambermoonFile.Key;

                if (log != null)
                    log.Append($"Trying to load file '{name}' ... ");

                // prefer direct files but also allow loading ADF disks
                if (loadPreference == LoadPreference.PreferExtracted && fileExistChecker(name))
                {
                    Files.Add(name, fileLoader(name));
                    HandleFileLoaded(name, true);
                }
                else if (loadPreference == LoadPreference.ForceExtracted)
                {
                    if (fileExistChecker(name))
                    {
                        Files.Add(name, fileLoader(name));
                        HandleFileLoaded(name, true);
                    }
                    else
                    {
                        if (versionPreference != VersionPreference.Pre114 &&
                            Legacy.Files.Renamed114Files.TryGetValue(name, out string newName) &&
                            fileExistChecker(newName))
                        {
                            // Don't add it here, as it should be part of the file list anyway
                            continue;
                        }
                        else
                        {
                            HandleFileNotFound(name, ambermoonFile.Value);
                        }
                    }
                }
                else
                {
                    // load from disk
                    var disk = ambermoonFile.Value;

                    if (!loadedDisks.ContainsKey(disk))
                    {
                        var loadedDisk = diskLoader?.Invoke(disk);

                        if (loadedDisk != null)
                            loadedDisks.Add(disk, loadedDisk);
                    }

                    if (!loadedDisks.ContainsKey(disk) || !loadedDisks[disk].ContainsKey(name))
                    {
                        if (versionPreference != VersionPreference.Pre114 &&
                            Legacy.Files.Renamed114Files.TryGetValue(name, out string newName) &&
                            loadedDisks.ContainsKey(disk) &&
                            loadedDisks[disk].ContainsKey(newName))
                        {
                            // Don't add it here, as it should be part of the file list anyway
                            continue;
                        }

                        // file not found
                        if (loadPreference == LoadPreference.ForceAdf)
                        {
                            if (log != null)
                            {
                                log.AppendLine("failed");
                                log.AppendLine($" -> Unabled to find ADF disk file with letter '{disk}'. Try to rename your ADF file to 'ambermoon_{disk}.adf'.");
                            }

                            if (stopAtFirstError)
                                throw new FileNotFoundException($"Unabled to find ADF disk file with letter '{disk}'. Try to rename your ADF file to 'ambermoon_{disk}.adf'.");
                        }

                        if (loadPreference == LoadPreference.PreferAdf)
                        {
                            if (!fileExistChecker(name))
                            {
                                if (versionPreference != VersionPreference.Pre114 &&
                                    Legacy.Files.Renamed114Files.TryGetValue(name, out newName) &&
                                    fileExistChecker(newName))
                                {
                                    // Don't add it here, as it should be part of the file list anyway
                                    continue;
                                }
                                else
                                {
                                    HandleFileNotFound(name, disk);
                                }
                            }
                            else
                            {
                                Files.Add(name, fileLoader(name));
                                HandleFileLoaded(name, true);
                            }
                        }
                        else if (loadPreference == LoadPreference.PreferExtracted)
                        {
                            if (versionPreference != VersionPreference.Pre114 &&
                                Legacy.Files.Renamed114Files.TryGetValue(name, out newName) &&
                                fileExistChecker(newName))
                            {
                                // Don't add it here, as it should be part of the file list anyway
                                continue;
                            }
                            else
                            {
                                HandleFileNotFound(name, disk);
                            }
                        }
                    }
                    else
                    {
                        GameDataSource = GameDataSource == GameDataSource.LegacyFiles ? GameDataSource.ADFAndLegacyFiles : GameDataSource.ADF;
                        Files.Add(name, fileReader.ReadRawFile(name, loadedDisks[disk][name]));
                        HandleFileLoaded(name, false);
                    }
                }
            }

            if (savesOnly)
            {
                progressTracker?.Invoke(1.0f);
                Loaded = true;
                return;
            }

            if (foundNoDictionary && stopAtFirstError)
            {
                throw new FileNotFoundException("Unable to find any dictionary file.");
            }

            
            LoadTravelGraphics();
            // We assume 1% of time for this
            progress += 0.01f;
            progressTracker?.Invoke(progress);

            try
            {
                if (Files.TryGetValue("Text.amb", out var textAmb))
                {
                    var info = GetInfo(null, () => textAmb.Files[1]);
                    Version = info.Version;
                    Language = info.Language;
                    Advanced = info.Advanced;
                }
                else if (Files.TryGetValue("AM2_CPU", out var exe) || Files.TryGetValue("AM2_BLIT", out exe))
                {
                    var info = GetInfo(() => exe.Files[1], null);
                    Version = info.Version;
                    Language = info.Language;
                    Advanced = info.Advanced;
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                // We assume 1% of time for this
                progress += 0.01f;
                progressTracker?.Invoke(progress);
            }

            executableData = TryLoad(() => ExecutableData.ExecutableData.FromGameData(this));
            // We assume 3% of time for this
            progress += 0.03f;
            progressTracker?.Invoke(progress);

            if (executableData?.FileList == null && stopAtFirstError)
                throw new AmbermoonException(ExceptionScope.Data, "Incomplete game data. AM2_CPU is missing.");

            T TryLoad<T>(Func<T> provider) where T : class
            {
                try
                {
                    return provider();
                }
                catch
                {
                    if (stopAtFirstError)
                        throw;

                    return null;
                }
            }

            // Now we load 13 things. Each has usually a different load duration.
            // We have 0.5f (50%) or progress left.
            // These values express the progress per following step.
            // It is based on some example duration measurement.
            float[] progresses =
            [
                0.06679695f, 0.02982927f, 0.00666224f, 0.04767520f,
                0.13528821f, 0.00020068f, 0.00008975f, 0.00004560f,
                0.00008313f, 0.26092917f, 0.04355856f, 0.00019003f,
                0.00029546f
            ];
            int progressIndex = 0;
            void UpdateProgress()
            {
                progress += progresses[progressIndex++];
                progressTracker?.Invoke(progress);
            }
         
            IntroData = TryLoad(() => new IntroData(this));
            UpdateProgress();
            FantasyIntroData = TryLoad(() => new FantasyIntroData(this));
            UpdateProgress();
            OutroData = TryLoad(() => new OutroData(this));
            UpdateProgress();
            var additionalPalettes = new List<Graphic>();
            if (IntroData?.IntroPalettes != null)
                additionalPalettes.AddRange(IntroData.IntroPalettes);
            if (OutroData?.OutroPalettes != null)
                additionalPalettes.AddRange(OutroData.OutroPalettes);
            if (FantasyIntroData?.FantasyIntroPalettes != null)
                additionalPalettes.AddRange(FantasyIntroData.FantasyIntroPalettes);
            GraphicProvider = TryLoad(() => new GraphicProvider(this, executableData, additionalPalettes));
            UpdateProgress();
            CharacterManager = TryLoad(() => new CharacterManager(this));
            UpdateProgress();
            if (executableData?.ItemManager != null && Files.TryGetValue("Object_texts.amb", out var objTexts))
            {              
                foreach (var objectTextFile in objTexts.Files)
                    executableData.ItemManager.AddTexts((uint)objectTextFile.Key, Serialization.TextReader.ReadTexts(objectTextFile.Value));
            }
            UpdateProgress();
            FontProvider = TryLoad(() => new FontProvider(executableData));
            UpdateProgress();
            DataNameProvider = TryLoad(() => new DataNameProvider(executableData));
            UpdateProgress();
            LightEffectProvider = TryLoad(() => new LightEffectProvider(executableData));
            UpdateProgress();
            MapManager = TryLoad(() => new MapManager(this, new MapReader(), new TilesetReader(), new LabdataReader(), stopAtFirstError));
            UpdateProgress();
            SongManager = ignoreMusic ? null : TryLoad(() => new SongManager(this));
            UpdateProgress();
            Dictionary = TryLoad(() => TextDictionary.Load(new TextDictionaryReader(), GetDictionary()));
            UpdateProgress();
            Places = TryLoad(() => Places.Load(new PlacesReader(), Files["Place_data"].Files[1]));
            UpdateProgress();

            Loaded = true;
        }

        void LoadTravelGraphics()
        {
            // Travel gfx stores graphics with a header:
            // uword NumberOfHorizontalSprites (a sprite has a width of 16 pixels)
            // uword Height (in pixels)
            // uword XOffset (in pixels relative to drawing position)
            // uword YOffset (in pixels relative to drawing position)
            IFileContainer container;

            try
            {
                container = Files["Travel_gfx.amb"];
            }
            catch (KeyNotFoundException)
            {
                if (stopAtFirstError)
                    throw new FileNotFoundException("Unable to find travel graphics.");
                else
                    return;
            }

            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                GraphicFormat = GraphicFormat.Palette5Bit,
                Alpha = true
            };
            foreach (var file in container.Files)
            {
                var reader = file.Value;
                reader.Position = 0;

                Graphic LoadGraphic()
                {
                    var graphic = new Graphic();
                    graphicReader.ReadGraphic(graphic, reader, graphicInfo);
                    return graphic;
                }

                for (int direction = 0; direction < 4; ++direction)
                {
                    int numSprites = reader.ReadWord();
                    graphicInfo.Height = reader.ReadWord();
                    graphicInfo.Width = numSprites * 16;
                    uint xOffset = reader.ReadWord();
                    uint yOffset = reader.ReadWord();

                    travelGraphicInfos.Add(new TravelGraphicInfo
                    {
                        Width = (uint)graphicInfo.Width,
                        Height = (uint)graphicInfo.Height,
                        OffsetX = xOffset,
                        OffsetY = yOffset
                    });
                    TravelGraphics.Add(LoadGraphic());
                }
            }
        }

        public TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction)
        {
            return travelGraphicInfos[(int)type * 4 + (int)direction];
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
            TicksPerFrame = 0,
            NoDirections = false,
            IgnoreTileType = false,
            UseTopSprite = true
        };
    }
}
