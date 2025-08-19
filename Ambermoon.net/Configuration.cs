using Ambermoon.Data.Legacy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ambermoon
{
    internal class Configuration : IConfiguration
    {
        internal const string ConfigurationFileName = "ambermoon.cfg";
        internal const string ExternalSavegameFolder = "external";

        internal static string GetVersionSavegameFolder(GameVersion gameVersion)
        {
            if (gameVersion.ExternalData)
                return ExternalSavegameFolder;

            if (gameVersion.Info.ToLower().Contains("advanced"))
                return $"advanced_{gameVersion.Language.ToString().ToLower()}";

            return gameVersion.Language.ToString().ToLower();
        }

        internal static IEnumerable<string> GetAllPossibleSavegameFolders()
        {
            var folders = new List<string> { ExternalSavegameFolder };
            folders.AddRange(EnumHelper.GetValues<GameLanguage>().Select(l => l.ToString().ToLower()));
            folders.AddRange(EnumHelper.GetValues<GameLanguage>().Select(l => $"advanced_{l.ToString().ToLower()}"));
            return folders;
        }

        public event Action SaveRequested;
        [JsonIgnore]
        public bool FirstStart { get; set; } = false;
        [JsonIgnore]
        public bool IsMobile { get; } = false;

        public bool? UsePatcher { get; set; } = null;
        public int? PatcherTimeout { get; set; } = null;
        public bool? UseProxyForPatcher { get; set; } = null;
		public string PatcherProxy { get; set; } = null;

		public int? WindowX { get; set; } = null;
        public int? WindowY { get; set; } = null;
        public int? MonitorIndex { get; set; } = null;
        public int? Width { get; set; } = null;
        public int? Height { get; set; } = null;
        public int? FullscreenWidth { get; set; } = null;
        public int? FullscreenHeight { get; set; } = null;
        public bool Fullscreen { get; set; } = false;
        public bool UseDataPath { get; set; } = false;
        public string DataPath { get; set; } = ExecutableDirectoryPath;
        public SaveOption SaveOption { get; set; } = SaveOption.ProgramFolder;
        public int GameVersionIndex { get; set; } = -1;
        public bool LegacyMode { get; set; } = false;
        public bool Music { get; set; } = true;
        public int Volume { get; set; } = 100;
        public bool ExternalMusic { get; set; } = false;
        [Obsolete("Use BattleSpeed instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? FastBattleMode { get; set; } = null;
        public int BattleSpeed { get; set; } = 0;
        [Obsolete("Music is no longer cached but streamed.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? CacheMusic { get; set; } = null;
        public bool AutoDerune { get; set; } = true;
        public bool EnableCheats { get; set; } = false;
        public bool ShowButtonTooltips { get; set; } = true;
        public bool ShowFantasyIntro { get; set; } = true;
        public bool ShowIntro { get; set; } = true;
        [Obsolete("Use GraphicFilter instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseGraphicFilter { get; set; } = null;
        public GraphicFilter GraphicFilter { get; set; } = GraphicFilter.None;
        public GraphicFilterOverlay GraphicFilterOverlay { get; set; } = GraphicFilterOverlay.None;
        public Effects Effects { get; set; } = Effects.None;
        public bool ShowPlayerStatsTooltips { get; set; } = true;
        public bool ShowPyrdacorLogo { get; set; } = true;
        public bool ShowAdvancedLogo { get; set; } = true;        
        [Obsolete("Now the fantasy intro is shown instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowThalionLogo { get; set; } = null;
        public bool ShowFloor { get; set; } = true;
        public bool ShowCeiling { get; set; } = true;
        public bool ShowFog { get; set; } = true;
        public bool ExtendedSavegameSlots { get; set; } = true;
        [Obsolete("Use AdditionalSavegameSlots instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] AdditionalSavegameNames { get; set; } = null;
        [Obsolete("Use AdditionalSavegameSlots instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ContinueSavegameSlot { get; set; } = null;
        public AdditionalSavegameSlots[] AdditionalSavegameSlots { get; set; }
        public bool ShowSaveLoadMessage { get; set; } = false;
        public Movement3D Movement3D { get; set; } = Movement3D.WASD;
        public bool TurnWithArrowKeys { get; set; } = true;
        public GameLanguage Language { get; set; } = (CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture)?.Name?.ToLower() switch
        {
            string l when l.StartsWith("de") => GameLanguage.German,
            string l when l.StartsWith("fr") => GameLanguage.French,
            string l when l.StartsWith("pl") => GameLanguage.Polish,
			string l when l.StartsWith("cz") => GameLanguage.Czech,
			_ => GameLanguage.English
        };
        public bool ShowCompletedQuests { get; set; } = true;

        public void RequestSave() => SaveRequested?.Invoke();

        public static string GetSavePath(string version, bool createIfMissing = true)
        {
            string suffix = $"Saves{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";
            string alternativeSuffix = $"SavesRemake{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";

            try
            {
                var path = Path.Combine(BundleDirectory, suffix);

                if (createIfMissing)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        path = Path.Combine(BundleDirectory, alternativeSuffix);
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
                else if (Directory.Exists(path))
                {
                    return path;
                }

                throw new Exception();
            }
            catch
            {
                var path = Path.Combine(FallbackConfigDirectory, suffix);

                if (createIfMissing)
                {                    
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        path = Path.Combine(FallbackConfigDirectory, alternativeSuffix);
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
                else if (Directory.Exists(path))
                {
                    return path;
                }

                return null;
            }
        }

        public static void FixMacOSPaths()
        {
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    // As we changed the place where the config, saves and other files are
                    // stored for macOS 12 and higher we have to check if there were old configs
                    // or saves beforehand and move them over.

                    static void CheckAndMovePath(string relativePath, Func<string, bool> existChecker,
                        Action<string, string> moveAction, bool catchException)
                    {
                        try
                        {
                            string newPath = Path.Combine(BundleDirectory, relativePath);

                            if (!existChecker(newPath))
                            {
                                string oldConfigPath = Path.Combine(ReadonlyBundleDirectory, relativePath);

                                if (existChecker(oldConfigPath))
                                    moveAction(oldConfigPath, newPath);
                                else
                                {
                                    oldConfigPath = Path.Combine(FallbackConfigDirectory, relativePath);

                                    if (existChecker(oldConfigPath))
                                        moveAction(oldConfigPath, newPath);
                                }
                            }
                        }
                        catch
                        {
                            if (!catchException)
                                throw;
                        }
                    }

                    if (Directory.Exists(BrokenMacBundleDirectory))
                    {
                        Directory.Move(BrokenMacBundleDirectory, BundleDirectory);
                    }
                    else if (OperatingSystem.IsMacOSVersionAtLeast(12))
                    {
                        // Move old config over
                        CheckAndMovePath(ConfigurationFileName, File.Exists, File.Move, true);

                        // Move old save folder over
                        try
                        {
                            CheckAndMovePath("Saves", Directory.Exists, Directory.Move, false);
                        }
                        catch
                        {
                            CheckAndMovePath("SavesRemake", Directory.Exists, Directory.Move, true);
                        }

                        // Move screenshots folder
                        CheckAndMovePath("Screenshots", Directory.Exists, Directory.Move, true);
                    }
                }
            }
            catch
            {
                // ignore errors
            }
        }

#pragma warning disable CS0618
        public void UpgradeAdditionalSavegameSlots()
        {
            if (AdditionalSavegameSlots != null)
                return;

            AdditionalSavegameSlots = GetAllPossibleSavegameFolders().Select(f => new AdditionalSavegameSlots
            {
                GameVersionName = f,
                ContinueSavegameSlot = 0,
                Names = new string[Game.NumAdditionalSavegameSlots]
            }).ToArray();

            // Copy old savegame names to new format
            // Note: The amount of version slots is now dynamic but this upgrade is for older configs where it was fixed. So this is ok.
            if (AdditionalSavegameNames != null && GameVersionIndex >= 0 && GameVersionIndex < 3)
            {
                // "external" moved from slot 2 to 4
                var additionalSavegameSlot = AdditionalSavegameSlots[GameVersionIndex == 2 ? 4 : GameVersionIndex];

                additionalSavegameSlot.ContinueSavegameSlot = ContinueSavegameSlot ?? 0;

                for (int i = 0; i < Math.Min(Game.NumAdditionalSavegameSlots, AdditionalSavegameNames.Length); ++i)
                    additionalSavegameSlot.Names[i] = AdditionalSavegameNames[i];
            }

            AdditionalSavegameNames = null;
            ContinueSavegameSlot = null;
        }

        public AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots(string gameVersionName)
        {
            if (AdditionalSavegameSlots == null)
                UpgradeAdditionalSavegameSlots();

            gameVersionName = gameVersionName.ToLower();

            var savegameSlots = AdditionalSavegameSlots.FirstOrDefault(s => s.GameVersionName.ToLower() == gameVersionName);

            if (savegameSlots == null)
            {
                savegameSlots = new AdditionalSavegameSlots
                {
                    GameVersionName = gameVersionName,
                    ContinueSavegameSlot = 0,
                    Names = new string[Game.NumAdditionalSavegameSlots]
                };

                AdditionalSavegameSlots = Enumerable.Concat(AdditionalSavegameSlots, [savegameSlots]).ToArray();
            }

            return savegameSlots;
        }
#pragma warning restore CS0618

        public static readonly string FallbackConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ambermoon.net");

        private static string bundleDirectory = null;
        /// <summary>
        /// The folder path where the bundle is located. This falls back to
        /// the user's app path on macOS if it is a translocated app.
        /// 
        /// If not on Mac this is identical to <see cref="ExecutableDirectoryPath"/>.
        /// </summary>
        [JsonIgnore]
        public static string BundleDirectory
        {
            get
            {
                if (bundleDirectory != null)
                    return bundleDirectory;

                bundleDirectory = ReadonlyBundleDirectory;

                if (!OperatingSystem.IsMacOS())
                    return bundleDirectory;

                if (OperatingSystem.IsMacOSVersionAtLeast(12) || new DirectoryInfo(bundleDirectory).Attributes.HasFlag(FileAttributes.ReadOnly))
                    bundleDirectory = "/Library/Application Support/Ambermoon.net";

                return bundleDirectory;
            }
        }

        // Some weird Mac OS behavior stored stuff in this folder...
        public const string BrokenMacBundleDirectory = "/Applications/Ambermoon.net.app/Contents/Resources/~/Library/Application Support/Ambermoon.net";

        private static string readonlyBundleDirectory = null;
        /// <summary>
        /// The folder path where the bundle is located. This also
        /// allows translocated app paths on macOS as only read access is needed.
        /// 
        /// If not on Mac this is identical to <see cref="ExecutableDirectoryPath"/>.
        /// </summary>
        [JsonIgnore]
        public static string ReadonlyBundleDirectory
        {
            get
            {
                if (readonlyBundleDirectory != null)
                    return readonlyBundleDirectory;

                if (!OperatingSystem.IsMacOS())
                    return readonlyBundleDirectory = ExecutableDirectoryPath;

                try
                {
                    readonlyBundleDirectory = Path.GetDirectoryName(Environment.ProcessPath).TrimEnd('/'); // "MacOS"

                    if (readonlyBundleDirectory.EndsWith("MacOS"))
                    {
                        readonlyBundleDirectory = Path.GetDirectoryName(readonlyBundleDirectory); // "Contents"
                        readonlyBundleDirectory = Path.GetDirectoryName(readonlyBundleDirectory); // "Ambermoon.net.app"
                        readonlyBundleDirectory = Path.GetDirectoryName(readonlyBundleDirectory); // folder which contains the bundle
                    }

                    return readonlyBundleDirectory;
                }
                catch
                {
                    return readonlyBundleDirectory = FallbackConfigDirectory;
                }
            }
        }

        [JsonIgnore]
        private static readonly Regex netFolderRegex = new(@"net[0-9]+\.[0-9]+$", RegexOptions.Compiled);

        /// <summary>
        /// Directory path where the executable is located.
        /// 
        /// On Windows this will consider build pathes of Visual Studio.
        /// In that case the directory of the project file is used.
        /// 
        /// If the application is running via dotnet CLI, the correct
        /// directory of the exe or dll will still be returned.
        /// </summary>
        public static string ExecutableDirectoryPath
        {
            get
            {
                var assemblyPath = Environment.ProcessPath;

#pragma warning disable IL3000
                if (assemblyPath.EndsWith("dotnet") || assemblyPath.EndsWith("dotnet.exe"))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().Location;
                }
#pragma warning restore IL3000

                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (OperatingSystem.IsWindows())
                {
                    if (assemblyDirectory.EndsWith("Debug") || assemblyDirectory.EndsWith("Release")
                         || netFolderRegex.IsMatch(assemblyDirectory))
                    {
                        string projectFile = Path.GetFileNameWithoutExtension(assemblyPath) + ".csproj";

                        var root = new DirectoryInfo(assemblyDirectory);

                        while (root.Parent != null)
                        {
                            if (File.Exists(Path.Combine(root.FullName, projectFile)))
                                break;

                            root = root.Parent;

                            if (root.Parent == null) // we could not find it (should not happen)
                                return assemblyDirectory;
                        }

                        return root.FullName;
                    }
                    else
                    {
                        return assemblyDirectory;
                    }
                }
                else
                {
                    return assemblyDirectory;
                }
            }
        }

        public static Configuration Load(string filename, Configuration defaultValue = null)
        {
            if (!File.Exists(filename))
                return defaultValue;

            var configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));

            if (configuration == null) // corrupt config
            {
                Console.WriteLine("Corrupted configuration detected. Creating a clean one.");

                configuration = defaultValue ?? new Configuration();
                configuration.FirstStart = false;

                try
                {
                    var versionSavegameFolders = GetAllPossibleSavegameFolders().ToArray();
                    var savegameSlots = configuration.AdditionalSavegameSlots = new AdditionalSavegameSlots[versionSavegameFolders.Length];
                    int versionIndex = 0;

                    foreach (var savegameFolder in versionSavegameFolders)
                    {
                        // Ticks of last saving, slot index (1 .. 30)
                        Tuple<long, int> mostRecentSavegameSlotOfVersion = Tuple.Create(0L, -1);
                        var slots = savegameSlots[versionIndex] = new AdditionalSavegameSlots();
                        string savePath = GetSavePath(savegameFolder, false);
                        slots.GameVersionName = savegameFolder;

                        for (int i = 0; i < Game.NumAdditionalSavegameSlots; ++i)
                            slots.Names[i] = "";

                        static long GetLastWriteTicksOfSaveFiles(DirectoryInfo saveFolder)
                        {
                            try
                            {
                                return saveFolder.GetFiles().Where(f => SavegameManager.SaveFileNames.Contains(f.Name)).Max(f => f.LastWriteTime.Ticks);
                            }
                            catch
                            {
                                return 0;
                            }
                        }

                        if (savePath != null && Directory.Exists(savePath))
                        {
                            foreach (var saveFolder in new DirectoryInfo(savePath).GetDirectories())
                            {
                                if (saveFolder.Name.Length == 7 && saveFolder.Name.StartsWith("Save."))
                                {
                                    if (int.TryParse(saveFolder.Name[5..], out int index))
                                    {
                                        if (index > 10 && index <= 10 + Game.NumAdditionalSavegameSlots)
                                        {
                                            slots.Names[index - 10] = saveFolder.Name.Replace('.', ' ');
                                        }

                                        if (index >= 1 && index <= 10 + Game.NumAdditionalSavegameSlots)
                                        {
                                            long lastWriteTicks = Math.Max(saveFolder.LastWriteTime.Ticks, GetLastWriteTicksOfSaveFiles(saveFolder));

                                            if (lastWriteTicks > mostRecentSavegameSlotOfVersion.Item1)
                                                mostRecentSavegameSlotOfVersion = Tuple.Create(lastWriteTicks, index);
                                        }
                                    }
                                }
                            }
                        }

                        slots.ContinueSavegameSlot = Math.Max(0, mostRecentSavegameSlotOfVersion.Item2);

                        ++versionIndex;
                    }
                }
                catch
                {
                    // ignore
                }
            }

#pragma warning disable CS0618
            if (configuration?.UseGraphicFilter == true && configuration.GraphicFilter == GraphicFilter.None)
                configuration.GraphicFilter = GraphicFilter.Blur; // matches the old filter

            configuration.UseGraphicFilter = null;

            if (configuration?.FastBattleMode == true && configuration.BattleSpeed == 0)
                configuration.BattleSpeed = 100;
            else
            {
                if (configuration.BattleSpeed % 10 != 0)
                    configuration.BattleSpeed += 10 - configuration.BattleSpeed % 10;
                configuration.BattleSpeed = Util.Limit(0, configuration.BattleSpeed, 100);
            }

            configuration.FastBattleMode = null;

            if (configuration.ShowThalionLogo == true && !configuration.ShowFantasyIntro)
                configuration.ShowFantasyIntro = true;

            configuration.ShowThalionLogo = null;
#pragma warning restore CS0618

            if (configuration.ShowFog && (!configuration.ShowFloor || !configuration.ShowCeiling))
                configuration.ShowFog = false;

            return configuration;
        }

        public void Save(string filename)
        {
#pragma warning disable CS0618
            // not used anymore
            UseGraphicFilter = null;
            FastBattleMode = null;
            CacheMusic = null;
            ShowThalionLogo = null;
            AdditionalSavegameNames = null;
            ContinueSavegameSlot = null;
#pragma warning restore CS0618

            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            File.WriteAllText(filename, JsonConvert.SerializeObject(this,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                })
            );
        }
    }
}
