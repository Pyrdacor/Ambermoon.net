using System;
using System.Collections;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Repository.Util
{
    public class TwoDimensionalData<TElement> : IEnumerable, IEnumerable<TElement>
    {
        private TElement[] _elements;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Count => _elements.Length;

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

        public IEnumerator<TElement> GetEnumerator() => (IEnumerator<TElement>)_elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        public TElement this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }
    }
}
