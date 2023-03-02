using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects
{
    internal class TextList
    {
        readonly List<string> texts;

        public TextList(IDataReader dataReader)
        {
            int count = dataReader.ReadWord();

            texts = new List<string>(count);

            for (int i = 0; i < count; ++i)
                texts.Add(dataReader.ReadString());
        }

        public string? GetText(int index) => index >= texts.Count ? null : texts[index];

        public void Write(IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)texts.Count);

            foreach (var text in texts)
                dataWriter.Write(text);
        }
    }
}
