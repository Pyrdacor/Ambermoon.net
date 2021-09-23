using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class TextDictionaryWriter
    {
        public static void WriteTextDictionary(TextDictionary textDictionary, IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)textDictionary.Entries.Count);

            foreach (var entry in textDictionary.Entries)
                dataWriter.Write(entry);
        }
    }
}
