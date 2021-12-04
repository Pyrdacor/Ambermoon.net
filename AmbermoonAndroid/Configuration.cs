using Ambermoon;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AmbermoonAndroid
{
    internal class Configuration : IConfiguration
    {
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
        public int GameVersionIndex { get; set; } = 0;
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
        public bool ExtendedSavegameSlots { get; set; } = true;
        public string[] AdditionalSavegameNames { get; set; } = new string[20];
        public int ContinueSavegameSlot { get; set; } = 0;

        public static readonly string FallbackConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ambermoon");

        public static string ExecutableDirectoryPath => FallbackConfigDirectory;

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
