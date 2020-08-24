namespace Ambermoon.Data.Legacy
{
    public class FontProvider : IFontProvider
    {
        readonly ExecutableData.ExecutableData executableData;

        public FontProvider(ExecutableData.ExecutableData executableData)
        {
            this.executableData = executableData;
        }

        public IFont GetFont()
        {
            return new Font(executableData.Glyphs);
        }
    }
}
