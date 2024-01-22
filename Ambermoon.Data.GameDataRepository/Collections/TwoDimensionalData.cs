using System.Collections;

namespace Ambermoon.Data.GameDataRepository.Collections
{
    public class TwoDimensionalData<TElement> : IEnumerable<TElement>, ICloneable
        where TElement : ICloneable
    {

        #region Fields

        private TElement[] _elements;

        #endregion


        #region Properties

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Count => _elements.Length;

        #endregion


        #region Indexers

        public TElement this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        public TElement this[int x, int y]
        {
            get => Get(x, y);
            set => Set(x, y, value);
        }

        #endregion


        #region Constructors

        public TwoDimensionalData()
        {
            _elements = Array.Empty<TElement>();
        }

        public TwoDimensionalData(int width, int height)
        {
            _elements = new TElement[width * height];
            Width = width;
            Height = height;
        }

        #endregion


        #region Methods

        public void Resize(int width, int height, Func<TElement> defaultValueProvider)
        {
            int xDiff = width - Width;
            int yDiff = height - Height;

            if (xDiff == 0 && yDiff == 0)
                return; // no change

            var newElements = new TElement[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var element = x < Width && y < Height ? _elements[y * width + x] : defaultValueProvider();
                    newElements[y * width + x] = element;
                }
            }

            _elements = newElements;
            Width = width;
            Height = height;
        }

        public TElement Get(int x, int y) => _elements[y * Width + x];

        public void Set(int x, int y, TElement element) => _elements[y * Width + x] = element;

        public IEnumerator<TElement> GetEnumerator() => ((IEnumerable<TElement>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        #endregion


        #region Cloning

        public TwoDimensionalData<TElement> Copy(bool cloneElements = true)
        {
            var clone = new TwoDimensionalData<TElement>(Width, Height);

            for (int i = 0; i < _elements.Length; ++i)
                clone._elements[i] = cloneElements ? (TElement)_elements[i].Clone() : _elements[i];

            return clone;
        }

        public object Clone() => Copy();

        #endregion

    }
}
