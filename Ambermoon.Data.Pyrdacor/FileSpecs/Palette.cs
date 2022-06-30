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
        const int PaletteCount = 69;

        static readonly GraphicInfo PaletteGraphicInfo = new GraphicInfo
        {
            Width = 32,
            Height = PaletteCount,
            GraphicFormat = GraphicFormat.RGBA32,
            Alpha = false
        };

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
            graphic = new Graphic();
            new GraphicReader().ReadGraphic(graphic, dataReader, PaletteGraphicInfo);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (graphic == null)
                throw new AmbermoonException(ExceptionScope.Application, "Palette data was null when trying to write it.");

            dataWriter.Write(graphic.Data);
        }
    }
}
