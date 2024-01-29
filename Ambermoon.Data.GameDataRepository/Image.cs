using Ambermoon.Data.GameDataRepository.Data;
using Ambermoon.Data.GameDataRepository.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository
{
    public class ImageData : ICloneable
    {

        #region Fields

        private readonly byte[] _colorIndices;

        #endregion


        #region Properties

        public int Width { get; }

        public int Height { get; }

        #endregion


        #region Constructors

        private ImageData(int width, int height, byte[] colorIndices)
        {
            Width = width;
            Height = height;
            _colorIndices = (byte[])colorIndices.Clone();
        }

        internal ImageData(Graphic graphic)
        {
            if (graphic.Width % 16 != 0)
            {
                int addWidth = 16 - graphic.Width % 16;
                Width = graphic.Width + addWidth;
                Height = graphic.Height;
                _colorIndices = new byte[Width * Height];

                for (int y = 0; y < Height; y++)
                    Array.Copy(graphic.Data, y * graphic.Width, _colorIndices, y * Width, graphic.Width);
            }
            else
            {
                Width = graphic.Width;
                Height = graphic.Height;
                _colorIndices = (byte[])graphic.Data.Clone();
            }
        }

        internal ImageData(int width, int height, byte[] data, Palette palette)
        {
            if (width % 16 != 0)
                throw new ArgumentException("Graphic width must be a multiple of 16.");

            if (data.Length != width * height)
                throw new ArgumentException("Data length must match width and height.");

            Width = width;
            Height = height;
            _colorIndices = new byte[width * height];

            for (int i = 0; i < _colorIndices.Length; i++)
            {
                int colorIndex = palette.GetColorIndex(data, i * 4);

                if (colorIndex == -1)
                    throw new ArgumentException($"Color at (X:{i % Width}, Y:{i / Width}) not found in palette.");

                _colorIndices[i] = (byte)colorIndex;
            }
        }

        #endregion


        #region Methods

        /// <summary>
        /// This is stored as B8G8R8A8 data, which means
        /// first byte is blue, second green, third red and fourth alpha.
        /// This is compatible with little-endian Windows bitmap data.
        /// </summary>
        public byte[] GetData(Palette palette)
        {
            var data = new byte[_colorIndices.Length * 4];

            for (int i = 0; i < _colorIndices.Length; i++)
            {
                palette.CopyColor(data, i * 4, _colorIndices[i]);
            }

            return data;
        }

        public void ReplaceColors(params ImageColorReplacement[] colorReplacements)
        {
            var lookup = colorReplacements.ToDictionary(cr => cr.ColorIndexToReplace, cr => cr.NewColorIndex);

            for (int i = 0; i < _colorIndices.Length; i++)
            {
                if (lookup.TryGetValue(_colorIndices[i], out uint newColorIndex))
                    _colorIndices[i] = (byte)newColorIndex;
            }
        }

        #endregion


        #region Cloning

        public ImageData Copy()
        {
            return new ImageData(Width, Height, _colorIndices);
        }

        public object Clone() => Copy();

        #endregion

    }

    public class Image : IIndexed, IMutableIndex, ICloneable
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public List<ImageData> Frames { get; }

        public int Width => Frames.FirstOrDefault()?.Width ?? 0;

        public int Height => Frames.FirstOrDefault()?.Height ?? 0;

        #endregion


        #region Constructors

        internal Image(uint index, IEnumerable<Graphic> frames)
        {
            Frames = new List<ImageData>(frames.Select(frame => new ImageData(frame)));
            (this as IMutableIndex).Index = index;
        }

        public Image(uint index, params ImageData[] frames)
        {
            Frames = new List<ImageData>(frames);
            (this as IMutableIndex).Index = index;
        }

        public Image(uint index)
        {
            Frames = new List<ImageData>();
            (this as IMutableIndex).Index = index;
        }

        public Image()
        {
            Frames = new List<ImageData>();
        }

        #endregion


        #region Serialization

        public static Image Deserialize(uint index, IDataReader dataReader, int numFrames, int width, int height, GraphicFormat format,
            bool alpha = false, byte colorKey = 0, byte paletteOffset = 0)
        {
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo()
            {
                Alpha = alpha,
                ColorKey = colorKey,
                GraphicFormat = format,
                Width = width,
                Height = height,
                PaletteOffset = paletteOffset
            };
            var frames = new ImageData[numFrames];

            for (int i = 0; i < numFrames; ++i)
            {
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);
                frames[i] = new ImageData(graphic);
            }

            var image = new Image(index, frames);
            (image as IMutableIndex).Index = index;
            return image;;
        }

        public static Image DeserializeFullData(uint index, IDataReader dataReader, int width, int height, GraphicFormat format,
            bool alpha = false, byte colorKey = 0, byte paletteOffset = 0)
        {
            var graphicInfo = new GraphicInfo
            {
                GraphicFormat = format,
            };
            int frameSize = width * height * graphicInfo.BitsPerPixel / 8;
            int remainingSize = dataReader.Size - dataReader.Position;
            return Deserialize(index, dataReader, remainingSize / frameSize, width, height,
                format, alpha, colorKey, paletteOffset);
        }

        #endregion


        #region Cloning

        public Image Copy()
        {
            var copy = new Image(Index, Frames.Select(frame => frame.Copy()).ToArray());
            (copy as IMutableIndex).Index = Index;
            return copy;
        }

        public virtual object Clone() => Copy();

        #endregion

    }

    public class ImageWithPaletteIndex : Image
    {

        #region Properties

        public uint PaletteIndex { get; }

        #endregion


        #region Constructors

        public ImageWithPaletteIndex()
        {

        }

        internal ImageWithPaletteIndex(uint index, uint paletteIndex, IEnumerable<Graphic> frames)
            : base(index, frames)
        {
            PaletteIndex = paletteIndex;
        }

        public ImageWithPaletteIndex(uint index, uint paletteIndex, params ImageData[] frames)
            : base(index, frames)
        {
            PaletteIndex = paletteIndex;
        }

        #endregion


        #region Serialization

        public static ImageWithPaletteIndex Deserialize(uint index, uint paletteIndex, IDataReader dataReader, int numFrames,
            int width, int height, GraphicFormat format, bool alpha = false, byte colorKey = 0, byte paletteOffset = 0)
        {
            var image = Deserialize(index, dataReader, numFrames, width, height, format, alpha, colorKey, paletteOffset);
            var imageWithPaletteIndex = new ImageWithPaletteIndex(index, paletteIndex, image.Frames.ToArray());
            (imageWithPaletteIndex as IMutableIndex).Index = index;
            return imageWithPaletteIndex;
        }

        public static ImageWithPaletteIndex DeserializeFullData(uint index, uint paletteIndex, IDataReader dataReader,
            int width, int height, GraphicFormat format, bool alpha = false, byte colorKey = 0, byte paletteOffset = 0)
        {
            var image = DeserializeFullData(index, dataReader, width, height, format, alpha, colorKey, paletteOffset);
            var imageWithPaletteIndex = new ImageWithPaletteIndex(index, paletteIndex, image.Frames.ToArray());
            (imageWithPaletteIndex as IMutableIndex).Index = index;
            return imageWithPaletteIndex;
        }

        #endregion


        #region Cloning

        public new ImageWithPaletteIndex Copy()
        {
            var copy = new ImageWithPaletteIndex(Index, PaletteIndex, Frames.Select(frame => frame.Copy()).ToArray());
            (copy as IMutableIndex).Index = Index;
            return copy;
        }

        public override object Clone() => Copy();

        #endregion

    }

    public class CombatBackgroundImage : Image
    {

        #region Fields

        public uint[] PaletteIndices { get; } = new uint[3];

        #endregion


        #region Constructors

        public CombatBackgroundImage()
        {

        }

        internal CombatBackgroundImage(uint index, uint[] paletteIndices, Graphic graphic)
            : base(index, new[] { graphic })
        {
            if (paletteIndices.Length == 1)
            {
                for (int i = 0; i < 3; i++)
                    PaletteIndices[i] = paletteIndices[0];
            }
            else if (paletteIndices.Length == 3)
            {
                for (int i = 0; i < 3; i++)
                    PaletteIndices[i] = paletteIndices[i];
            }
            else
            {
                throw new ArgumentException("There must be exactly 1 or 3 palette indices.");
            }
        }

        public CombatBackgroundImage(uint index, uint[] paletteIndices, ImageData graphic)
            : base(index, graphic)
        {
            if (paletteIndices.Length == 1)
            {
                for (int i = 0; i < 3; i++)
                    PaletteIndices[i] = paletteIndices[0];
            }
            else if (paletteIndices.Length == 3)
            {
                for (int i = 0; i < 3; i++)
                    PaletteIndices[i] = paletteIndices[i];
            }
            else
            {
                throw new ArgumentException("There must be exactly 1 or 3 palette indices.");
            }
        }

        #endregion


        #region Methods

        public uint GetPaletteIndex(CombatBackgroundDaytime daytime)
        {
            return PaletteIndices[(int)daytime];
        }

        public uint GetPaletteIndex(uint ingameHour)
        {
            return GetPaletteIndex(ingameHour.HourToCombatBackgroundDaytime());
        }

        #endregion


        #region Serialization

        public static CombatBackgroundImage Deserialize(uint index, uint[] paletteIndices, IDataReader dataReader)
        {
            var image = DeserializeImage(index, dataReader);
            var combatBackgroundImage = new CombatBackgroundImage(index, paletteIndices, image.Frames[0]);
            (combatBackgroundImage as IMutableIndex).Index = index;
            return combatBackgroundImage;
        }

        public static Image DeserializeImage(uint index, IDataReader dataReader)
        {
            return Deserialize(index, dataReader, 1, 320, 95, GraphicFormat.Palette5Bit);
        }

        #endregion


        #region Cloning

        public new CombatBackgroundImage Copy()
        {
            var copy = new CombatBackgroundImage(Index, PaletteIndices, Frames[0].Copy());
            (copy as IMutableIndex).Index = Index;
            return copy;
        }

        public override object Clone() => Copy();

        #endregion

    }

    public class ImageColorReplacement
    {

        #region Properties

        public uint ColorIndexToReplace { get; }

        public uint NewColorIndex { get; }

        #endregion


        #region Constructors

        public ImageColorReplacement(uint colorIndexToReplace, uint newColorIndex)
        {
            ColorIndexToReplace = colorIndexToReplace;
            NewColorIndex = newColorIndex;
        }

        #endregion

    }

    public static class ImageExtensions
    {
        public static T WithColorReplacements<T>(this T image, params ImageColorReplacement[] colorReplacements)
            where T : Image, ICloneable
        {
            var clone = (T)image.Clone();

            foreach (var frame in clone.Frames)
            {
                frame.ReplaceColors(colorReplacements);
            }

            return clone;
        }
    }
}
