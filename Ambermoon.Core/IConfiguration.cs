using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    public enum ScreenRatio
    {
        Ratio16_10, // same as original (320x200)
        Ratio16_9,
        Ratio4_3,
    }

    public enum SaveOption
    {
        ProgramFolder,
        DataFolder
    }

    public static class ScreenResolutions
    {
        static readonly Dictionary<ScreenRatio, int[]> screenWidths = new Dictionary<ScreenRatio, int[]>
        {
            {
                ScreenRatio.Ratio16_10, new int[]
                {
                    1280,
                    1440,
                    1920,
                    2048,
                    2560,
                    320,
                    640
                }
            },
            {
                ScreenRatio.Ratio16_9, new int[]
                {
                    1280,
                    1600,
                    1920,
                    2048,
                    2560,
                    640
                }
            },
            {
                ScreenRatio.Ratio4_3, new int[]
                {
                    1280,
                    320,
                    640,
                    800,
                    1024
                }
            }
        };

        public static List<Size> Filter(ScreenRatio screenRatio, List<Size> resolutions)
        {
            float ratio = screenRatio switch
            {
                ScreenRatio.Ratio16_10 => 16.0f / 10.0f,
                ScreenRatio.Ratio16_9 => 16.0f / 9.0f,
                ScreenRatio.Ratio4_3 => 4.0f / 3.0f,
                _ => 16.0f / 10.0f
            };
            return resolutions.Where(res => Util.FloatEqual((float)res.Width / res.Height, ratio)).ToList();
        }

        public static List<Size> GetPossibleResolutions(ScreenRatio screenRatio, Size maxSize)
        {
            var widths = screenWidths[screenRatio];
            var resolutions = new List<Size>(widths.Length);
            float ratio = screenRatio switch
            {
                ScreenRatio.Ratio16_10 => 10.0f / 16.0f,
                ScreenRatio.Ratio16_9 => 9.0f / 16.0f,
                ScreenRatio.Ratio4_3 => 3.0f / 4.0f,
                _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid screen ratio")
            };

            foreach (var width in widths)
            {
                if (width > maxSize.Width)
                    continue;

                int height = Util.Round(ratio * width);

                if (height <= maxSize.Height)
                {
                    resolutions.Add(new Size(width, height));
                }
            }

            if (resolutions.Count == 0)
                resolutions.Add(new Size(640, Util.Round(ratio * 640)));

            return resolutions;
        }
    }

    public interface IConfiguration
    {
        ScreenRatio ScreenRatio { get; set; }
        int? Width { get; set; }
        int? Height { get; set; }
        bool Fullscreen { get; set; }
        bool UseDataPath { get; set; }
        string DataPath { get; set; }
        SaveOption SaveOption { get; set; }
        int GameVersionIndex { get; set; }
        bool LegacyMode { get; set; }
        bool Music { get; set; }
        bool FastBattleMode { get; set; }
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
                switch (configuration.ScreenRatio)
                {
                    default:
                    case ScreenRatio.Ratio4_3:
                        height = width * 3 / 4;
                        break;
                    case ScreenRatio.Ratio16_9:
                        height = width * 9 / 16;
                        break;
                    case ScreenRatio.Ratio16_10:
                        height = width * 10 / 16;
                        break;
                }
            }
            else
            {
                switch (configuration.ScreenRatio)
                {
                    default:
                    case ScreenRatio.Ratio4_3:
                        width = height * 4 / 3;
                        break;
                    case ScreenRatio.Ratio16_9:
                        width = height * 16 / 9;
                        break;
                    case ScreenRatio.Ratio16_10:
                        width = height * 16 / 10;
                        break;
                }
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
