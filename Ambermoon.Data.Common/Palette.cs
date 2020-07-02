namespace Ambermoon.Data
{
    public class Palette
    {
        private readonly Graphic _graphic;

        public Palette(Graphic graphic)
        {
            _graphic = graphic;
        }

        public void Fill(byte[] graphicData, int offset, int paletteIndex)
        {
            /*if (paletteIndex == 0) // TODO: REMOVE
            {
                graphicData[offset + 0] = 0;
                graphicData[offset + 1] = 0;
                graphicData[offset + 2] = 0;
                graphicData[offset + 3] = 255;
                return;
            }*/

            graphicData[offset + 0] = _graphic.Data[paletteIndex * 4 + 0];
            graphicData[offset + 1] = _graphic.Data[paletteIndex * 4 + 1];
            graphicData[offset + 2] = _graphic.Data[paletteIndex * 4 + 2];
            graphicData[offset + 3] = _graphic.Data[paletteIndex * 4 + 3];
        }
    }
}
