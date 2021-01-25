using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public static class TextWriter
    {
        public static void Write(BinaryWriter writer, List<string> texts)
        {
            writer.Write((uint)texts.Count);
        }
    }
}
