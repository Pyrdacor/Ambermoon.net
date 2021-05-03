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

    public interface ILightEffectProvider
    {
        IEnumerable<SkyPart> GetSkyParts(Map map, uint hour, uint minute,
            IGraphicProvider graphicProvider, out PaletteReplacement paletteReplacement);
    }
}
