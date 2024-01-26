using System.ComponentModel;

namespace Ambermoon.Data.GameDataRepository.Enumerations
{
    public enum Daytime
    {
        /// <summary>
        /// 20:00 to 05:59
        /// </summary>
        Night,
        /// <summary>
        /// 06:00 to 07:59
        /// </summary>
        Dusk,
        /// <summary>
        /// 08:00 to 16:59
        /// </summary>
        Day,
        /// <summary>
        /// 17:00 to 19:59
        /// </summary>
        Dawn
    }

    public enum CombatBackgroundDaytime
    {
        /// <summary>
        /// 07:00 to 18:59
        /// </summary>
        Day,
        /// <summary>
        /// 05:00 to 06:59 and 19:00 to 20:59
        /// </summary>
        Twilight,
        /// <summary>
        /// 21:00 to 4:59
        /// </summary>
        Night
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DaytimeExtensions
    {
        public static Daytime HourToDaytime(this uint hour) => hour switch
        {
            6 or 7 => Daytime.Dusk,
            >= 20 or <= 5 => Daytime.Night,
            >= 17 => Daytime.Dawn,
            _ => Daytime.Day
        };

        public static CombatBackgroundDaytime HourToCombatBackgroundDaytime(this uint hour) => hour switch
        {
            >= 7 and <= 18 => CombatBackgroundDaytime.Day,
            >= 21 or <= 4 => CombatBackgroundDaytime.Night,
            _ => CombatBackgroundDaytime.Twilight
        };
    }
}
