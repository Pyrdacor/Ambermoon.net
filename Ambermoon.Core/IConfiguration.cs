namespace Ambermoon
{
    public enum ScreenRatio
    {
        Ratio4_3,
        Ratio16_9,
        Ratio16_10
    }

    public enum SaveOption
    {
        ProgramFolder,
        DataFolder
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
        bool FastBattles { get; set; }
    }

    public static class ConfigurationExtensions
    {
        public static Size GetScreenSize(this IConfiguration configuration)
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
    }
}
