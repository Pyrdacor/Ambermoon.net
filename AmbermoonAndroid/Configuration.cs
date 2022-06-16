using Ambermoon;
using Newtonsoft.Json;

namespace AmbermoonAndroid
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
        public bool IsMobile { get; } = true;

        public int? Width { get; set; } = null;
        public int? Height { get; set; } = null;
        public int? FullscreenWidth { get; set; } = null;
        public int? FullscreenHeight { get; set; } = null;
        public bool Fullscreen { get; set; } = false;
        public bool UseDataPath { get; set; } = false;
        public string DataPath { get; set; } = ExecutableDirectoryPath;
        public SaveOption SaveOption { get; set; } = SaveOption.ProgramFolder;
        public int GameVersionIndex { get; set; } = 0;
        public bool LegacyMode { get; set; } = false;
        public bool Music { get; set; } = true;
        public int Volume { get; set; } = 100;
        [JsonIgnore] // not needed/supported on Android
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
            if (AdditionalSavegameSlots is not null)
                return;

            AdditionalSavegameSlots = VersionSavegameFolders.Select(f => new AdditionalSavegameSlots
            {
                GameVersionName = f,
                ContinueSavegameSlot = 0,
                Names = new string[Game.NumAdditionalSavegameSlots]
            }).ToArray();

            // Copy old savegame names to new format
            if (AdditionalSavegameNames is not null && GameVersionIndex >= 0 && GameVersionIndex < 3)
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

            if (AdditionalSavegameSlots is null)
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

        public static string ExecutableDirectoryPath => FallbackConfigDirectory;

        public static Configuration Load(string filename, Configuration defaultValue = null)
        {
            if (!File.Exists(filename))
                return defaultValue;

            // TODO: ReadAllText on android possible?
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
