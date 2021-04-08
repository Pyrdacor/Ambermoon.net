namespace Ambermoon.Data.Serialization
{
    public interface IGraphicReader
    {
        void ReadGraphic(Graphic graphic, IDataReader dataReader, GraphicInfo? graphicInfo, byte maskColor = 0);
    }
}
