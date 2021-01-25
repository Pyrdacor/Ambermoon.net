using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Pyrdacor.Serialization.Compression
{
    static class RLE0
    {
        public static byte[] Compress(byte[] data)
        {
            List<byte> result = new List<byte>(data.Length);
            int rleCount = -1;

            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0)
                {
                    if (++rleCount == 255)
                    {
                        result.Add(0);
                        result.Add(255);
                        rleCount = -1;
                    }
                }
                else
                {
                    if (rleCount >= 0)
                    {
                        result.Add(0);
                        result.Add((byte)rleCount);
                        rleCount = -1;
                    }

                    result.Add(data[i]);
                }
            }

            if (rleCount >= 0)
            {
                result.Add(0);
                result.Add((byte)rleCount);
            }

            return result.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            List<byte> result = new List<byte>(data.Length);

            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0)
                {
                    int numBytes = 1 + data[++i];
                    result.Capacity = Math.Max(result.Capacity, result.Count + numBytes);
                    for (int j = 0; j < numBytes; ++j)
                        result.Add(0);
                }
                else
                {
                    result.Add(data[i]);
                }
            }

            return result.ToArray();
        }
    }
}
