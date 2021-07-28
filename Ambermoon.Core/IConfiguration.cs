using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    public enum SaveOption
    {
        ProgramFolder,
        DataFolder
    }

    public static class ScreenResolutions
    {
        public static List<Size> GetPossibleResolutions(Size maxSize)
        {
            var resolutions = new List<Size>(4);

            // 4/8, 5/8, 6/8 and 7/8 of max size
            for (int i = 0; i < 4; ++i)
            {
                int width = maxSize.Width * (4 + i) / 8;
                resolutions.Add(new Size(width, width * 10 / 16));
            }

            return resolutions;
        }
    }

    public interface IConfiguration
    {
        bool FirstStart { get; set; }

        int? Width { get; set; }
        int? Height { get; set; }
        int? FullscreenWidth { get; set; }
        int? FullscreenHeight { get; set; }
        bool Fullscreen { get; set; }
        bool UseDataPath { get; set; }
        string DataPath { get; set; }
        SaveOption SaveOption { get; set; }
        int GameVersionIndex { get; set; }
        bool LegacyMode { get; set; }
        bool Music { get; set; }
        int Volume { get; set; }
        bool FastBattleMode { get; set; }
        bool CacheMusic { get; set; }
        bool EnableCheats { get; set; }
        bool ShowButtonTooltips { get; set; }
        bool ShowFantasyIntro { get; set; }
        bool ShowIntro { get; set; }
    }

    public static class ConfigurationExtensions
    {
        public static Size GetScreenResolution(this IConfiguration configuration)
        {
            int? width = configuration.Width;
            int? height = configuration.Height;

            if (width == null && height == null)
                width = 1280;

            if (width != null)
            {
                height = width * 10 / 16;
            }
            else
            {
                width = height * 16 / 10;
            }

            return new Size(width.Value, height.Value);
        }

        public static Size GetScreenSize(this IConfiguration configuration)
        {
            int? width = configuration.Width;
            int? height = configuration.Height;

            if (width != null && height != null)
                return new Size(width.Value, height.Value);

            return GetScreenResolution(configuration);
        }
    }
}
