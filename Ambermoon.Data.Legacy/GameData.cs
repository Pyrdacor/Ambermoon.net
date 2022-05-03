using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public Dictionary<string, IDataReader> Dictionaries { get; } = new Dictionary<string, IDataReader>();
        public Dictionary<TravelType, GraphicInfo> StationaryImageInfos { get; } = new Dictionary<TravelType, GraphicInfo>
        {
            { TravelType.Horse, new GraphicInfo { Width = 32, Height = 22, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.Raft, new GraphicInfo { Width = 32, Height = 11, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.Ship, new GraphicInfo { Width = 48, Height = 34, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.SandLizard, new GraphicInfo { Width = 48, Height = 21, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { TravelType.SandShip, new GraphicInfo { Width = 48, Height = 39, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } }
        };
        private readonly Dictionary<char, Dictionary<string, byte[]>> loadedDisks = new Dictionary<char, Dictionary<string, byte[]>>();
        private readonly LoadPreference loadPreference;
        private readonly ILogger log;
        private readonly bool stopAtFirstError;
        private readonly List<TravelGraphicInfo> travelGraphicInfos = new List<TravelGraphicInfo>(44);
        internal List<Graphic> TravelGraphics { get; } = new List<Graphic>(44);
        public bool Loaded { get; private set; } = false;
        public GameDataSource GameDataSource { get; private set; } = GameDataSource.Memory;
        public string Version { get; private set; } = "Unknown";
        public string Language { get; private set; } = "Unknown";

        public GameData(LoadPreference loadPreference = LoadPreference.PreferExtracted, ILogger logger = null, bool stopAtFirstError = true)
        {
            this.loadPreference = loadPreference;
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

        static bool IsDictionary(string file) => file.ToLower().StartsWith("dictionary.");

        public void LoadFromFileSystem(IReadOnlyFileSystem fileSystem)
        {
            GameDataSource = fileSystem.MemoryFileSystem ? GameDataSource.Memory : GameDataSource.LegacyFiles;
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

        public void LoadFromMemoryZip(Stream stream, Func<IGameData> fallbackGameDataProvider = null)
        {
            Loaded = false;
            GameDataSource = GameDataSource.Memory;
            using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, true);
            var fileReader = new FileReader();
            IGameData fallbackGameData = null;
            IGameData EnsureFallbackData()
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
                return fileReader.ReadFile(name, uncompressedStream);
            }
            bool CheckFileExists(string name)
            {
                return archive.GetEntry(name) != null ||
                    EnsureFallbackData()?.Files?.ContainsKey(name) == true;
            }
            Load(LoadFile, null, CheckFileExists);
        }

        public static string GetVersionInfo(string folderPath) => GetVersionInfo(folderPath, out _);

        // TODO: preference settings?
        public static string GetVersionInfo(string folderPath, out string language)
        {
            language = null;

            if (!Directory.Exists(folderPath))
                return null;

            var possibleAssemblies = new string[2] { "AM2_CPU", "AM2_BLIT" };

            foreach (var assembly in possibleAssemblies)
            {
                var assemblyPath = Path.Combine(folderPath, assembly);

                if (File.Exists(assemblyPath))
                {
                    // check last 128 bytes
                    using var stream = File.OpenRead(assemblyPath);

                    if (stream.Length < 128)
                        return null;

                    stream.Position = stream.Length - 128;
                    Span<byte> buffer = new byte[128];
                    stream.Read(buffer);
                    var version = GetVersionFromAssembly(buffer, out language);
                    if (version == null)
                    {
                        stream.Position = 0;
                        stream.Read(buffer);
                        version = GetVersionFromAssembly(buffer, out language, false);
                    }
                    return version;
                }
            }

            var diskFile = FindDiskFile(folderPath, 'A');

            if (diskFile != null)
            {
                using var stream = File.OpenRead(diskFile);
                var adf = ADFReader.ReadADF(stream);

                foreach (var assembly in possibleAssemblies)
                {
                    if (adf.ContainsKey(assembly))
                    {
                        var data = adf[assembly];

                        if (data.Length < 128)
                            continue;

                        var version = GetVersionFromAssembly(data.TakeLast(128).ToArray(), out language);

                        if (version == null)
                            version = GetVersionFromAssembly(data.ToArray(), out language, false);

                        return version;
                    }
                }
            }

            return null;
        }

        static string GetVersionFromAssembly(Span<byte> last128Bytes, out string language, bool reversed = true)
        {
            language = null;
            if (reversed)
                last128Bytes.Reverse();
            string result = "";
            string version = null;

            for (int i = 0; i < last128Bytes.Length; ++i)
            {
                if (last128Bytes[i] >= 128)
                    result = "";
                else
                {
                    if (last128Bytes[i] == 0 && result.Contains("Ambermoon "))
                    {
                        version = result.Substring(result.Length - 4, 4);
                        result = "";
                    }
                    else if (version != null && last128Bytes[i] < 32)
                    {
                        language = result.Split(' ').LastOrDefault();
                        return version;
                    }
                    else
                        result += (char)last128Bytes[i];
                }
            }

            return version;
        }

        public void Load(string folderPath, bool savesOnly = false)
        {
            Loaded = false;
            GameDataSource = GameDataSource.LegacyFiles;

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
                return fileReader.ReadFile(name, stream);
            };
            Func<char, Dictionary<string, byte[]>> diskLoader = disk =>
            {
                string diskFile = FindDiskFile(folderPath, disk);

                if (diskFile == null)
                    return null;

                using var stream = File.OpenRead(diskFile);

                return ADFReader.ReadADF(stream);
            };
            Func<string, bool> fileExistChecker = name => File.Exists(GetPath(name));
            Load(fileLoader, diskLoader, fileExistChecker, savesOnly);
        }

        void Load(Func<string, IFileContainer> fileLoader, Func<char, Dictionary<string, byte[]>> diskLoader,
            Func<string, bool> fileExistChecker, bool savesOnly = false)
        {
            var ambermoonFiles = savesOnly ? Legacy.Files.AmigaSaveFiles : Legacy.Files.AmigaFiles;
            var fileReader = new FileReader();
            bool foundNoDictionary = true;

            void HandleFileLoaded(string file)
            {
                if (log != null)
                    log.AppendLine("succeeded");

                if (IsDictionary(file))
                {
                    Dictionaries.Add(file.Split('.').Last(), Files[file].Files[1]);
                    foundNoDictionary = false;
                }
            }

            void HandleFileNotFound(string file, char disk)
            {
                if (log != null)
                {
                    log.AppendLine("failed");
                    log.AppendLine($" -> Unable to find file '{file}'.");
                }

                // We only need 1 dictionary, no savegames and only AM2_CPU but not AM2_BLIT.
                if (IsDictionary(file) || disk == 'J' || file == "AM2_BLIT")
                    return;

                if (stopAtFirstError)
                    throw new FileNotFoundException($"Unable to find file '{file}'.");
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
                    HandleFileLoaded(name);
                }
                else if (loadPreference == LoadPreference.ForceExtracted)
                {
                    if (fileExistChecker(name))
                    {
                        Files.Add(name, fileLoader(name));
                        HandleFileLoaded(name);
                    }
                    else
                    {
                        HandleFileNotFound(name, ambermoonFile.Value);
                    }
                }
                else
                {
                    // load from disk
                    var disk = ambermoonFile.Value;

                    if (!loadedDisks.ContainsKey(disk))
                    {
                        var loadedDisk = diskLoader?.Invoke(disk);

                        if (loadedDisk == null)
                        {
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
                                    HandleFileNotFound(name, disk);
                                }
                                else
                                {
                                    Files.Add(name, fileLoader(name));
                                    HandleFileLoaded(name);
                                }
                            }

                            continue;
                        }

                        loadedDisks.Add(disk, loadedDisk);
                    }

                    if (!loadedDisks[disk].ContainsKey(name))
                    {
                        HandleFileNotFound(name, disk);
                    }
                    else
                    {
                        GameDataSource = GameDataSource.ADF;
                        Files.Add(name, fileReader.ReadFile(name, loadedDisks[disk][name]));
                        HandleFileLoaded(name);
                    }
                }
            }

            if (savesOnly)
            {
                Loaded = true;
                return;
            }

            if (foundNoDictionary && stopAtFirstError)
            {
                throw new FileNotFoundException("Unable to find any dictionary file.");
            }

            LoadTravelGraphics();

            try
            {
                var possibleAssemblies = new string[2] { "AM2_CPU", "AM2_BLIT" };

                foreach (var assembly in possibleAssemblies)
                {
                    if (Files.ContainsKey(assembly))
                    {
                        var file = Files[assembly].Files[1];

                        if (file.Size >= 128)
                        {
                            file.Position = file.Size - 128;
                            Version = GetVersionFromAssembly(file.ReadToEnd(), out var language);
                            if (Version == null)
                            {
                                file.Position = 0;
                                Version = GetVersionFromAssembly(file.ReadBytes(128), out language, false);
                            }
                            if (language != null)
                                Language = language;
                            file.Position = 0;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

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
