using System;

namespace Ambermoon.Data
{
    public class Automap
    {
        public byte[] ExplorationBits { get; set; }

        public bool IsBlockExplored(Map map, uint x, uint y)
        {
            return IsBlockExplored(x + y * (uint)map.Width);
        }

        public bool IsBlockExplored(uint blockIndex)
        {
            int byteIndex = (int)blockIndex / 8;

            if (byteIndex >= ExplorationBits.Length)
                throw new AmbermoonException(ExceptionScope.Data, "Block index exceeds automap size.");

            int bitIndex = (int)blockIndex % 8;

            return (ExplorationBits[byteIndex] & (1 << bitIndex)) != 0;
        }

        public void ExploreBlock(Map map, uint x, uint y)
        {
            ExploreBlock(x + y * (uint)map.Width);
        }

        public void ExploreBlock(uint blockIndex)
        {
            int byteIndex = (int)blockIndex / 8;

            if (byteIndex >= ExplorationBits.Length)
                throw new AmbermoonException(ExceptionScope.Data, "Block index exceeds automap size.");

            int bitIndex = (int)blockIndex % 8;

            ExplorationBits[byteIndex] |= (byte)(1 << bitIndex);
        }

        public void ResetExploration()
        {
            Array.Fill(ExplorationBits, (byte)0);
        }
    }
}
