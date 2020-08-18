namespace Ambermoon.Data.Legacy
{
    public class FontProvider : IFontProvider
    {
        public IFont GetFont()
        {
            return new Font();
        }
    }
}
