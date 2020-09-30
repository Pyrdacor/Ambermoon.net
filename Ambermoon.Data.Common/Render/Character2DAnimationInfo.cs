namespace Ambermoon.Render
{
    public struct Character2DAnimationInfo
    {
        public int FrameWidth;
        public int FrameHeight;
        public uint StandFrameIndex;
        public uint SitFrameIndex;
        public uint SleepFrameIndex;
        public uint NumStandFrames;
        public uint NumSitFrames;
        public uint NumSleepFrames;
        public uint TicksPerFrame;
        public bool NoDirections;
    }
}
