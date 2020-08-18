namespace Ambermoon.Data
{
    public struct CombatBackgroundInfo
    {
        public uint GraphicIndex;
        /// <summary>
        /// 3 palettes for daylight (07:00-18:59),
        /// twilight (05:00-06:59, 19:00-20:59)
        /// and night (21:00-04:59) in that order.
        /// </summary>
        public uint[] Palettes;
    }
}
