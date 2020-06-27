namespace Ambermoon.Data
{
    public enum GraphicFormat
    {
        Palette5Bit,
        Texture4Bit,
        XRGB16,
        RGBA32,
    }

    public struct GraphicInfo
    {
        public GraphicFormat GraphicFormat;
        public int Width;
        public int Height;
        public Palette Palette;
    }

    public class Graphic
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; }
    }
}
