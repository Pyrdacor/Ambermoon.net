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
        public bool FastBattleMode { get; set; } = false;
        public bool CacheMusic { get; set; } = true;
        public bool AutoDerune { get; set; } = true;
        public bool EnableCheats { get; set; } = false;
        public bool ShowButtonTooltips { get; set; } = true;
        [JsonIgnore] // TODO: remove attribute later
        public bool ShowFantasyIntro { get; set; } = false; // TODO: change to true later
        [JsonIgnore] // TODO: remove attribute later
        public bool ShowIntro { get; set; } = false; // TODO: change to true later
        public bool UseGraphicFilter { get; set; } = true;
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
                GameVersionIndex = 0;

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

        public static string ExecutableDirectoryPath
        {
            get
            {
                bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

                var assemblyPath = Process.GetCurrentProcess().MainModule.FileName;

#pragma warning disable IL3000
                if (assemblyPath.EndsWith("dotnet"))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().Location;
                }
#pragma warning restore IL3000

                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (isWindows)
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

            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }

        public void Save(string filename)
        {
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
