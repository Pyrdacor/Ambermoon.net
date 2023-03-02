using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    internal class Palette : IFileSpec
    {
        public string Magic => "PAL";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        Graphic? graphic = null;

        public Graphic Graphic => graphic!;

        public Palette()
        {

        }

        public Palette(Graphic graphic)
        {
            this.graphic = graphic;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            int paletteCount = dataReader.ReadWord();
            graphic = new Graphic();
            new GraphicReader().ReadGraphic(graphic, dataReader, new GraphicInfo
            {
                Width = 32,
                Height = paletteCount,
                GraphicFormat = GraphicFormat.RGBA32,
                Alpha = false
            });
        }

        public void Write(IDataWriter dataWriter)
        {
            if (graphic == null)
                throw new AmbermoonException(ExceptionScope.Application, "Palette data was null when trying to write it.");

            dataWriter.Write((ushort)graphic.Height);
            dataWriter.Write(graphic.Data);
        }
    }
}
