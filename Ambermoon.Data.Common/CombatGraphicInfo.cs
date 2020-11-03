namespace Ambermoon.Data
{
    public struct CombatGraphicInfo
    {
        public uint FrameCount;
        public GraphicInfo GraphicInfo;
        public uint Palette;

        public CombatGraphicInfo(uint frames, int width, int height, uint palette = 18, bool ui = false)
        {
            FrameCount = frames;
            GraphicInfo = new GraphicInfo
            {
                GraphicFormat = ui ? GraphicFormat.Palette3Bit : GraphicFormat.Palette5Bit,
                Width = width,
                Height = height,
                Alpha = true,
                PaletteOffset = (byte)(ui ? 24 : 0)
            };
            Palette = palette;
        }
    }
}
