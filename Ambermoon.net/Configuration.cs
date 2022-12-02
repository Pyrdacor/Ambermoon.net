using Ambermoon.Data.Legacy;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ambermoon
{
    internal class Configuration : IConfiguration
    {
        [JsonIgnore]
        internal static readonly string[] VersionSavegameFolders = new string[5]
        {
            "german",
            "english",
            "advanced_german",
            "advanced_english",
            "external"
        };

        public event Action SaveRequested;
        [JsonIgnore]
        public bool FirstStart { get; set; } = false;
        [JsonIgnore]
        public bool IsMobile { get; } = false;

        public bool? UsePatcher { get; set; } = null;
        public int? PatcherTimeout { get; set; } = null;

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
        [JsonIgnore] // TODO: remove attribute later
        public bool ShowFantasyIntro { get; set; } = false; // TODO: change to true later
        [JsonIgnore] // TODO: remove attribute later
        public bool ShowIntro { get; set; } = false; // TODO: change to true later
        [Obsolete("Use GraphicFilter instead.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseGraphicFilter { get; set; } = null;
        public GraphicFilter GraphicFilter { get; set; } = GraphicFilter.None;
        public GraphicFilterOverlay GraphicFilterOverlay { get; set; } = GraphicFilterOverlay.None;
        public Effects Effects { get; set; } = Effects.None;
        public bool ShowPlayerStatsTooltips { get; set; } = true;
        public bool ShowPyrdacorLogo { get; set; } = true;
        public bool ShowThalionLogo { get; set; } = true;
        public bool ShowFloor { get; set; } = true;
        public bool ShowCeiling { get; set; } = true;
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

#pragma warning disable CS0618
        public void UpgradeAdditionalSavegameSlots()
        {
            if (AdditionalSavegameSlots != null)
                return;

            AdditionalSavegameSlots = VersionSavegameFolders.Select(f => new AdditionalSavegameSlots
            {
                GameVersionName = f,
                ContinueSavegameSlot = 0,
                Names = new string[Game.NumAdditionalSavegameSlots]
            }).ToArray();

            // Copy old savegame names to new format
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

        public AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots()
        {
            if (GameVersionIndex < 0 || GameVersionIndex >= VersionSavegameFolders.Length)
#if DEBUG
                GameVersionIndex = VersionSavegameFolders.Length - 1; // external
#else
                GameVersionIndex = 0;
#endif

            if (AdditionalSavegameSlots == null)
                UpgradeAdditionalSavegameSlots();
            else if (GameVersionIndex >= AdditionalSavegameSlots.Length)
            {
                var versionSlots = new AdditionalSavegameSlots[VersionSavegameFolders.Length];

                Array.Copy(AdditionalSavegameSlots, versionSlots, AdditionalSavegameSlots.Length);

                for (int i = AdditionalSavegameSlots.Length; i < VersionSavegameFolders.Length; ++i)
                {
                    versionSlots[i] = new AdditionalSavegameSlots
                    {
                        GameVersionName = VersionSavegameFolders[i],
                        ContinueSavegameSlot = 0,
                        Names = new string[Game.NumAdditionalSavegameSlots]
                    };
                }

                AdditionalSavegameSlots = versionSlots;
            }

            return AdditionalSavegameSlots[GameVersionIndex];
        }
#pragma warning restore CS0618

        public static readonly string FallbackConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ambermoon");

        /// <summary>
        /// The folder path where the bundle is located.
        /// 
        /// If not on Mac this is identical to <see cref="ExecutableDirectoryPath"/>.
        /// </summary>
        public static string BundleDirectory
        {
            get
            {
                if (!OperatingSystem.IsMacOS())
                    return ExecutableDirectoryPath;

                try
                {
                    var bundleDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); // "MacOS"
                    bundleDirectory = Path.GetDirectoryName(bundleDirectory.TrimEnd('/')); // "Contents"
                    bundleDirectory = Path.GetDirectoryName(bundleDirectory); // "Ambermoon.net.app"
                    bundleDirectory = Path.GetDirectoryName(bundleDirectory); // folder which contains the bundle

                    return bundleDirectory;
                }
                catch
                {
                    return ExecutableDirectoryPath;
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

                // Ticks of last saving, version index
                Tuple<long, int> mostRecentSavegameSlot = Tuple.Create(0L, -1);

                try
                {
                    var savegameSlots = configuration.AdditionalSavegameSlots = new AdditionalSavegameSlots[VersionSavegameFolders.Length];
                    int versionIndex = 0;

                    foreach (var savegameFolder in VersionSavegameFolders)
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

                                            if (lastWriteTicks > mostRecentSavegameSlot.Item1)
                                                mostRecentSavegameSlot = Tuple.Create(lastWriteTicks, versionIndex);

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

                    configuration.GameVersionIndex = Math.Max(0, mostRecentSavegameSlot.Item2);
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
#pragma warning restore CS0618

            return configuration;
        }

        public void Save(string filename)
        {
#pragma warning disable CS0618
            UseGraphicFilter = null; // not used anymore
            FastBattleMode = null; // not used anymore
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
