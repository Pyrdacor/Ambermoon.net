using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class SkyPart
    {
        public int Y { get; set; }
        public int Height { get; set; }
        public uint Color { get; set; }
    }

    public interface ISkyProvider
    {
        IEnumerable<SkyPart> GetSkyParts(Map map, uint hour, uint minute);
    }
}
