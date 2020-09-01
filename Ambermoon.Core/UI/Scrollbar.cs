using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;

namespace Ambermoon.UI
{
    internal class Scrollbar
    {
        Position position;
        readonly Rect scrollArea;
        readonly bool vertical;
        readonly int scrollRange;
        readonly int barSize;
        readonly ILayerSprite sprite;
        readonly ScrollbarType baseType; // the highlighted one is always 1 above
        public bool Scrolling { get; private set; } = false;
        int? scrollStartPosition = null;
        Rect BarArea => new Rect(position, new Size(vertical ? scrollArea.Size.Width : barSize, vertical ? barSize : scrollArea.Size.Height));
        public event Action<int> Scrolled;
        public int ScrollOffset { get; private set; } = 0;

        public Scrollbar(Layout layout, ScrollbarType type, Rect scrollArea, int width, int height, int scrollRange)
        {
            this.scrollArea = scrollArea;
            vertical = type == ScrollbarType.SmallVertical || type == ScrollbarType.LargeVertical; // TODO: are there even horizontal ones?
            this.scrollRange = scrollRange;
            barSize = vertical ? height : width;
            position = new Position(scrollArea.Position);
            baseType = type;

            // We add 1 to height because there is 1 pixel row for a shadow.
            sprite = layout.RenderView.SpriteFactory.Create(width, height + 1, 0, 0, false, true) as ILayerSprite;
            sprite.Layer = layout.RenderView.GetLayer(Layer.UIBackground);
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground).GetOffset(Graphics.ScrollbarOffset + (uint)type);
            sprite.PaletteIndex = 49;
            sprite.X = position.X;
            sprite.Y = position.Y;
            sprite.Visible = true;
        }

        void SetBarPosition(Position position)
        {
            if (this.position == position)
                return;

            this.position = position;

            sprite.X = vertical ? position.X : Util.Limit(scrollArea.Left, position.X, scrollArea.Right - barSize);
            sprite.Y = vertical ? Util.Limit(scrollArea.Top, position.Y, scrollArea.Bottom - barSize) : position.Y;
        }

        public void SetScrollPosition(int position)
        {
            ScrollOffset = position;

            if (vertical)
            {
                SetBarPosition(new Position(this.position.X, scrollArea.Top + Util.Round((float)position * (scrollArea.Size.Height - barSize) / scrollRange)));
            }
            else // horizontal
            {
                SetBarPosition(new Position(scrollArea.Left + Util.Round((float)position * (scrollArea.Size.Width - barSize) / scrollRange), this.position.Y));
            }
        }

        public void Destroy()
        {
            sprite?.Delete();
        }

        public bool Drag(Position position)
        {
            if (!Scrolling)
                return false;

            if (vertical)
            {
                if (position.Y <= scrollArea.Top + barSize / 2)
                    SetScrollPosition(0);
                else if (position.Y >= scrollArea.Bottom - barSize / 2)
                    SetScrollPosition(scrollRange);
                else
                {
                    // There are (n + 1) areas where n is the scroll range.
                    // But they start at half bar size from both ends.
                    // The bar's center will jump to the center of an area
                    // if the cursor is closest to this center.
                    // The correct area can be calculated by dividing the
                    // cursor position by the area size.
                    int areaSize = (scrollArea.Size.Height - barSize) / (scrollRange + 1);
                    int newPosition = Math.Min(scrollRange, (position.Y - (scrollArea.Top + barSize / 2)) / areaSize);
                    if (newPosition != ScrollOffset)
                    {
                        ScrollOffset = newPosition;
                        Scrolled?.Invoke(newPosition);
                    }
                    SetBarPosition(new Position(this.position.X, position.Y - barSize / 2));
                }
            }
            else // horizontal
            {
                if (position.X <= scrollArea.Left)
                    SetScrollPosition(0);
                else if (position.X >= scrollArea.Right)
                    SetScrollPosition(scrollRange);
                else
                {
                    int areaSize = (scrollArea.Size.Width - barSize) / (scrollRange + 1);
                    int newPosition = Math.Min(scrollRange, (position.X - (scrollArea.Left + +barSize / 2)) / areaSize);
                    if (newPosition != ScrollOffset)
                    {
                        ScrollOffset = newPosition;
                        Scrolled?.Invoke(newPosition);
                    }
                    SetBarPosition(new Position(position.X - barSize / 2, this.position.Y));
                }
            }

            return true;
        }

        public void LeftMouseUp()
        {
            if (Scrolling)
            {
                sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground).GetOffset(Graphics.ScrollbarOffset + (uint)baseType);
                Scrolling = false;

                if (scrollStartPosition != ScrollOffset)
                    Scrolled?.Invoke(ScrollOffset);
            }

            scrollStartPosition = null;
        }

        public bool LeftClick(Position position)
        {
            if (!BarArea.Contains(position))
                return false;

            Scrolling = true;
            scrollStartPosition = ScrollOffset;
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground).GetOffset(Graphics.ScrollbarOffset + (uint)baseType + 1u);

            return true;
        }
    }
}
