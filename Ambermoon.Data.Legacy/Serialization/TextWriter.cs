using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextWriter
    {
        public static void WriteTexts(IDataWriter textDataWriter, List<string> texts)
        {
            if (texts == null)
            {
                textDataWriter.Write((ushort)0);
                return;
            }

            textDataWriter.Write((ushort)texts.Count);

            texts.ForEach(text => textDataWriter.Write((ushort)(text.Length + 1)));
            texts.ForEach(text => textDataWriter.WriteWithoutLength(text + "\0"));
        }
    }
}
