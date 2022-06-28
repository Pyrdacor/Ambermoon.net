using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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

                var bundleDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); // "MacOS"
                bundleDirectory = Path.GetDirectoryName(bundleDirectory.TrimEnd('/')); // "Contents"
                bundleDirectory = Path.GetDirectoryName(bundleDirectory); // "Ambermoon.net.app"
                bundleDirectory = Path.GetDirectoryName(bundleDirectory); // folder which contains the bundle

                return bundleDirectory;
            }
        }

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
                var assemblyPath = Process.GetCurrentProcess().MainModule.FileName;

#pragma warning disable IL3000
                if (assemblyPath.EndsWith("dotnet"))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().Location;
                }
#pragma warning restore IL3000

                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (OperatingSystem.IsWindows())
                {
                    if (assemblyDirectory.EndsWith("Debug") || assemblyDirectory.EndsWith("Release")
                         || assemblyDirectory.EndsWith("net6.0"))
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
