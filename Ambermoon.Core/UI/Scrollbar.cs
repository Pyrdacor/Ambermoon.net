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
        public int ScrollRange { get; private set; }
        readonly int barSize;
        readonly ILayerSprite backgroundSprite;
        readonly ILayerSprite sprite;
        readonly ScrollbarType baseType; // the highlighted one is always 1 above
        public bool Scrolling { get; private set; } = false;
        int? scrollStartPosition = null;
        bool disabled = false;
        Rect BarArea => new Rect(position, new Size(vertical ? scrollArea.Width : barSize, vertical ? barSize : scrollArea.Height));
        public event Action<int> Scrolled;
        public int ScrollOffset { get; private set; } = 0;
        public bool Disabled
        {
            get => disabled;
            set
            {
                if (disabled == value)
                    return;

                disabled = value;

                if (disabled)
                {
                    backgroundSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + baseType switch
                    {
                        ScrollbarType.SmallVertical => (uint)UICustomGraphic.ScrollbarSmallVerticalDisabled,
                        ScrollbarType.LargeVertical => (uint)UICustomGraphic.ScrollbarLargeVerticalDisabled,
                        _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid scrollbar type.")
                    });
                }
                else
                {
                    backgroundSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + baseType switch
                    {
                        ScrollbarType.SmallVertical => (uint)UICustomGraphic.ScrollbarBackgroundSmallVertical,
                        ScrollbarType.LargeVertical => (uint)UICustomGraphic.ScrollbarBackgroundLargeVertical,
                        _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid scrollbar type.")
                    });
                }

                sprite.Visible = !disabled;
            }
        }

        public Scrollbar(Layout layout, ScrollbarType type, Rect scrollArea, int width, int height, int scrollRange, byte displayLayer = 1)
        {
            this.scrollArea = scrollArea;
            vertical = type == ScrollbarType.SmallVertical || type == ScrollbarType.LargeVertical; // TODO: are there even horizontal ones?
            this.ScrollRange = scrollRange;
            barSize = vertical ? height : width;
            position = new Position(scrollArea.Position);
            baseType = type;

            backgroundSprite = layout.RenderView.SpriteFactory.Create(scrollArea.Width, scrollArea.Height, true) as ILayerSprite;
            backgroundSprite.Layer = layout.RenderView.GetLayer(Layer.UI);
            backgroundSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + type switch
            {
                ScrollbarType.SmallVertical => (uint)UICustomGraphic.ScrollbarBackgroundSmallVertical,
                ScrollbarType.LargeVertical => (uint)UICustomGraphic.ScrollbarBackgroundLargeVertical,
                _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid scrollbar type.")
            });
            backgroundSprite.DisplayLayer = displayLayer;
            backgroundSprite.PaletteIndex = 49;
            backgroundSprite.X = scrollArea.X;
            backgroundSprite.Y = scrollArea.Y;
            backgroundSprite.Visible = true;

            // We add 1 to height because there is 1 pixel row for a shadow.
            sprite = layout.RenderView.SpriteFactory.Create(width, height + 1, true) as ILayerSprite;
            sprite.Layer = layout.RenderView.GetLayer(Layer.UI);
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)type);
            sprite.DisplayLayer = (byte)Math.Min(255, displayLayer + 2);
            sprite.PaletteIndex = 49;
            sprite.X = position.X;
            sprite.Y = position.Y;
            sprite.Visible = true;

            if (scrollRange == 0)
                Disabled = true;
        }

        void SetBarPosition(Position position)
        {
            if (this.position == position)
                return;

            this.position = position;

            sprite.X = vertical ? position.X : Util.Limit(scrollArea.Left, position.X, scrollArea.Right - barSize);
            sprite.Y = vertical ? Util.Limit(scrollArea.Top, position.Y, scrollArea.Bottom - barSize) : position.Y;
        }

        public void SetScrollPosition(int position, bool raiseEvent = false)
        {
            if (ScrollOffset == position)
                return;

            ScrollOffset = position;

            if (vertical)
            {
                SetBarPosition(new Position(this.position.X, scrollArea.Top + Util.Round((float)position * (scrollArea.Height - barSize) / ScrollRange)));
            }
            else // horizontal
            {
                SetBarPosition(new Position(scrollArea.Left + Util.Round((float)position * (scrollArea.Width - barSize) / ScrollRange), this.position.Y));
            }

            if (raiseEvent)
                Scrolled?.Invoke(ScrollOffset);
        }

        public void Destroy()
        {
            sprite?.Delete();
            backgroundSprite?.Delete();
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
                    SetScrollPosition(ScrollRange);
                else
                {
                    // There are (n + 1) areas where n is the scroll range.
                    // But they start at half bar size from both ends.
                    // The bar's center will jump to the center of an area
                    // if the cursor is closest to this center.
                    // The correct area can be calculated by dividing the
                    // cursor position by the area size.
                    int areaSize = (scrollArea.Height - barSize) / (ScrollRange + 1);
                    int newPosition = Math.Min(ScrollRange, (position.Y - (scrollArea.Top + barSize / 2)) / areaSize);
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
                    SetScrollPosition(ScrollRange);
                else
                {
                    int areaSize = (scrollArea.Width - barSize) / (ScrollRange + 1);
                    int newPosition = Math.Min(ScrollRange, (position.X - (scrollArea.Left + +barSize / 2)) / areaSize);
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
                sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)baseType);
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
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)baseType + 1u);

            return true;
        }
    }
}
