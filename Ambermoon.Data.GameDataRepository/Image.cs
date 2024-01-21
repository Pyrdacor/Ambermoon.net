namespace Ambermoon.Data.GameDataRepository
{
    public class Image
    {
        public Image(params Graphic[] frames)
        {
            Frames = new List<Graphic>(frames);
        }

        public List<Graphic> Frames { get; }
        public int Width => Frames.FirstOrDefault()?.Width ?? 0;
        public int Height => Frames.FirstOrDefault()?.Height ?? 0;
    }

    public class ImageWithPaletteIndex : Image
    {
        public ImageWithPaletteIndex(uint paletteIndex, params Graphic[] frames)
            : base(frames)
        {
            PaletteIndex = paletteIndex;
        }

        public uint PaletteIndex { get; }
    }
}
