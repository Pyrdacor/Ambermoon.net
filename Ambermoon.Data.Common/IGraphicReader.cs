namespace Ambermoon.Data
{
    public interface IGraphicReader
    {
        void ReadGraphic(Graphic graphic, IDataReader dataReader, GraphicInfo? graphicInfo);
    }
}
