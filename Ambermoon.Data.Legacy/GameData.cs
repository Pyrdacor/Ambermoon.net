using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
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
        public Dictionary<StationaryImage, GraphicInfo> StationaryImageInfos { get; } = new Dictionary<StationaryImage, GraphicInfo>
        {
            { StationaryImage.Horse, new GraphicInfo { Width = 32, Height = 22, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { StationaryImage.Raft, new GraphicInfo { Width = 32, Height = 11, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { StationaryImage.Boat, new GraphicInfo { Width = 48, Height = 34, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { StationaryImage.SandLizard, new GraphicInfo { Width = 48, Height = 21, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } },
            { StationaryImage.SandShip, new GraphicInfo { Width = 48, Height = 39, GraphicFormat = GraphicFormat.Palette5Bit, Alpha = true } }
        };
        private readonly Dictionary<char, Dictionary<string, byte[]>> loadedDisks = new Dictionary<char, Dictionary<string, byte[]>>();
        private readonly LoadPreference loadPreference;
        private readonly ILogger log;
        private readonly bool stopAtFirstError;
        private readonly List<TravelGraphicInfo> travelGraphicInfos = new List<TravelGraphicInfo>(44);
        internal List<Graphic> TravelGraphics { get; } = new List<Graphic>(44);

        public GameData(LoadPreference loadPreference = LoadPreference.PreferExtracted, ILogger logger = null, bool stopAtFirstError = true)
        {
            this.loadPreference = loadPreference;
            log = logger;
            this.stopAtFirstError = stopAtFirstError;
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
                if (log != null)
                    log.AppendLine("succeeded");

                if (IsDictionary(file))
                {
                    Dictionaries.Add(file.Split('.').Last(), Files[file].Files[1]);
                    foundNoDictionary = false;
                }
            }

            void HandleFileNotFound(string file)
            {
                if (log != null)
                {
                    log.AppendLine("failed");
                    log.AppendLine($" -> Unable to find file '{file}'.");
                }

                // We only need 1 dictionary, no savegames and only AM2_CPU but not AM2_BLIT.
                if (IsDictionary(file) || IsSavegame(file) || file == "AM2_BLIT")
                    return;

                if (stopAtFirstError)
                    throw new FileNotFoundException($"Unable to find file '{file}'.");
            }

            foreach (var ambermoonFile in ambermoonFiles)
            {
                var name = ambermoonFile.Key;
                var path = Path.Combine(folderPath, name.Replace('/', Path.DirectorySeparatorChar));

                if (log != null)
                    log.Append($"Trying to load file '{name}' ... ");

                // prefer direct files but also allow loading ADF disks
                if (loadPreference == LoadPreference.PreferExtracted && File.Exists(path))
                {
                    Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                    HandleFileLoaded(name);
                }
                else if (loadPreference == LoadPreference.ForceExtracted)
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

                    if (!loadedDisks.ContainsKey(disk))
                    {
                        string diskFile = FindDiskFile(folderPath, disk);

                        if (diskFile == null)
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
                                if (!File.Exists(path))
                                {
                                    HandleFileNotFound(name);
                                }
                                else
                                {
                                    Files.Add(name, fileReader.ReadFile(name, File.OpenRead(path)));
                                    HandleFileLoaded(name);
                                }
                            }

                            continue;
                        }

                        loadedDisks.Add(disk, ADFReader.ReadADF(File.OpenRead(diskFile)));
                    }

                    if (!loadedDisks[disk].ContainsKey(name))
                        HandleFileNotFound(name);
                    else
                    {
                        Files.Add(name, fileReader.ReadFile(name, loadedDisks[disk][name]));
                        HandleFileLoaded(name);
                    }
                }

            }

            if (foundNoDictionary)
            {
                throw new FileNotFoundException($"Unable to find any dictionary file.");
            }

            LoadTravelGraphics();
        }

        void LoadTravelGraphics()
        {
            // Travel gfx stores graphics with a header:
            // uword NumberOfHorizontalSprites (a sprite has a width of 16 pixels)
            // uword Height (in pixels)
            // uword XOffset (in pixels relative to drawing position)
            // uword YOffset (in pixels relative to drawing position)
            var container = Files["Travel_gfx.amb"];
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
            TicksPerFrame = 0
        };
    }
}
