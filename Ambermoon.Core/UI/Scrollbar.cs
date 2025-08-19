/*
 * Scrollbar.cs - Scrollbar
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

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

        public Scrollbar(Game game, Layout layout, ScrollbarType type, Rect scrollArea, int width, int height, int scrollRange, byte displayLayer = 1)
        {
            this.scrollArea = scrollArea;
            vertical = type == ScrollbarType.SmallVertical || type == ScrollbarType.LargeVertical; // Note: There are no horizontal ones in Ambermoon.
            ScrollRange = scrollRange;
            barSize = vertical ? height : width;
            position = new Position(scrollArea.Position);
            baseType = type;

            backgroundSprite = layout.RenderView.SpriteFactory.CreateLayered(scrollArea.Width, scrollArea.Height);
            backgroundSprite.Layer = layout.RenderView.GetLayer(Layer.UI);
            backgroundSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + type switch
            {
                ScrollbarType.SmallVertical => (uint)UICustomGraphic.ScrollbarBackgroundSmallVertical,
                ScrollbarType.LargeVertical => (uint)UICustomGraphic.ScrollbarBackgroundLargeVertical,
                _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid scrollbar type.")
            });
            backgroundSprite.DisplayLayer = displayLayer;
            backgroundSprite.PaletteIndex = game.UIPaletteIndex;
            backgroundSprite.X = scrollArea.X;
            backgroundSprite.Y = scrollArea.Y;
            backgroundSprite.Visible = true;

            // We add 1 to height because there is 1 pixel row for a shadow.
            sprite = layout.RenderView.SpriteFactory.CreateLayered(width, height + 1);
            sprite.Layer = layout.RenderView.GetLayer(Layer.UI);
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)type);
            sprite.DisplayLayer = (byte)Math.Min(255, displayLayer + 2);
            sprite.PaletteIndex = game.UIPaletteIndex;
            sprite.X = position.X;
            sprite.Y = position.Y;
            sprite.Visible = true;

            if (scrollRange == 0)
                Disabled = true;
        }

        public void SetScrollRange(int scrollRange)
        {
            ScrollRange = scrollRange;

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

        public void SetScrollPosition(int position, bool raiseEvent = false, bool force = false)
        {
            if (!force && ScrollOffset == position)
                return;

            bool scrolled = ScrollOffset != position;
            ScrollOffset = position;

            if (vertical)
            {
                SetBarPosition(new Position(this.position.X, scrollArea.Top + Util.Round((float)position * (scrollArea.Height - barSize) / ScrollRange)));
            }
            else // horizontal
            {
                SetBarPosition(new Position(scrollArea.Left + Util.Round((float)position * (scrollArea.Width - barSize) / ScrollRange), this.position.Y));
            }

            if (scrolled && raiseEvent)
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
                    int areaSize;
                    int entriesPerScroll = 0;
                    do
                    {
                        areaSize = (scrollArea.Height - barSize) / ((ScrollRange + 1) / ++entriesPerScroll);
                    } while (areaSize == 0);
                    int newPosition = Math.Min(ScrollRange, entriesPerScroll * (position.Y - (scrollArea.Top + barSize / 2)) / areaSize);
                    if (newPosition != ScrollOffset)
                    {
                        ScrollOffset = newPosition;
                        Scrolled?.Invoke(newPosition);
                    }
                    SetBarPosition(new Position(this.position.X, scrollArea.Top + Util.Round((float)newPosition * (scrollArea.Height - barSize) / ScrollRange)));
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
                    int areaSize;
                    int entriesPerScroll = 0;
                    do
                    {
                        areaSize = (scrollArea.Width - barSize) / ((ScrollRange + 1) / ++entriesPerScroll);
                    } while (areaSize == 0);
                    int newPosition = Math.Min(ScrollRange, entriesPerScroll * (position.X - (scrollArea.Left + barSize / 2)) / areaSize);
                    if (newPosition != ScrollOffset)
                    {
                        ScrollOffset = newPosition;
                        Scrolled?.Invoke(newPosition);
                    }
                    SetBarPosition(new Position(scrollArea.Left + Util.Round((float)newPosition * (scrollArea.Width - barSize) / ScrollRange), this.position.Y));
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
            if (BarArea.Contains(position))
            {
                Scrolling = true;
                scrollStartPosition = ScrollOffset;
                sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)baseType + 1u);

                return true;
            }
            else if (scrollArea.Contains(position))
            {
                int scrollAmount = Util.Limit(1, ScrollRange / 2, 2);

                if (position.Y > BarArea.Y)
                {
                    if (ScrollOffset < ScrollRange)
                        SetScrollPosition(Math.Min(ScrollRange, ScrollOffset + scrollAmount), true);
                    else // The bar might be positioned in-between
                        SetScrollPosition(ScrollRange, true, true);
                }
                else if (position.Y < BarArea.Y)
                {
                    if (ScrollOffset > 0)
                        SetScrollPosition(Math.Max(0, ScrollOffset - scrollAmount), true);
                    else // The bar might be positioned in-between
                        SetScrollPosition(0, true, true);
                }

                return true;
            }

            return false;
        }
    }
}
