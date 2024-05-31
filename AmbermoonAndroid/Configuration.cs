using Ambermoon;
using Newtonsoft.Json;
using System.Globalization;

namespace AmbermoonAndroid
{
    internal class Configuration : IConfiguration
    {
		internal const string ConfigurationFileName = "ambermoon.cfg";
		internal const string ExternalSavegameFolder = "external";
		internal static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Ambermoon");

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
        public bool IsMobile { get; } = true;

		public bool? UsePatcher { get; set; } = false;
		public int? PatcherTimeout { get; set; } = null;

		[JsonIgnore] // not needed/supported on Android
		public int? WindowX { get; set; } = null;
		[JsonIgnore] // not needed/supported on Android
		public int? WindowY { get; set; } = null;
		[JsonIgnore] // not needed/supported on Android
		public int? MonitorIndex { get; set; } = null;
		public int? Width { get; set; } = null;
        public int? Height { get; set; } = null;
        public int? FullscreenWidth { get; set; } = null;
        public int? FullscreenHeight { get; set; } = null;
		[JsonIgnore] // not needed/supported on Android
		public bool Fullscreen { get; set; } = true;
		[JsonIgnore] // not needed/supported on Android
		public bool UseDataPath { get; set; } = false;
		[JsonIgnore] // not needed/supported on Android
		public string DataPath { get; set; } = "";
		[JsonIgnore] // not needed/supported on Android
		public SaveOption SaveOption { get; set; } = SaveOption.ProgramFolder;
        public int GameVersionIndex { get; set; } = -1;
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
		[JsonIgnore] // not needed/supported on Android
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

		public void RequestSave() => SaveRequested?.Invoke();

		public static string GetSavePath(string version, bool createIfMissing = true)
		{
			string suffix = $"Saves{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";
			string alternativeSuffix = $"SavesRemake{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";

			var path = Path.Combine(AppDataPath, suffix);

			if (createIfMissing)
			{
				try
				{
					Directory.CreateDirectory(path);
				}
				catch
				{
					path = Path.Combine(AppDataPath, alternativeSuffix);
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

        public static Configuration Load(Configuration defaultValue = null)
        {
			var filename = Path.Combine(AppDataPath, ConfigurationFileName);

            if (!File.Exists(filename) || new FileInfo(filename).Length == 0)
                return defaultValue;

            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }

        public void Save()
        {
			var filename = Path.Combine(AppDataPath, ConfigurationFileName);

			Directory.CreateDirectory(AppDataPath);
            File.WriteAllText(filename, JsonConvert.SerializeObject(this,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                })
            );
        }

		public AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots(string gameVersionName)
		{
			gameVersionName = gameVersionName.ToLower();

			AdditionalSavegameSlots ??= GetAllPossibleSavegameFolders().Select(f => new AdditionalSavegameSlots
			{
				GameVersionName = f,
				ContinueSavegameSlot = 0,
				Names = new string[Game.NumAdditionalSavegameSlots]
			}).ToArray();

			var savegameSlots = AdditionalSavegameSlots.FirstOrDefault(s => s.GameVersionName.ToLower() == gameVersionName);

			if (savegameSlots == null)
			{
				savegameSlots = new AdditionalSavegameSlots
				{
					GameVersionName = gameVersionName,
					ContinueSavegameSlot = 0,
					Names = new string[Game.NumAdditionalSavegameSlots]
				};

				AdditionalSavegameSlots = Enumerable.Concat(AdditionalSavegameSlots, new[] { savegameSlots }).ToArray();
			}

			return savegameSlots;
		}
	}
}
