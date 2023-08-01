using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class SkyPart
    {
        public int Y { get; set; }
        public int Height { get; set; }
        public uint Color { get; set; }
    }

    public class PaletteReplacement
    {
        public byte[] ColorData { get; } = new byte[16 * 4];
    }

    public class PaletteFading
    {
        public byte SourcePalette { get; set; }
        public byte DestinationPalette { get; set; }
        public float SourceFactor { get; set; }
    }

    public interface ILightEffectProvider
    {
        IEnumerable<SkyPart> GetSkyParts(Map map, uint hour, uint minute,
            IGraphicProvider graphicProvider);
        PaletteReplacement GetLightPaletteReplacement(Map map, uint hour, uint minute,
            uint buffLightIntensity, IGraphicProvider graphicProvider);
    }
}
