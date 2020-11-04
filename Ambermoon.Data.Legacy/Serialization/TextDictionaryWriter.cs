using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextDictionaryWriter
    {
        public void WriteTextDictionary(TextDictionary textDictionary, IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)textDictionary.Entries.Count);

            foreach (var entry in textDictionary.Entries)
                dataWriter.Write(entry);
        }
    }
}
